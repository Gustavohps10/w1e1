using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace W1E1.Core
{
    public sealed class SqlQuery<T> where T : new()
    {
        private ISqlProvider? _provider;
        private IMetadataProvider? _metadataProvider;

        // Pipeline State
        private string? _prepareScript;
        private SourceType _sourceType = SourceType.None;
        private string _sourceSql = string.Empty;
        
        private readonly Dictionary<string, string> _ctes = new();
        private readonly Dictionary<string, SqlFilter> _pushdowns = new();
        private readonly List<SqlJoin> _joins = new();
        private SqlFilter _mainFilter = new();
        private readonly List<string> _projections = new();
        private string _sourceCteBlock = string.Empty;
        private readonly List<string> _groupings = new();
        private SqlFilter? _havingFilter;
        private readonly List<SqlUnion> _setOperators = new();
        private readonly List<string> _options = new();

        // Sort & Page
        private string _orderBy = "1";
        private string _direction = "ASC";
        private bool _paged;
        private int _skip;
        private int _take;
        private PaginationMode _paginationMode = PaginationMode.None;
        private CountStrategy _countStrategy = CountStrategy.None;
        private LogLevel _logLevel = LogLevel.None;
        private Action<string>? _logger;

        public SqlQuery() { }

        public SqlQuery(ISqlProvider provider)
        {
            _provider = provider;
        }

        public SqlQuery(ISqlProvider provider, IMetadataProvider metadataProvider)
        {
            _provider = provider;
            _metadataProvider = metadataProvider;
        }

        // --- PREPARE & PUSHDOWN ---
        public SqlQuery<T> Prepare(string sql)
        {
            _prepareScript = sql;
            return this;
        }

        public SqlQuery<T> Pushdown(string key, SqlFilter filter)
        {
            _pushdowns[key] = filter;
            return this;
        }

        public SqlQuery<T> Pushdown(string key, Func<SqlFilter, SqlFilter> configureFilter)
        {
            var filter = new SqlFilter();
            _pushdowns[key] = configureFilter(filter);
            return this;
        }

        // --- CTES ---
        public SqlQuery<T> FromCte(Action<CteBuilder> configure)
        {
            var builder = new CteBuilder();
            configure(builder);
            foreach (var cte in builder.GetCtes())
                _ctes[cte.Key] = cte.Value;
            return this;
        }

        public SqlQuery<T> FromCte(string rawCteSql)
        {
            string sql = rawCteSql?.Trim() ?? string.Empty;
            if (TryExtractCteBlock(sql, out var cteBlock, out var sourceSql))
            {
                _sourceCteBlock = cteBlock;
                _sourceType = SourceType.Hybrid;
                _sourceSql = sourceSql;
            }
            else
            {
                _sourceType = SourceType.Hybrid;
                _sourceSql = sql;
            }
            return this;
        }

        // --- SOURCES ---
        public SqlQuery<T> FromTable(string tableAndAlias)
        {
            _sourceType = SourceType.Table;
            _sourceSql = tableAndAlias;
            return this;
        }

        public SqlQuery<T> FromRaw(string rawSql)
        {
            _sourceType = SourceType.Raw;
            _sourceSql = rawSql;
            return this;
        }

        public SqlQuery<T> From(string hybridSql)
        {
            _sourceType = SourceType.Hybrid;
            _sourceSql = hybridSql;
            return this;
        }

        public SqlQuery<T> FromEntity<TEntity>(string alias)
        {
            if (_metadataProvider == null) throw new InvalidOperationException("IMetadataProvider não configurado.");
            _sourceType = SourceType.Entity;
            _sourceSql = $"{_metadataProvider.GetTableName<TEntity>()} {alias}";
            return this;
        }

        // --- JOINS ---
        public SqlQuery<T> InnerJoin(string tableAndAlias, Action<JoinBuilder> configure) => AddJoin(JoinType.Inner, tableAndAlias, configure);
        public SqlQuery<T> LeftJoin(string tableAndAlias, Action<JoinBuilder> configure) => AddJoin(JoinType.Left, tableAndAlias, configure);
        public SqlQuery<T> CrossApply(string expression, string alias)
        {
            _joins.Add(new SqlJoin { Type = JoinType.CrossApply, Table = expression, Alias = alias });
            return this;
        }

        private SqlQuery<T> AddJoin(JoinType type, string tableAndAlias, Action<JoinBuilder> configure)
        {
            var builder = new JoinBuilder();
            configure(builder);
            _joins.Add(new SqlJoin { Type = type, Table = tableAndAlias, Conditions = builder.GetConditions() });
            return this;
        }

        // --- PIPELINE OPERATORS ---
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

        public SqlQuery<T> Project(string rawProjection)
        {
            _projections.Add(rawProjection);
            return this;
        }

        public SqlQuery<T> Project(Action<ProjectBuilder> configure)
        {
            var builder = new ProjectBuilder();
            configure(builder);
            _projections.AddRange(builder.GetProjections());
            return this;
        }

        public SqlQuery<T> Scalar(string expression)
        {
            _projections.Clear();
            _projections.Add(expression);
            return this;
        }

        public SqlQuery<T> GroupBy(Action<GroupBuilder> configure)
        {
            var builder = new GroupBuilder();
            configure(builder);
            _groupings.AddRange(builder.GetColumns());
            return this;
        }

        public SqlQuery<T> Having(Action<SqlFilter> configure)
        {
            _havingFilter = new SqlFilter();
            configure(_havingFilter);
            return this;
        }

        public SqlQuery<T> UnionAll(Action<SqlQuery<object>> branch)
        {
            var subQuery = new SqlQuery<object>(_provider!);
            branch(subQuery);
            _setOperators.Add(new SqlUnion { Operator = "UNION ALL", Query = subQuery });
            return this;
        }

        // --- SORT & PAGE ---
        public SqlQuery<T> Sort(string column, string direction = "ASC") => OrderBy(column, direction);

        public SqlQuery<T> OrderBy(string column, string direction = "ASC")
        {
            _orderBy = string.IsNullOrWhiteSpace(column) ? "1" : column;
            _direction = string.Equals(direction, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            return this;
        }

        public SqlQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> expr, string direction = "ASC")
        {
            var member = expr.Body as MemberExpression;
            _orderBy = member?.Member.Name ?? "1"; 
            _direction = string.Equals(direction, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            return this;
        }

        public SqlQuery<T> Count(CountStrategy strategy)
        {
            _countStrategy = strategy;
            return this;
        }

        public SqlQuery<T> Page(int skip, int take, PaginationMode mode = PaginationMode.RowNumber)
        {
            _paged = true;
            _skip = skip < 0 ? 0 : skip;
            _take = take <= 0 ? 50 : take;
            _paginationMode = mode;
            return this;
        }

        public SqlQuery<T> Option(string rawOption)
        {
            _options.Add(rawOption);
            return this;
        }

        public SqlQuery<T> WithLog(LogLevel level, Action<string>? logger = null)
        {
            _logLevel = level;
            _logger = logger ?? (msg => Debug.WriteLine(msg));
            return this;
        }

        // --- COMPILATION ENGINE ---
        private sealed class CompiledQuery
        {
            public string MainSql { get; init; } = string.Empty;
            public string CountSql { get; init; } = string.Empty;
            public DynamicParameters Parameters { get; init; } = new();
        }

        internal (string Sql, DynamicParameters Parameters) CompileSubQuery(ref int paramIndex)
        {
            var execParams = new DynamicParameters();
            string sql = BuildCoreSelect(execParams, ref paramIndex);
            return (sql.TrimEnd(), execParams);
        }

       private CompiledQuery Compile() {
            if (_sourceType == SourceType.None)
                throw new InvalidOperationException("Source é obrigatório no pipeline.");

            var execParams = new DynamicParameters();
            int paramIndex = 0;
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_prepareScript))
            {
                sb.AppendLine(ApplyPushdowns(_prepareScript, execParams, ref paramIndex));
                sb.AppendLine();
            }

            string cteBlock = ApplyPushdowns(BuildCteBlock(), execParams, ref paramIndex);
            string coreSelect = BuildCoreSelect(execParams, ref paramIndex);

            if (_setOperators.Count > 0)
            {
                var unionParts = new List<string>();
                unionParts.Add(Compact(coreSelect));

                foreach (var union in _setOperators)
                {
                    var branchCompiled = union.Query.CompileSubQuery(ref paramIndex);
                    execParams.AddDynamicParams(branchCompiled.Parameters);
                    unionParts.Add(Compact(branchCompiled.Sql));
                }

                var unionSql = string.Join($" \n{_setOperators[0].Operator}\n ", unionParts);
                coreSelect = $"SELECT * FROM ({unionSql}) Q_UNION";
            }

            string countSql = string.Empty;
            if (_countStrategy == CountStrategy.Separate)
            {
                if (TryUnwrapCoreSelect(coreSelect, out var innerCountSelect, out var countWhere))
                {
                    countSql = $"{cteBlock}SELECT COUNT(*) FROM ({Compact(innerCountSelect)}) Q";
                    if (!string.IsNullOrWhiteSpace(countWhere))
                        countSql += $" WHERE {countWhere}";
                    else
                        countSql += " WHERE 1=1  ";
                }
                else
                {
                    countSql = $"{cteBlock}SELECT COUNT(*) FROM ({Compact(coreSelect)}) Q WHERE 1=1  ";
                }
            }

            sb.Append(BuildMainQueryString(cteBlock, coreSelect));

            string finalSql = sb.ToString();

            if (_paged && _provider != null)
            {
                execParams.Add(_provider.PaginationStartParam, _skip + 1, DbType.Int32);
                execParams.Add(_provider.PaginationEndParam, _skip + _take, DbType.Int32);
                execParams.Add(_provider.PaginationOffsetParam, _skip, DbType.Int32);
                execParams.Add(_provider.PaginationFetchParam, _take, DbType.Int32);
            }

            return new CompiledQuery
            {
                MainSql = finalSql,
                CountSql = countSql,
                Parameters = execParams
            };
        }

        private string BuildCoreSelect(DynamicParameters execParams, ref int paramIndex)
        {
            var sb = new StringBuilder();
            
            string selectList = _projections.Count > 0 ? string.Join(",\n    ", _projections) : "*";
            sb.AppendLine($"SELECT\n    {selectList}");

            string resolvedSource = ApplyPushdowns(_sourceSql, execParams, ref paramIndex);
            
            if (_sourceType == SourceType.Table || _sourceType == SourceType.Entity)
            {
                sb.AppendLine($"FROM {resolvedSource}");
            }
            else
            {
                var inlineSource = Compact(resolvedSource);
                sb.AppendLine($"FROM ({inlineSource}) Q");
            }

            foreach (var join in _joins)
            {
                string joinTypeStr = join.Type switch
                {
                    JoinType.Inner => "INNER JOIN",
                    JoinType.Left => "LEFT JOIN",
                    JoinType.Right => "RIGHT JOIN",
                    JoinType.CrossApply => "CROSS APPLY",
                    JoinType.OuterApply => "OUTER APPLY",
                    _ => "JOIN"
                };

                if (join.Type == JoinType.CrossApply || join.Type == JoinType.OuterApply)
                {
                    sb.AppendLine($"{joinTypeStr} ({Compact(join.Table)}) {join.Alias}");
                }
                else
                {
                    sb.AppendLine($"{joinTypeStr} {join.Table}");
                    if (join.Conditions.Any())
                    {
                        sb.AppendLine($"    ON {string.Join(" AND ", join.Conditions)}");
                    }
                }
            }

            var filterResult = SqlFilterRenderer.Instance.Render(_mainFilter, ref paramIndex);
            if (!string.IsNullOrWhiteSpace(filterResult.Sql))
            {
                execParams.AddDynamicParams(filterResult.Parameters);
                sb.AppendLine($"WHERE 1=1  AND {filterResult.Sql}");
            }

            if (_groupings.Count > 0)
            {
                sb.AppendLine($"GROUP BY\n    {string.Join(",\n    ", _groupings)}");
            }

            if (_havingFilter != null)
            {
                var havingResult = SqlFilterRenderer.Instance.Render(_havingFilter, ref paramIndex);
                if (!string.IsNullOrWhiteSpace(havingResult.Sql))
                {
                    execParams.AddDynamicParams(havingResult.Parameters);
                    sb.AppendLine($"HAVING\n    {havingResult.Sql}");
                }
            }

            return sb.ToString();
        }

        private string BuildCteBlock()
        {
            if (!string.IsNullOrWhiteSpace(_sourceCteBlock))
                return _sourceCteBlock + "\n";

            if (_ctes.Count == 0) return string.Empty;

            var cteParts = _ctes.Select(c => $"{c.Key} AS ({Compact(c.Value)})");
            return ";WITH " + string.Join(", ", cteParts) + "\n";
        }

        private static bool TryExtractCteBlock(string sql, out string cteBlock, out string sourceSql)
        {
            cteBlock = string.Empty;
            sourceSql = string.Empty;
            if (string.IsNullOrWhiteSpace(sql)) return false;

            sql = sql.Trim();
            if (sql.StartsWith(";")) sql = sql.Substring(1).TrimStart();
            if (!sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)) return false;

            int index = FindMainSelectIndex(sql);
            if (index <= 0) return false;

            cteBlock = ";" + sql.Substring(0, index).Trim();
            sourceSql = sql.Substring(index).Trim();
            return true;
        }

        private static int FindMainSelectIndex(string sql)
        {
            int depth = 0;
            bool inString = false;

            for (int i = 0; i < sql.Length - 5; i++)
            {
                char c = sql[i];
                if (c == '\'')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;
                if (c == '(') depth++;
                else if (c == ')') depth = Math.Max(0, depth - 1);

                if (depth == 0 && string.Equals(sql.Substring(i, 6), "SELECT", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string Compact(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

            var lines = Regex.Split(sql, "\r?\n");
            var cleanedLines = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            var compacted = string.Join(" ", cleanedLines);
            compacted = compacted.Replace("WHERE 1=1 AND", "WHERE 1=1  AND");
            return compacted.Trim();
        }

        private static bool TryUnwrapCoreSelect(string coreSelect, out string innerSelect, out string whereClause)
        {
            innerSelect = string.Empty;
            whereClause = string.Empty;
            if (string.IsNullOrWhiteSpace(coreSelect)) return false;

            var m = Regex.Match(coreSelect, @"SELECT\s*\*\s*FROM\s*\(\s*(SELECT[\s\S]*?)\s*\)\s*Q\s*(?:WHERE\s*(.*))?$", RegexOptions.IgnoreCase);
            if (!m.Success) return false;

            innerSelect = m.Groups[1].Value.Trim();
            whereClause = m.Groups.Count > 2 ? (m.Groups[2].Value ?? string.Empty).Trim() : string.Empty;
            return true;
        }

        private string BuildMainQueryString(string cteBlock, string coreSelect)
        {
            var sb = new StringBuilder();
            sb.Append(cteBlock);

            string safeOrder = QuoteIdentifier(_orderBy);
            string orderClause = $"ORDER BY {safeOrder} {_direction}";

            if (!_paged)
            {
                if (_countStrategy == CountStrategy.Window)
                {
                    if (TryUnwrapCoreSelect(coreSelect, out var innerSel, out var whereSel))
                    {
                        sb.Append($"SELECT *, COUNT(*) OVER() AS {SqlConstants.TotalCountColumn} FROM ({Compact(innerSel)}) Q");
                        if (!string.IsNullOrWhiteSpace(whereSel))
                            sb.Append($" WHERE {whereSel}");
                    }
                    else
                    {
                        sb.Append($"SELECT *, COUNT(*) OVER() AS {SqlConstants.TotalCountColumn} FROM ({Compact(coreSelect)}) Q");
                    }
                }
                else
                {
                    sb.Append(Compact(coreSelect));
                }

                // FIX A: dois espaços antes do ORDER BY para consistência com "WHERE 1=1  AND"
                // O espaço duplo separa visualmente a query do ORDER BY quando não há WHERE
                sb.Append("  ");
                sb.AppendLine(orderClause);

                if (_options.Count > 0) sb.AppendLine(string.Join("\n", _options));

                return sb.ToString();
            }

            if (_provider == null) throw new InvalidOperationException("Provider é obrigatório para consultas paginadas.");

            if (_paginationMode == PaginationMode.OffsetFetch)
            {
                if (_countStrategy == CountStrategy.Window)
                {
                    if (TryUnwrapCoreSelect(coreSelect, out var innerSelOff, out var whereSelOff))
                    {
                        sb.AppendLine("SELECT *");
                        sb.AppendLine("                    , COUNT(*) OVER() AS " + SqlConstants.TotalCountColumn);
                        sb.AppendLine($"              FROM ({Compact(innerSelOff)}) Q");
                        if (!string.IsNullOrWhiteSpace(whereSelOff))
                            sb.AppendLine($"              WHERE {whereSelOff}");
                    }
                    else
                    {
                        sb.AppendLine("SELECT *");
                        sb.AppendLine("                    , COUNT(*) OVER() AS " + SqlConstants.TotalCountColumn);
                        sb.AppendLine($"              FROM ({Compact(coreSelect)}) Q");
                    }
                }
                else
                {
                    string compacted = Compact(coreSelect);
                    
                    if (compacted.Contains(" FROM (") && compacted.Contains(") Q"))
                    {
                        sb.AppendLine(compacted);
                    }
                    else
                    {
                        sb.AppendLine("SELECT *");
                        sb.AppendLine();
                        sb.AppendLine($"              FROM ({compacted}) Q");
                    }
                }

                // FIX E: sem linha em branco extra antes do ORDER BY no OffsetFetch.
                // A linha em branco causava "Incorrect syntax near 'OFFSET'" no parser SQL.
                sb.AppendLine($"              {orderClause}");
                sb.AppendLine($"              OFFSET @{_provider.PaginationOffsetParam} ROWS FETCH NEXT @{_provider.PaginationFetchParam} ROWS ONLY");
            }
            else
            {
                // RowNumber mode
                sb.AppendLine("SELECT * FROM (");
                sb.AppendLine("                                    SELECT *,");
                sb.AppendLine($"                                           ROW_NUMBER() OVER ({orderClause}) AS RowNum");

                if (_countStrategy == CountStrategy.Window)
                {
                    sb.AppendLine($"                                           , COUNT(*) OVER() AS {SqlConstants.TotalCountColumn}");
                }
                else
                {
                    // FIX F: linha em branco após ROW_NUMBER quando não há COUNT(*) OVER,
                    // para manter consistência visual com o bloco que tem COUNT
                    sb.AppendLine("                                           ");
                }

                if (TryUnwrapCoreSelect(coreSelect, out var innerRowSel, out var whereRowSel))
                {
                    sb.AppendLine($"                                    FROM ({Compact(innerRowSel)}) Q");
                    if (!string.IsNullOrWhiteSpace(whereRowSel))
                    {
                        sb.AppendLine($"                                    WHERE {whereRowSel}");
                    }
                    else
                    {
                        // FIX D: linha em branco quando RowNumber sem WHERE
                        sb.AppendLine("                                    ");
                    }
                }
                else
                {
                    sb.AppendLine($"                                    FROM ({Compact(coreSelect)}) Q");
                    // FIX D: linha em branco quando RowNumber sem WHERE
                    sb.AppendLine("                                    ");
                }

                sb.AppendLine("                                ) X");
                sb.AppendLine($"                                WHERE RowNum BETWEEN @{_provider.PaginationStartParam} AND @{_provider.PaginationEndParam}");
            }

            if (_options.Count > 0) sb.AppendLine(string.Join("\n", _options));

            return sb.ToString();
        }

        private string ApplyPushdowns(string sql, DynamicParameters execParams, ref int paramIndex)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;
            
            string result = sql;
            foreach (var pd in _pushdowns)
            {
                var rr = SqlFilterRenderer.Instance.Render(pd.Value, ref paramIndex);
                if (rr.Parameters != null) execParams.AddDynamicParams(rr.Parameters);
                
                string replacement = string.Empty;
                if (!string.IsNullOrWhiteSpace(rr.Sql))
                {
                    var trimmedSql = rr.Sql.TrimStart();
                    if (trimmedSql.StartsWith("AND ", StringComparison.OrdinalIgnoreCase) || trimmedSql.StartsWith("OR ", StringComparison.OrdinalIgnoreCase))
                        replacement = trimmedSql;
                    else
                        replacement = $"AND {trimmedSql}";
                }
                result = result.Replace($"{{{{{pd.Key}}}}}", replacement);
            }
            return result;
        }

        private static string QuoteIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            if (Regex.IsMatch(name, "^[0-9]+$")) return name;
            if (name.Contains("[") || name.Contains("]") || name.Contains("(") || name.Contains(" ") || name.Contains(".") ) return name;
            return $"[{name}]";
        }

        // --- EXECUTION ---
        public async Task<QueryResult<T>> ExecuteAsync(SqlConnection connection, CancellationToken ct = default)
        {
            Stopwatch swPhase = Stopwatch.StartNew();

            CompiledQuery compiled = Compile();
            long buildMs = swPhase.ElapsedMilliseconds;

            if (_logLevel == LogLevel.Detailed) Log($"SQL:\n{compiled.MainSql}");

            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

            swPhase.Restart();

            var cmd = new CommandDefinition(compiled.MainSql, compiled.Parameters, cancellationToken: ct);
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
                    Timing = new QueryTiming { BuildMs = buildMs, ExecuteMs = executeMs, MapMs = mapMs }
                };
            }

            int total = await ResolveTotalAsync(connection, compiled, items, ct).ConfigureAwait(false);

            return new QueryResult<T>
            {
                Items = items,
                Total = total,
                IsPaged = _paged,
                Timing = new QueryTiming { BuildMs = buildMs, ExecuteMs = executeMs, MapMs = mapMs }
            };
        }

        private async Task<int> ResolveTotalAsync(SqlConnection connection, CompiledQuery compiled, List<T> items, CancellationToken ct)
        {
            if (_countStrategy == CountStrategy.None) return items.Count;

            if (_countStrategy == CountStrategy.Window)
            {
                var prop = typeof(T).GetProperty(SqlConstants.TotalCountColumn);
                if (prop != null && items.Count > 0) return Convert.ToInt32(prop.GetValue(items[0]));
                throw new InvalidOperationException($"CountStrategy.Window requer a propriedade '{SqlConstants.TotalCountColumn}' no DTO.");
            }

            if (_countStrategy == CountStrategy.Separate)
            {
                var cmd = new CommandDefinition(compiled.CountSql, compiled.Parameters, cancellationToken: ct);
                return await connection.ExecuteScalarAsync<int>(cmd).ConfigureAwait(false);
            }

            return items.Count;
        }

        // --- SQL GENERATION TO STRING ---
        public string ToSql()
        {
            CompiledQuery compiled = Compile();
            return MaterializeParameters(compiled.MainSql, compiled.Parameters);
        }

        public string ToSqlFull()
        {
            CompiledQuery compiled = Compile();
            var sb = new StringBuilder();
            sb.AppendLine("-- MAIN QUERY");
            sb.AppendLine(MaterializeParameters(compiled.MainSql, compiled.Parameters));

            if (_countStrategy == CountStrategy.Separate && !string.IsNullOrWhiteSpace(compiled.CountSql))
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

        private static string MaterializeParameters(string sql, DynamicParameters parameters)
        {
            if (parameters == null || parameters.ParameterNames == null) return sql;

            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in parameters.ParameterNames)
            {
                string paramKey = name.StartsWith("@") ? name : "@" + name;
                index[paramKey] = ToSqlLiteral(parameters.Get<object>(name));
            }

            var sb = new StringBuilder(sql.Length * 2);
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
                    if (index.TryGetValue(token, out string? value))
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

            if (val is byte or sbyte || val is short || val is ushort || val is int || val is uint || val is long || val is ulong || val is float || val is double || val is decimal)
                return Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";

            if (val is bool b) return b ? "1" : "0";
            if (val is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
            if (val is DateTimeOffset dto) return $"'{dto:yyyy-MM-dd HH:mm:ss.fff zzz}'";
            if (val is Guid g) return $"'{g}'";

            return $"'{val.ToString()!.Replace("'", "''")}'";
        }
    }
}