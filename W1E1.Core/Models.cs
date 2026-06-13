using System.Collections.Generic;
using System.Linq.Expressions;
using Dapper;

namespace W1E1.Core
{
    internal static class SqlConstants
    {
        internal const string TotalCountColumn = "TotalRegistros";
        internal const string WindowCountClause = ", COUNT(*) OVER() AS TotalRegistros";
    }

    public sealed class QueryTiming
    {
        public long BuildMs { get; init; }
        public long ExecuteMs { get; init; }
        public long MapMs { get; init; }
    }

    public sealed class QueryResult<T>
    {
        public List<T> Items { get; init; } = new();
        public int Total { get; init; }
        public bool IsPaged { get; init; }
        public QueryTiming Timing { get; init; } = new();
    }

    public sealed class RenderResult
    {
        public string Sql { get; init; } = string.Empty;
        public DynamicParameters Parameters { get; init; } = new();
    }

    internal sealed class FilterAstNode
    {
        public FilterNodeType NodeType { get; set; }
        public LogicalOperator Operator { get; set; }
        
        public string RawSql { get; set; } = string.Empty;
        
        public Expression? LambdaExpression { get; set; }
        
        public string ExpressionString { get; set; } = string.Empty;
        public object? Value { get; set; }
        
        public SqlQuery<object>? SubQueryObj { get; set; }
        
        public List<FilterAstNode>? Children { get; set; }
    }

    internal sealed class SqlJoin
    {
        public JoinType Type { get; set; }
        public string Table { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public IEnumerable<string> Conditions { get; set; } = Enumerable.Empty<string>();
    }

    internal sealed class SqlUnion
    {
        public string Operator { get; set; } = string.Empty;
        public SqlQuery<object> Query { get; set; } = null!;
    }
}