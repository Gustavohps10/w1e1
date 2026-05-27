namespace W1E1.Core
{
    public sealed class SqlFilter
    {
        internal List<FilterAstNode> Nodes { get; } = new List<FilterAstNode>();
        private readonly Stack<string> _scopeAliases = new Stack<string>();

        private string ResolveColumn(string column)
        {
            if (string.IsNullOrWhiteSpace(column)) return column;
            if (column.Contains(".")) return column;
            if (_scopeAliases.Count == 0) return column;
            return $"{_scopeAliases.Peek()}.{column}";
        }

        public SqlFilter Scope(string alias, Action<SqlFilter> configure)
        {
            _scopeAliases.Push(alias);
            configure(this);
            if (_scopeAliases.Count > 0) _scopeAliases.Pop();
            return this;
        }

        public SqlFilter And(string expression, object? value = null)
        {
            if (value is null) return this;

            Nodes.Add(new FilterAstNode
            {
                NodeType = FilterNodeType.Condition,
                Operator = LogicalOperator.And,
                Expression = ResolveColumn(expression),
                Value = value
            });
            return this;
        }

        public SqlFilter AndIf(bool condition, string expression, object? value = null)
        {
            if (condition) And(expression, value);
            return this;
        }

        public SqlFilter Or(string expression, object? value = null)
        {
            if (value is null) return this;

            Nodes.Add(new FilterAstNode
            {
                NodeType = FilterNodeType.Condition,
                Operator = LogicalOperator.Or,
                Expression = ResolveColumn(expression),
                Value = value
            });
            return this;
        }

        public SqlFilter In<TVal>(string column, IEnumerable<TVal>? values)
        {
            if (values is null) return this;

            List<object> list = new List<object>();
            foreach (TVal val in values)
            {
                if (val != null) list.Add(val);
            }

            if (list.Count == 0) return this;

            Nodes.Add(new FilterAstNode
            {
                NodeType = FilterNodeType.In,
                Operator = LogicalOperator.And,
                Expression = ResolveColumn(column),
                Values = list
            });
            return this;
        }

        public SqlFilter NotIn<TVal>(string column, IEnumerable<TVal>? values)
        {
            if (values is null) return this;

            List<object> list = new List<object>();
            foreach (TVal val in values)
            {
                if (val != null) list.Add(val);
            }

            if (list.Count == 0) return this;

            Nodes.Add(new FilterAstNode
            {
                NodeType = FilterNodeType.NotIn,
                Operator = LogicalOperator.And,
                Expression = ResolveColumn(column),
                Values = list
            });
            return this;
        }

        public SqlFilter Search(string term, params string[] columns)
        {
            if (string.IsNullOrWhiteSpace(term)) return this;
            if (columns is null || columns.Length == 0) return this;

            string[] resolvedColumns = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                resolvedColumns[i] = ResolveColumn(columns[i]);
            }

            Nodes.Add(new FilterAstNode
            {
                NodeType = FilterNodeType.Search,
                Operator = LogicalOperator.And,
                Expression = term,
                Columns = resolvedColumns
            });
            return this;
        }

        public SqlFilter Raw(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return this;

            Nodes.Add(new FilterAstNode
            {
                NodeType = FilterNodeType.Raw,
                Operator = LogicalOperator.And,
                Expression = sql
            });
            return this;
        }

        public SqlFilter Group(Action<SqlFilter> configure)
        {
            return AppendGroup(LogicalOperator.And, configure);
        }

        public SqlFilter OrGroup(Action<SqlFilter> configure)
        {
            return AppendGroup(LogicalOperator.Or, configure);
        }

        private SqlFilter AppendGroup(LogicalOperator logicalOperator, Action<SqlFilter> configure)
        {
            SqlFilter child = new SqlFilter();
            string[] currentAliases = _scopeAliases.ToArray();
            for (int i = currentAliases.Length - 1; i >= 0; i--)
            {
                child._scopeAliases.Push(currentAliases[i]);
            }

            configure(child);

            if (child.Nodes.Count > 0)
            {
                Nodes.Add(new FilterAstNode
                {
                    NodeType = FilterNodeType.Group,
                    Operator = logicalOperator,
                    Children = child.Nodes
                });
            }
            return this;
        }
    }
}
