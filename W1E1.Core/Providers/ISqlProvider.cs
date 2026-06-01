namespace W1E1.Core.Providers
{
    public interface ISqlProvider
    {
        string QuoteIdentifier(string name);
        string BuildPaginationRowNumber(string source, string filter, string orderBy, string direction, bool includeTotal);
        string BuildPaginationOffsetFetch(string source, string filter, string orderBy, string direction, bool includeTotal);
        string PaginationStartParam { get; }
        string PaginationEndParam { get; }
        string PaginationOffsetParam { get; }
        string PaginationFetchParam { get; }
    }
}
