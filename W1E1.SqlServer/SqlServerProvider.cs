using W1E1.Core;

namespace W1E1.SqlServer
{
    public sealed class SqlServerProvider : ISqlProvider
    {
        public static readonly SqlServerProvider Instance = new SqlServerProvider();

        private SqlServerProvider() { }

        public string PaginationStartParam => "_start";
        public string PaginationEndParam => "_end";
        public string PaginationOffsetParam => "_offset";
        public string PaginationFetchParam => "_fetch";
        public string BuildIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            if (name.StartsWith("[") && name.EndsWith("]")) return name;
            
            return $"[{name.Replace("]", "]]")}]";
        }

        public string BuildParameter(int index)
        {
            return $"p{index}";
        }

        public string BuildPagination(string coreSelect, string orderClause, PaginationMode mode, bool includeTotal)
        {
            string windowCount = includeTotal ? ", COUNT(*) OVER() AS TotalRegistros" : string.Empty;

            if (mode == PaginationMode.OffsetFetch)
            {
                // Estratégia moderna com OFFSET / FETCH
                return $@"SELECT *{windowCount} FROM (
    {coreSelect.Replace("\n", "\n    ")}
) _OFFSET_WRAP_
{orderClause}
OFFSET @{PaginationOffsetParam} ROWS FETCH NEXT @{PaginationFetchParam} ROWS ONLY";
            }

            // Estratégia clássica com ROW_NUMBER()
            return $@"SELECT *
FROM (
    SELECT *, ROW_NUMBER() OVER ({orderClause}) AS RowNum{windowCount}
    FROM (
        {coreSelect.Replace("\n", "\n        ")}
    ) _PAG_BASE
) _PAG_WRAP
WHERE RowNum BETWEEN @{PaginationStartParam} AND @{PaginationEndParam}";
        }
    }
}