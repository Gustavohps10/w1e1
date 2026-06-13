namespace W1E1.Core
{
    public enum SourceType { None, Table, Raw, Hybrid, Entity }
    public enum JoinType { Inner, Left, Right, Full, CrossApply, OuterApply }
    public enum PaginationMode { None, RowNumber, OffsetFetch }
    public enum CountStrategy { None, Window, Separate }
    public enum LogLevel { None, Summary, Detailed }
    public enum FilterNodeType { Raw, Condition, ExpressionTree, Group, Exists, NotExists }
    public enum LogicalOperator { And, Or }
    public enum ColumnType { String, Int, Decimal, DateTime, Bool, Guid, Unknown }
}