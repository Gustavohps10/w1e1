using W1E1.Core.Providers;

namespace W1E1.SqlServer
{
    internal static class SqlConstants
    {
        internal const string TotalCountColumn = "TotalRegistros";
        internal const string WindowCountClause = ", COUNT(*) OVER() AS TotalRegistros";
    }

    public sealed class SqlServerProvider : ISqlProvider
    {
        public static readonly SqlServerProvider Instance = new SqlServerProvider();

        public string QuoteIdentifier(string name) => $"[{name.Replace("]", "]]")}]";

        public string PaginationStartParam => "@_start";
        public string PaginationEndParam => "@_end";
        public string PaginationOffsetParam => "@_offset";
        public string PaginationFetchParam => "@_fetch";

        public string BuildPaginationRowNumber(string source, string filter, string orderBy, string direction, bool includeTotal)
        {
            string total = includeTotal ? SqlConstants.WindowCountClause : string.Empty;
            return $@"SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY {orderBy} {direction}) AS RowNum
                                           {total}
                                    FROM {source}
                                    {filter}
                                ) X
                                WHERE RowNum BETWEEN {PaginationStartParam} AND {PaginationEndParam}";
        }

        public string BuildPaginationOffsetFetch(string source, string filter, string orderBy, string direction, bool includeTotal)
        {
            string total = includeTotal ? SqlConstants.WindowCountClause : string.Empty;
            return $@"SELECT *
                    {total}
              FROM {source}
              {filter}
              ORDER BY {orderBy} {direction}
              OFFSET {PaginationOffsetParam} ROWS FETCH NEXT {PaginationFetchParam} ROWS ONLY";
        }
    }
}
