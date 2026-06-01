using Microsoft.Data.SqlClient;
using W1E1.SqlServer;

namespace W1E1.Core.SqlQueryBuilder.Extensions
{
    public static class SqlConnectionExtensions
    {
        /// <summary>
        /// Executa uma query fluente e retorna QueryResult&lt;T&gt;.
        /// </summary>
        /// <example>
        /// await conn.QueryResultAsync&lt;UsuarioDto&gt;(q => q
        ///     .From(sql)
        ///     .Filter(f => f.Search("gustavo", "Nome"))
        ///     .OrderBy(x => x.Nome)
        ///     .Page(0, 20)
        ///     .Count(CountStrategy.Separate));
        /// </example>
        public static Task<QueryResult<T>> QueryResultAsync<T>(this SqlConnection connection,Action<SqlQuery<T>> configure,  CancellationToken ct = default) where T : new()
        {
            SqlQuery<T> query = new(SqlServerProvider.Instance);
            configure(query);
            return query.ExecuteAsync(connection, ct);
        }
    }
}
