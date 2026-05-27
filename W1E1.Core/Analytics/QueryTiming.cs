namespace W1E1.Core.Analytics
{
    public sealed class QueryTiming
    {
        public long BuildMs { get; init; }
        public long ExecuteMs { get; init; }
        public long MapMs { get; init; }
        public long TotalMs => BuildMs + ExecuteMs + MapMs;
        public override string ToString() => $"[Build={BuildMs}ms | Execute={ExecuteMs}ms | Map={MapMs}ms | Total={TotalMs}ms]";
    }
}
