using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace W1E1.Core
{
    public interface IMetadataProvider
    {
        string GetTableName<T>();
        string GetColumnName<T>(Expression<Func<T, object>> prop);
        string GetSchema<T>();
        ColumnType GetColumnType<T>(Expression<Func<T, object>> prop);
        string GetFullyQualifiedName<T>();
    }

    public interface ISourceProvider
    {
        string BuildParameter(int index);
        string BuildIdentifier(string name);
        string BuildPagination(string coreSelect, string orderClause, PaginationMode mode, bool includeTotal);
    }

    public interface IExecutionEngine
    {
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters, CancellationToken ct = default);
        Task<int> ExecuteScalarAsync<T>(string sql, object parameters, CancellationToken ct = default);
    }

    public interface ISqlProvider : ISourceProvider
    {
        string PaginationStartParam { get; }
        string PaginationEndParam { get; }
        string PaginationOffsetParam { get; }
        string PaginationFetchParam { get; }
    }
}