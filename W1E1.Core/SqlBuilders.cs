using System;
using System.Collections.Generic;

namespace W1E1.Core
{
    public sealed class CteBuilder
    {
        private readonly Dictionary<string, string> _ctes = new();
        
        public CteBuilder With(string name, string sql)
        {
            _ctes[name] = sql;
            return this;
        }
        
        internal IReadOnlyDictionary<string, string> GetCtes() => _ctes;
    }

    public sealed class JoinBuilder
    {
        private readonly List<string> _conditions = new();
        
        public JoinBuilder On(string leftColumn, string rightColumn)
        {
            _conditions.Add($"{leftColumn} = {rightColumn}");
            return this;
        }

        public JoinBuilder And(string leftColumn, string rightColumn)
        {
            _conditions.Add($"{leftColumn} = {rightColumn}");
            return this;
        }
        
        internal IEnumerable<string> GetConditions() => _conditions;
    }

    public sealed class ProjectBuilder
    {
        private readonly List<string> _columns = new();
        
        public ProjectBuilder Column(string expression, string alias = "")
        {
            _columns.Add(string.IsNullOrWhiteSpace(alias) ? expression : $"{expression} AS {alias}");
            return this;
        }

        public ProjectBuilder Aggregate(string expression, string alias) => Column(expression, alias);

        public ProjectBuilder Window(Action<WindowBuilder> configure)
        {
            var wb = new WindowBuilder();
            configure(wb);
            _columns.Add(wb.Build());
            return this;
        }
        
        internal IEnumerable<string> GetProjections() => _columns;
    }

    public sealed class WindowBuilder
    {
        private string _function = string.Empty;
        private string _partitionBy = string.Empty;
        private string _orderBy = string.Empty;
        private string _alias = string.Empty;

        public WindowBuilder RowNumber() { _function = "ROW_NUMBER()"; return this; }
        public WindowBuilder DenseRank() { _function = "DENSE_RANK()"; return this; }
        public WindowBuilder Rank() { _function = "RANK()"; return this; }
        public WindowBuilder Aggregate(string agg) { _function = agg; return this; }
        
        public WindowBuilder PartitionBy(string columns) { _partitionBy = $"PARTITION BY {columns}"; return this; }
        public WindowBuilder OrderBy(string clause) { _orderBy = $"ORDER BY {clause}"; return this; }
        public WindowBuilder As(string alias) { _alias = alias; return this; }

        internal string Build()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_partitionBy)) parts.Add(_partitionBy);
            if (!string.IsNullOrWhiteSpace(_orderBy)) parts.Add(_orderBy);
            
            string overClause = parts.Count > 0 ? string.Join(" ", parts) : string.Empty;
            return $"{_function} OVER({overClause}) {_alias}".Trim();
        }
    }

    public sealed class GroupBuilder
    {
        private readonly List<string> _columns = new();
        
        public GroupBuilder Column(string expression)
        {
            _columns.Add(expression);
            return this;
        }
        
        internal IEnumerable<string> GetColumns() => _columns;
    }
}