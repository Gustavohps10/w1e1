using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace W1E1.Tests.Helpers;

public static class SqlAssert
{
    private enum SqlVersion
    {
        Sql100 = 100,
        Sql110 = 110
    }

    public static void Validate(string sql, int version = 100)
    {
        TSqlParser parser = ((SqlVersion)version) switch
        {
            SqlVersion.Sql110 => new TSql110Parser(false),
            _ => new TSql100Parser(false)
        };

        using StringReader reader = new(sql);
        parser.Parse(reader, out IList<ParseError> errors);

        if (errors.Any())
        {
            string msg = string.Join("\n", errors.Select(e => $"Linha {e.Line}, Coluna {e.Column}: {e.Message}"));
            throw new Exception("SQL inválido:\n" + msg + $"\n{sql}");
        }
    }
}