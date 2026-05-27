using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using W1E1.Core.Providers;
using W1E1.Core.Analytics;

namespace W1E1.Core
{
    public enum PaginationMode { None, RowNumber, OffsetFetch }
    public enum CountStrategy { None, Window, Separate }
    public enum LogLevel { None, Summary, Detailed }
    public enum FilterNodeType { Condition, In, NotIn, Search, Group, Raw }
    public enum LogicalOperator { And, Or }

    internal static class SqlConstants
    {
        internal const string TotalCountColumn = "TotalRegistros";
        internal const string WindowCountClause = ", COUNT(*) OVER() AS TotalRegistros";
    }

    public sealed class RenderResult
    {
        public string Sql { get; init; } = string.Empty;
        public DynamicParameters Parameters { get; init; } = new DynamicParameters();
    }

    public sealed class SqlQuery<T> where T : new()
    {
        private string _sql = string.Empty;
        private readonly ISqlProvider _provider;
        private bool _isCte;
        private readonly Dictionary<string, SqlFilter> _pushdowns = new Dictionary<string, SqlFilter>();
        private SqlFilter _mainFilter = new SqlFilter();

        private string _orderBy = "1";
        private string _direction = "ASC";
        private bool _paged;
        private int _start;
        private int _length;
        private PaginationMode _paginationMode = PaginationMode.None;
        private CountStrategy _countStrategy = CountStrategy.None;
        private LogLevel _logLevel = LogLevel.None;
        private Action<string>? _logger;

        public SqlQuery(ISqlProvider provider)
        {
            _provider = provider;
        }

        public SqlQuery<T> From(string sql)
        {
            _sql = sql;
            _isCte = false;
            return this;
        }

        public SqlQuery<T> Prepare(string sql)
        {
            return this;
        }

        public SqlQuery<T> FromCte(string sql)
        {
            _sql = sql;
            _isCte = true;
            return this;
        }
        public static SqlQuery<T> Prepare(string sql, ISqlProvider provider)
        {
            return new SqlQuery<T>(provider).From(sql);
        }

        public static SqlQuery<T> From(string sql, ISqlProvider provider)
        {
            return new SqlQuery<T>(provider).From(sql);
        }

        public static SqlQuery<T> FromCte(string sql, ISqlProvider provider)
        {
            return new SqlQuery<T>(provider).FromCte(sql);
        }

        public SqlQuery<T> Pushdown(string key, SqlFilter filter)
        {
            _pushdowns[key] = filter;
            return this;
        }
        public SqlQuery<T> Pushdown(string key, Func<SqlFilter, SqlFilter> configureFilter)
        {
            SqlFilter filter = new SqlFilter();
            _pushdowns[key] = configureFilter(filter);
            return this;
        }

        public SqlQuery<T> Filter(SqlFilter filter)
        {
            if (filter != null) _mainFilter = filter;
            return this;
        }

        public SqlQuery<T> Filter(Action<SqlFilter> configure)
        {
            if (configure != null) configure(_mainFilter);
            return this;
        }

        public SqlQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> expr, string direction = "ASC")
        {
            _orderBy = ColumnResolver.Resolve(expr);
            _direction = string.Equals(direction, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            return this;
        }

        public SqlQuery<T> OrderBy(string column, string direction = "ASC")
        {
            PropertyInfo? prop = string.IsNullOrWhiteSpace(column) ? null :
                typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .FirstOrDefault(p => string.Equals(p.Name, column, StringComparison.OrdinalIgnoreCase));

            if (prop is null) throw new ArgumentException($"Coluna inválida para OrderBy: '{column}' no tipo {typeof(T).Name}");

            _orderBy = prop.Name;
            _direction = string.Equals(direction, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            return this;
        }

        public SqlQuery<T> Page(int start, int length, PaginationMode mode = PaginationMode.RowNumber)
        {
            _paged = true;
            _start = start < 0 ? 0 : start;
            _length = length <= 0 ? 50 : length;
            _paginationMode = mode;
            return this;
        }

        public SqlQuery<T> Page(string start, string length, PaginationMode mode = PaginationMode.RowNumber)
        {
            int s;
            int l;
            int.TryParse(start, out s);
            int.TryParse(length, out l);
            return Page(s, l, mode);
        }

        public SqlQuery<T> Count(CountStrategy strategy)
        {
            _countStrategy = strategy;
            return this;
        }

        public SqlQuery<T> WithLog(LogLevel level, Action<string>? logger = null)
        {
            _logLevel = level;
            _logger = logger ?? (msg => Debug.WriteLine(msg));
            return this;
        }

        private string SafeOrderBy => int.TryParse(_orderBy, out int _) ? _orderBy : _provider.QuoteIdentifier(_orderBy);

        private static void SplitCte(string sql, out string cte, out string main)
        {
            int level = 0;
            bool inString = false;

            for (int i = 0; i <= sql.Length - 6; i++)
            {
                char c = sql[i];
                if (c == '\'')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (c == '(')
                {
                    level++;
                    continue;
                }

                if (c == ')')
                {
                    level--;
                    continue;
                }

                if (level != 0) continue;
                if (c != 'S' && c != 's') continue;
                if (string.Compare(sql, i, "SELECT", 0, 6, StringComparison.OrdinalIgnoreCase) != 0) continue;

                bool validStart = i == 0 || char.IsWhiteSpace(sql[i - 1]) || sql[i - 1] == ')';
                bool validEnd = (i + 6 == sql.Length) || char.IsWhiteSpace(sql[i + 6]);

                if (!validStart || !validEnd) continue;

                cte = sql.Substring(0, i).Trim();
                if (cte.Length > 0 && !cte.StartsWith(";")) cte = ";" + cte;
                if (cte.Length > 0) cte += "\n";

                main = sql.Substring(i).Trim();
                return;
            }

            cte = string.Empty;
            main = sql;
        }

        private sealed class CompiledQuery
        {
            public string MainSql { get; init; } = string.Empty;
            public string CountSql { get; init; } = string.Empty;
            public DynamicParameters Parameters { get; init; } = new DynamicParameters();
        }

        private CompiledQuery Compile()
        {
            DynamicParameters execParams = new DynamicParameters();
            int paramIndex = 0;
            string resolvedSql = _sql;

            foreach (KeyValuePair<string, SqlFilter> pd in _pushdowns)
            {
                RenderResult rr = SqlFilterRenderer.Instance.Render(pd.Value, ref paramIndex);
                execParams.AddDynamicParams(rr.Parameters);
                resolvedSql = resolvedSql.Replace($"{{{{{pd.Key}}}}}", rr.Sql);
            }

            RenderResult mainRr = SqlFilterRenderer.Instance.Render(_mainFilter, ref paramIndex);
            execParams.AddDynamicParams(mainRr.Parameters);
            string filterSql = mainRr.Sql;
            string filterClause = string.IsNullOrWhiteSpace(filterSql) ? string.Empty : $"WHERE 1=1  {filterSql}";

            string ctePrefix = string.Empty;
            string mainSelect = resolvedSql;

            if (_isCte) SplitCte(resolvedSql, out ctePrefix, out mainSelect);

            string source = $"({mainSelect}) Q";
            bool includeTotal = _countStrategy == CountStrategy.Window;

            string mainSql = BuildMainQueryString(source, filterClause, includeTotal);
            mainSql = $"{ctePrefix}{mainSql}";

            string countSql = string.Empty;
            if (_countStrategy == CountStrategy.Separate)
            {
                countSql = $"{ctePrefix}SELECT COUNT(*) FROM {source} WHERE 1=1  {filterSql}";
            }

            if (_paged)
            {
                execParams.Add(_provider.PaginationStartParam, _start + 1, DbType.Int32);
                execParams.Add(_provider.PaginationEndParam, _start + _length, DbType.Int32);
                execParams.Add(_provider.PaginationOffsetParam, _start, DbType.Int32);
                execParams.Add(_provider.PaginationFetchParam, _length, DbType.Int32);
            }

            return new CompiledQuery
            {
                MainSql = mainSql,
                CountSql = countSql,
                Parameters = execParams
            };
        }

        private string ApplyPushdowns(string sql)
        {
            int dummy = 0;

            foreach (var pd in _pushdowns)
            {
                RenderResult rr = SqlFilterRenderer.Instance.Render(pd.Value, ref dummy);

                sql = sql.Replace($"{{{{{pd.Key}}}}}", rr.Sql);
            }

            return sql;
        }

        private string BuildMainQueryString(string source, string filterClause, bool includeTotal)
        {
            if (!_paged)
            {
                string totalClause = includeTotal ? SqlConstants.WindowCountClause : string.Empty;
                return $"SELECT *{totalClause} FROM {source} {filterClause} ORDER BY {SafeOrderBy} {_direction}";
            }

            if (_paginationMode == PaginationMode.OffsetFetch) return _provider.BuildPaginationOffsetFetch(source, filterClause, SafeOrderBy, _direction, includeTotal);

            return _provider.BuildPaginationRowNumber(source, filterClause, SafeOrderBy, _direction, includeTotal);
        }

        public async Task<QueryResult<T>> ExecuteAsync(SqlConnection connection, CancellationToken ct = default)
        {
            Stopwatch swPhase = Stopwatch.StartNew();

            CompiledQuery compiled = Compile();
            long buildMs = swPhase.ElapsedMilliseconds;

            if (_logLevel == LogLevel.Detailed) Log($"SQL:\n{compiled.MainSql}");

            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

            swPhase.Restart();

            CommandDefinition cmd = new CommandDefinition(compiled.MainSql, compiled.Parameters, cancellationToken: ct);
            IEnumerable<T> rows = await connection.QueryAsync<T>(cmd).ConfigureAwait(false);

            long executeMs = swPhase.ElapsedMilliseconds;

            swPhase.Restart();
            List<T> items = rows.AsList();
            long mapMs = swPhase.ElapsedMilliseconds;

            Log($"[Execute={executeMs}ms] [Map={mapMs}ms] [Rows={items.Count}]");

            if (items.Count == 0)
            {
                return new QueryResult<T>
                {
                    Items = items,
                    IsPaged = _paged,
                    Timing = MakeTiming(buildMs, executeMs, mapMs)
                };
            }

            int total = await ResolveTotalAsync(connection, compiled, items, ct).ConfigureAwait(false);

            return new QueryResult<T>
            {
                Items = items,
                Total = total,
                IsPaged = _paged,
                Timing = MakeTiming(buildMs, executeMs, mapMs)
            };
        }

        public string ToSql()
        {
            CompiledQuery compiled = Compile();
            return MaterializeParameters(compiled.MainSql, compiled.Parameters);
        }

        public string ToSqlFull()
        {
            CompiledQuery compiled = Compile();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- MAIN QUERY");
            sb.AppendLine(MaterializeParameters(compiled.MainSql, compiled.Parameters));

            if (_countStrategy == CountStrategy.Separate)
            {
                sb.AppendLine();
                sb.AppendLine("-- COUNT QUERY");
                sb.AppendLine(MaterializeParameters(compiled.CountSql, compiled.Parameters));
            }

            return sb.ToString();
        }

        private void Log(string message)
        {
            if (_logLevel != LogLevel.None) _logger?.Invoke($"[SqlQuery<{typeof(T).Name}>] {message}");
        }

        private static QueryTiming MakeTiming(long buildMs, long executeMs, long mapMs)
        {
            return new QueryTiming { BuildMs = buildMs, ExecuteMs = executeMs, MapMs = mapMs };
        }

        private async Task<int> ResolveTotalAsync(SqlConnection connection, CompiledQuery compiled, List<T> items, CancellationToken ct)
        {
            if (_countStrategy == CountStrategy.None) return items.Count;

            if (_countStrategy == CountStrategy.Window)
            {
                PropertyInfo? prop = typeof(T).GetProperty(SqlConstants.TotalCountColumn);
                if (prop != null && items.Count > 0) return Convert.ToInt32(prop.GetValue(items[0]));
                throw new InvalidOperationException($"CountStrategy.Window requer propriedade '{SqlConstants.TotalCountColumn}' no DTO '{typeof(T).Name}'.");
            }

            if (_countStrategy == CountStrategy.Separate)
            {
                CommandDefinition cmd = new CommandDefinition(compiled.CountSql, compiled.Parameters, cancellationToken: ct);
                return await connection.ExecuteScalarAsync<int>(cmd).ConfigureAwait(false);
            }

            return items.Count;
        }

        private static string MaterializeParameters(string sql, DynamicParameters parameters)
        {
            Dictionary<string, string> index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in parameters.ParameterNames)
            {
                string paramKey = name.StartsWith("@") ? name : "@" + name;
                index[paramKey] = ToSqlLiteral(parameters.Get<object>(name));
            }

            StringBuilder sb = new StringBuilder(sql.Length * 2);
            int pos = 0;
            int len = sql.Length;

            while (pos < len)
            {
                char c = sql[pos];
                if (c == '@' && pos + 1 < len)
                {
                    int start = pos;
                    pos++;

                    while (pos < len && (char.IsLetterOrDigit(sql[pos]) || sql[pos] == '_')) pos++;

                    string token = sql.Substring(start, pos - start);
                    string? value;
                    if (index.TryGetValue(token, out value))
                    {
                        sb.Append(value);
                        continue;
                    }

                    sb.Append(token);
                    continue;
                }

                sb.Append(c);
                pos++;
            }

            return sb.ToString();
        }

        private static string ToSqlLiteral(object? val)
        {
            if (val is null || val == DBNull.Value) return "NULL";

            if (val is byte || val is sbyte || val is short || val is ushort || val is int || val is uint || val is long || val is ulong || val is float || val is double || val is decimal)
                return Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";

            if (val is bool b) return b ? "1" : "0";
            if (val is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
            if (val is DateTimeOffset dto) return $"'{dto:yyyy-MM-dd HH:mm:ss.fff zzz}'";
            if (val is Guid g) return $"'{g}'";

            return $"'{val.ToString()!.Replace("'", "''")}'";
        }
    }
}