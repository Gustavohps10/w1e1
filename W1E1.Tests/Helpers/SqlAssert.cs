using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W1E1.Tests.Helpers
{
    public static class SqlAssert
    {
        public static void Validate(string sql)
        {
            TSql100Parser parser = new(false);

            using StringReader reader = new(sql);
            parser.Parse(reader, out IList<ParseError> errors);

            if (errors.Any())
            {
                string msg = string.Join("\n", errors.Select(e => $"Linha {e.Line}, Coluna {e.Column}: {e.Message}"));
                throw new Exception("SQL inválido:\n" + msg + $"\n{sql}");
            }
        }
    }
}
