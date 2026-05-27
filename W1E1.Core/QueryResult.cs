using W1E1.Core.Analytics;

namespace W1E1.Core
{
    public sealed class QueryResult<T>
    {
        public List<T> Items { get; init; } = new List<T>();
        public int Total { get; init; }
        public bool IsPaged { get; init; }
        public QueryTiming? Timing { get; init; }
    }
}
