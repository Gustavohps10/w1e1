namespace W1E1.Core
{
    public sealed class FilterAstNode
    {
        public FilterNodeType NodeType { get; init; }
        public LogicalOperator Operator { get; init; }
        public string Expression { get; init; } = string.Empty;
        public object? Value { get; init; }
        public List<object> Values { get; init; } = new List<object>();
        public string[] Columns { get; init; } = Array.Empty<string>();
        public List<FilterAstNode> Children { get; init; } = new List<FilterAstNode>();
    }
}
