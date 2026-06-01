using Dapper;
using System.Text;

namespace W1E1.Core
{
    public sealed class SqlFilterRenderer
    {
        public static readonly SqlFilterRenderer Instance = new SqlFilterRenderer();

        public RenderResult Render(SqlFilter filter, ref int parameterIndex)
        {
            if (filter is null) return new RenderResult { Sql = string.Empty, Parameters = new DynamicParameters() };
            if (filter.Nodes.Count == 0) return new RenderResult { Sql = string.Empty, Parameters = new DynamicParameters() };

            DynamicParameters parameters = new DynamicParameters();
            int[] indexRef = new int[] { parameterIndex };

            string sql = RenderNodes(filter.Nodes, parameters, indexRef, true);
            parameterIndex = indexRef[0];

            return new RenderResult { Sql = sql, Parameters = parameters };
        }

        private string RenderNodes(List<FilterAstNode> nodes, DynamicParameters parameters, int[] indexRef, bool isRoot)
        {
            if (nodes.Count == 0) return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < nodes.Count; i++)
            {
                FilterAstNode node = nodes[i];
                string nodeSql = RenderNode(node, parameters, indexRef);

                if (string.IsNullOrWhiteSpace(nodeSql)) continue;

                if (i > 0)
                {
                    builder.Append(node.Operator == LogicalOperator.And ? " AND " : " OR ");
                    builder.Append(nodeSql);
                    continue;
                }

                if (isRoot) builder.Append(node.Operator == LogicalOperator.And ? "AND " : "OR ");

                builder.Append(nodeSql);
            }

            return builder.ToString();
        }

        private string RenderNode(FilterAstNode node, DynamicParameters parameters, int[] indexRef)
        {
            if (node.NodeType == FilterNodeType.Condition) return RenderCondition(node, parameters, indexRef);
            if (node.NodeType == FilterNodeType.In) return RenderIn(node, parameters, indexRef, false);
            if (node.NodeType == FilterNodeType.NotIn) return RenderIn(node, parameters, indexRef, true);
            if (node.NodeType == FilterNodeType.Search) return RenderSearch(node, parameters, indexRef);
            if (node.NodeType == FilterNodeType.Group) return RenderGroup(node, parameters, indexRef);
            if (node.NodeType == FilterNodeType.Raw) return node.Expression;

            return string.Empty;
        }

        private static readonly string[] _operators = new string[] { "=", "!=", "<>", ">=", "<=", ">", "<", "LIKE", "NOT LIKE", "IS", "IS NOT" };

        private bool EndsWithOperator(string expression)
        {
            string trimmed = expression.TrimEnd();
            for (int i = 0; i < _operators.Length; i++)
            {
                if (trimmed.EndsWith(_operators[i], StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private string RenderCondition(FilterAstNode node, DynamicParameters parameters, int[] indexRef)
        {
            indexRef[0]++;
            string paramName = $"@_p{indexRef[0]}";
            parameters.Add(paramName, node.Value);

            string expression = node.Expression.TrimEnd();
            bool hasOperator = EndsWithOperator(expression);

            if (hasOperator) return $"{expression} {paramName}";

            return $"{expression} = {paramName}";
        }

        private string RenderIn(FilterAstNode node, DynamicParameters parameters, int[] indexRef, bool isNot)
        {
            if (node.Values.Count == 0) return string.Empty;

            List<string> paramNames = new List<string>(node.Values.Count);
            for (int i = 0; i < node.Values.Count; i++)
            {
                indexRef[0]++;
                string paramName = $"@_p{indexRef[0]}";
                parameters.Add(paramName, node.Values[i]);
                paramNames.Add(paramName);
            }

            string operatorString = isNot ? "NOT IN" : "IN";
            string joinedParams = string.Join(",", paramNames);
            return $"{node.Expression} {operatorString} ({joinedParams})";
        }

        private string RenderSearch(FilterAstNode node, DynamicParameters parameters, int[] indexRef)
        {
            indexRef[0]++;
            string paramName = $"@_p{indexRef[0]}";
            parameters.Add(paramName, $"%{node.Expression}%");

            List<string> searchClauses = new List<string>(node.Columns.Length);
            for (int i = 0; i < node.Columns.Length; i++)
            {
                searchClauses.Add($"{node.Columns[i]} LIKE {paramName}");
            }

            string joined = string.Join(" OR ", searchClauses);
            return $"({joined})";
        }

        private string RenderGroup(FilterAstNode node, DynamicParameters parameters, int[] indexRef)
        {
            string inner = RenderNodes(node.Children, parameters, indexRef, false);
            if (string.IsNullOrWhiteSpace(inner)) return string.Empty;
            return $"({inner})";
        }
    }
}
