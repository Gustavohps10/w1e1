using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace W1E1.Core
{
    public sealed class SqlFilter
    {
        internal List<FilterAstNode> Nodes { get; } = new();
        private readonly ISqlProvider? _provider;
        private readonly IMetadataProvider? _metadataProvider;

        public SqlFilter() { }

        internal SqlFilter(ISqlProvider? provider, IMetadataProvider? metadataProvider)
        {
            _provider = provider;
            _metadataProvider = metadataProvider;
        }

        // --- ESTILO 1: Expression Tree ---
        public SqlFilter Where<T>(Expression<Func<T, bool>> predicate) => AddExpression(LogicalOperator.And, predicate);
        public SqlFilter And<T>(Expression<Func<T, bool>> predicate) => AddExpression(LogicalOperator.And, predicate);
        public SqlFilter Or<T>(Expression<Func<T, bool>> predicate) => AddExpression(LogicalOperator.Or, predicate);

        private SqlFilter AddExpression(LogicalOperator op, Expression predicate)
        {
            Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.ExpressionTree, Operator = op, LambdaExpression = predicate });
            return this;
        }

        // --- ESTILO 2: Lambda Property ---
        public SqlFilter Where<T>(Expression<Func<T, object>> property, object? value) => And<T>(property, "=", value);
        public SqlFilter Where<T>(Expression<Func<T, object>> property, string op, object? value) => And<T>(property, op, value);
        
        public SqlFilter And<T>(Expression<Func<T, object>> property, object? value) => And<T>(property, "=", value);
        public SqlFilter And<T>(Expression<Func<T, object>> property, string op, object? value)
        {
            string col = GetPropertyName(property);
            Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Condition, Operator = LogicalOperator.And, ExpressionString = $"{col} {op}", Value = value });
            return this;
        }

        public SqlFilter Or<T>(Expression<Func<T, object>> property, object? value) => Or<T>(property, "=", value);
        public SqlFilter Or<T>(Expression<Func<T, object>> property, string op, object? value)
        {
            string col = GetPropertyName(property);
            Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Condition, Operator = LogicalOperator.Or, ExpressionString = $"{col} {op}", Value = value });
            return this;
        }

        // --- ESTILO 3: Strings (Escape Hatch com sobrecargas de Condição) ---
        public SqlFilter Where(string expression, object? value = null) => And(expression, value);
        public SqlFilter Where(bool condition, string expression, object? value = null) => And(condition, expression, value);

        public SqlFilter And(string expression, object? value = null)
        {
            if (value is null) return this;
            Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Condition, Operator = LogicalOperator.And, ExpressionString = expression, Value = value });
            return this;
        }

        public SqlFilter And(bool condition, string expression, object? value = null)
        {
            if (!condition) return this;
            return And(expression, value);
        }

        public SqlFilter Or(string expression, object? value = null)
        {
            if (value is null) return this;
            Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Condition, Operator = LogicalOperator.Or, ExpressionString = expression, Value = value });
            return this;
        }

        public SqlFilter Or(bool condition, string expression, object? value = null)
        {
            if (!condition) return this;
            return Or(expression, value);
        }

        public SqlFilter Raw(string sql)
        {
            if (!string.IsNullOrWhiteSpace(sql))
                Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Raw, Operator = LogicalOperator.And, RawSql = sql });
            return this;
        }

        public SqlFilter Raw(bool condition, string sql)
        {
            if (!condition) return this;
            return Raw(sql);
        }

        // --- SUBGRUPOS (Fluent Logic) ---
        public SqlFilter Where(Action<SqlFilter> configure) => AppendGroup(LogicalOperator.And, configure);
        public SqlFilter And(Action<SqlFilter> configure) => AppendGroup(LogicalOperator.And, configure);
        public SqlFilter Or(Action<SqlFilter> configure) => AppendGroup(LogicalOperator.Or, configure);

        private SqlFilter AppendGroup(LogicalOperator op, Action<SqlFilter> configure)
        {
            var child = new SqlFilter(_provider, _metadataProvider);
            configure(child);
            if (child.Nodes.Count > 0)
                Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Group, Operator = op, Children = child.Nodes });
            return this;
        }

        // --- SUBQUERIES (Exists / Scalar) ---
        public SqlFilter WhereExists(Action<SqlQuery<object>> subQueryBuilder) => ExistsInternal(LogicalOperator.And, FilterNodeType.Exists, subQueryBuilder);
        public SqlFilter AndNotExists(Action<SqlQuery<object>> subQueryBuilder) => ExistsInternal(LogicalOperator.And, FilterNodeType.NotExists, subQueryBuilder);

        private SqlFilter ExistsInternal(LogicalOperator op, FilterNodeType type, Action<SqlQuery<object>> subQueryBuilder)
        {
            var subQuery = _provider != null 
                ? new SqlQuery<object>(_provider, _metadataProvider!) 
                : new SqlQuery<object>();

            subQueryBuilder(subQuery);
            Nodes.Add(new FilterAstNode { NodeType = type, Operator = op, SubQueryObj = subQuery });
            return this;
        }

        public SqlFilter Where(string expression, Action<SqlQuery<object>> subQueryBuilder) => And(expression, subQueryBuilder);
        public SqlFilter And(string expression, Action<SqlQuery<object>> subQueryBuilder)
        {
            var subQuery = _provider != null 
                ? new SqlQuery<object>(_provider, _metadataProvider!) 
                : new SqlQuery<object>();

            subQueryBuilder(subQuery);
            Nodes.Add(new FilterAstNode { NodeType = FilterNodeType.Condition, Operator = LogicalOperator.And, ExpressionString = expression, SubQueryObj = subQuery });
            return this;
        }

        // --- UTIL ---
        private static string GetPropertyName<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberUnary)
                return memberUnary.Member.Name;
            if (expression.Body is MemberExpression member)
                return member.Member.Name;
            throw new ArgumentException("Expressão inválida para mapeamento de propriedade.");
        }
    }
}