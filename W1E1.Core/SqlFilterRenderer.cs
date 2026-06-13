using Dapper;
using System.Collections;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace W1E1.Core
{
    internal sealed class SqlFilterRenderer
    {
        public static readonly SqlFilterRenderer Instance = new SqlFilterRenderer();

        private SqlFilterRenderer() { }

        public RenderResult Render(SqlFilter filter, ref int paramIndex)
        {
            var parameters = new DynamicParameters();
            var sql = RenderNodes(filter.Nodes, parameters, ref paramIndex, isRoot: true);

            return new RenderResult { Sql = sql, Parameters = parameters };
        }

        private string RenderNodes(List<FilterAstNode> nodes, DynamicParameters parameters, ref int paramIndex, bool isRoot)
        {
            if (nodes == null || nodes.Count == 0) return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Raw nodes que já têm prefixo AND/OR explícito não precisam de conector adicional
                bool rawHasPrefix = node.NodeType == FilterNodeType.Raw
                    && !string.IsNullOrWhiteSpace(node.RawSql)
                    && (node.RawSql.TrimStart().StartsWith("AND ", StringComparison.OrdinalIgnoreCase)
                        || node.RawSql.TrimStart().StartsWith("OR ", StringComparison.OrdinalIgnoreCase));

                if (i > 0 && !rawHasPrefix)
                {
                    sb.Append(nodes[i].Operator == LogicalOperator.And ? " AND " : " OR ");
                }

                switch (node.NodeType)
                {
                    case FilterNodeType.Raw:
                        if (rawHasPrefix && sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]))
                            sb.Append(' ');

                        // FIX B/C/H: Raw NUNCA adiciona parênteses automáticos.
                        // Parênteses são responsabilidade do Group que envolve o Raw,
                        // ou do chamador que decidiu usar Raw diretamente.
                        // Apenas normaliza espaços dentro de listas IN/NOT IN.
                        sb.Append(NormalizeInList(node.RawSql));
                        break;

                    case FilterNodeType.Condition:
                        if (node.SubQueryObj != null)
                        {
                            var subResult = node.SubQueryObj.CompileSubQuery(ref paramIndex);
                            parameters.AddDynamicParams(subResult.Parameters);
                            sb.Append($"{node.ExpressionString} (\n    {subResult.Sql.Replace("\n", "\n    ")}\n)");
                        }
                        else
                        {
                            string pName = $"p{paramIndex++}";
                            parameters.Add(pName, node.Value);
                            
                            string expr = node.ExpressionString.Trim();
                            if (!expr.Contains("=") && !expr.Contains(">") && !expr.Contains("<")
                                && !expr.Contains(" LIKE") && !expr.Contains(" IN"))
                            {
                                sb.Append($"{expr} = @{pName}");
                            }
                            else
                            {
                                sb.Append($"{expr} @{pName}");
                            }
                        }
                        break;

                    case FilterNodeType.ExpressionTree:
                        sb.Append(TranslateExpression(node.LambdaExpression, parameters, ref paramIndex));
                        break;

                    case FilterNodeType.Exists:
                    case FilterNodeType.NotExists:
                        if (node.SubQueryObj != null)
                        {
                            string existOp = node.NodeType == FilterNodeType.Exists ? "EXISTS" : "NOT EXISTS";
                            var subResult = node.SubQueryObj.CompileSubQuery(ref paramIndex);
                            parameters.AddDynamicParams(subResult.Parameters);
                            sb.Append($"{existOp}(\n    {subResult.Sql.Replace("\n", "\n    ")}\n)");
                        }
                        break;

                    case FilterNodeType.Group:
                        string groupSql = RenderNodes(node.Children!, parameters, ref paramIndex, isRoot: false);
                        if (!string.IsNullOrWhiteSpace(groupSql))
                        {
                            sb.Append($"({groupSql})");
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Normaliza espaços dentro de listas IN/NOT IN:
        /// "IN (a, b, c)" -> "IN (a,b,c)"
        /// Não altera outras partes da expressão.
        /// </summary>
        private static string NormalizeInList(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;
            return Regex.Replace(sql, @"\bIN\s*\(\s*([^)]*)\s*\)", m =>
            {
                var content = m.Groups[1].Value;
                var items = content.Split(',');
                var normalized = string.Join(",", System.Linq.Enumerable.Select(items, x => x.Trim()));
                return $"IN ({normalized})";
            });
        }

        private string TranslateExpression(Expression? expression, DynamicParameters parameters, ref int paramIndex)
        {
            if (expression == null) return string.Empty;

            if (expression is LambdaExpression lambda)
            {
                if (lambda.Body is MemberExpression mb && mb.Type == typeof(bool))
                    return $"{mb.Member.Name} = 1";
                if (lambda.Body is UnaryExpression un && un.NodeType == ExpressionType.Not && un.Operand is MemberExpression mbNot && mbNot.Type == typeof(bool))
                    return $"{mbNot.Member.Name} = 0";

                return TranslateExpression(lambda.Body, parameters, ref paramIndex);
            }

            if (expression is BinaryExpression binary)
            {
                string left = TranslateExpression(binary.Left, parameters, ref paramIndex);
                string right = TranslateExpression(binary.Right, parameters, ref paramIndex);
                string op = GetSqlOperator(binary.NodeType);

                if (binary.NodeType == ExpressionType.AndAlso || binary.NodeType == ExpressionType.OrElse)
                    return $"({left} {op} {right})";
                
                return $"{left} {op} {right}";
            }

            if (expression is MemberExpression member)
            {
                if (member.Expression != null && member.Expression.NodeType == ExpressionType.Parameter)
                    return member.Member.Name; 

                var value = GetValueFromMember(member);
                string pName = $"p{paramIndex++}";
                parameters.Add(pName, value);
                return $"@{pName}";
            }

            if (expression is ConstantExpression constant)
            {
                string pName = $"p{paramIndex++}";
                parameters.Add(pName, constant.Value);
                return $"@{pName}";
            }

            if (expression is UnaryExpression unary)
            {
                if (unary.NodeType == ExpressionType.Not)
                    return $"(NOT {TranslateExpression(unary.Operand, parameters, ref paramIndex)})";
                if (unary.NodeType == ExpressionType.Convert)
                    return TranslateExpression(unary.Operand, parameters, ref paramIndex);
            }

            if (expression is MethodCallExpression methodCall)
            {
                if (methodCall.Method.DeclaringType == typeof(string))
                {
                    string left = TranslateExpression(methodCall.Object, parameters, ref paramIndex);
                    var value = GetValueFromExpression(methodCall.Arguments[0]);
                    string pName = $"p{paramIndex++}";

                    if (methodCall.Method.Name == "StartsWith")
                    {
                        parameters.Add(pName, $"{value}%");
                        return $"{left} LIKE @{pName}";
                    }
                    if (methodCall.Method.Name == "EndsWith")
                    {
                        parameters.Add(pName, $"%{value}");
                        return $"{left} LIKE @{pName}";
                    }
                    if (methodCall.Method.Name == "Contains")
                    {
                        parameters.Add(pName, $"%{value}%");
                        return $"{left} LIKE @{pName}";
                    }
                }

                bool isExtensionContains = methodCall.Method.Name == "Contains" && methodCall.Arguments.Count == 2;
                bool isInstanceContains = methodCall.Method.Name == "Contains" && methodCall.Object != null;

                if (isExtensionContains || isInstanceContains)
                {
                    var collectionExpr = isExtensionContains ? methodCall.Arguments[0] : methodCall.Object;
                    var propertyExpr = isExtensionContains ? methodCall.Arguments[1] : methodCall.Arguments[0];

                    var collection = GetValueFromExpression(collectionExpr!) as IEnumerable;
                    string right = TranslateExpression(propertyExpr, parameters, ref paramIndex); 

                    if (collection != null)
                    {
                        var inParams = new List<string>();
                        foreach (var item in collection)
                        {
                            string pName = $"p{paramIndex++}";
                            parameters.Add(pName, item);
                            inParams.Add($"@{pName}");
                        }
                        return inParams.Count > 0 ? $"{right} IN ({string.Join(",", inParams)})" : "1=0";
                    }
                }
            }

            throw new NotSupportedException($"Expression type '{expression.NodeType}' não suportada.");
        }

        private string GetSqlOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Operador lógico {type} não mapeado.")
            };
        }

        private object? GetValueFromExpression(Expression expression)
        {
            if (expression is ConstantExpression constant) return constant.Value;
            if (expression is MemberExpression member) return GetValueFromMember(member);
            var lambda = Expression.Lambda(expression);
            return lambda.Compile().DynamicInvoke();
        }

        private object? GetValueFromMember(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            return getterLambda.Compile()();
        }
    }
}