using W1E1.Core;
using W1E1.SqlServer;
using W1E1.Tests.Helpers;

namespace W1E1.Tests
{
    internal sealed class DTODeListaGeracaoDeDocumentoDestino
    {
        public string ChaveFatoOrigem { get; set; } = string.Empty;
        public int NumDoctoOrigem { get; set; }
        public int StatusDestino { get; set; }
        public int TotalRegistros { get; set; }
    }

    [UsesVerify]
    public class SqlBuilderTests
    {
        private const string BaseSqlReal = "SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S";
        private const string CteSqlReal = "WITH CTE_Origem AS (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) SELECT * FROM CTE_Origem";

        [Fact]
        public async Task Deve_Gerar_Sql_Base()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Filtro()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.And("Q.StatusDestino", "F"));
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Paginacao_RowNumber()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .OrderBy(x => x.NumDoctoOrigem, "ASC")
                .Count(CountStrategy.Window)
                .Page(0, 50, PaginationMode.RowNumber);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Paginacao_OffsetFetch()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .OrderBy(x => x.NumDoctoOrigem, "DESC")
                .Count(CountStrategy.Separate)
                .Page(20, 10, PaginationMode.OffsetFetch);
            string sql = query.ToSqlFull();

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Filtro_E_Paginacao_RowNumber()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.And("Q.StatusDestino", "A"))
                .Count(CountStrategy.Window)
                .OrderBy(x => x.ChaveFatoOrigem, "ASC")
                .Page(10, 30, PaginationMode.RowNumber);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Filtro_E_Paginacao_OffsetFetch()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.And("Q.NumDoctoOrigem >", 1000))
                .Count(CountStrategy.Window)
                .OrderBy(x => x.ChaveFatoOrigem, "DESC")
                .Page(0, 100, PaginationMode.OffsetFetch);
            string sql = query.ToSqlFull();

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_CountSeparate()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Count(CountStrategy.Separate);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_CountWindow()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Count(CountStrategy.Window);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Filtro_Paginacao_RowNumber_E_CountSeparate()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.In("Q.StatusDestino", new string[] { "F", "A" }))
                .OrderBy(x => x.NumDoctoOrigem, "ASC")
                .Page(0, 15, PaginationMode.RowNumber)
                .Count(CountStrategy.Separate);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Base_Com_Filtro_Paginacao_OffsetFetch_E_CountWindow()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.Search("50", "Q.ChaveFatoOrigem"))
                .OrderBy(x => x.NumDoctoOrigem, "DESC")
                .Page(50, 25, PaginationMode.OffsetFetch)
                .Count(CountStrategy.Window);
            string sql = query.ToSqlFull();

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Cte()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).FromCte(CteSqlReal);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Cte_Com_Filtro()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).FromCte(CteSqlReal)
                .Filter(f => f.And("StatusDestino", "F"));
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Cte_Com_Paginacao_RowNumber()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).FromCte(CteSqlReal)
                .OrderBy(x => x.NumDoctoOrigem, "ASC")
                .Count(CountStrategy.Window)
                .Page(0, 10, PaginationMode.RowNumber);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Cte_Com_Paginacao_OffsetFetch()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).FromCte(CteSqlReal)
                .OrderBy("NumDoctoOrigem", "ASC")
                .Page(0, 10, PaginationMode.OffsetFetch);
            string sql = query.ToSqlFull();

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Cte_Com_Filtro_Paginacao_RowNumber_E_CountSeparate()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).FromCte(CteSqlReal)
                .Filter(f => f.And("NumDoctoOrigem >", 555))
                .OrderBy(x => x.ChaveFatoOrigem, "ASC")
                .Page(0, 20, PaginationMode.RowNumber)
                .Count(CountStrategy.Separate);
            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Com_Joins_Multiplos_E_Filtros_Complexos()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance)
                           .From(@"SELECT 
                                            S.Chave_fato AS ChaveFatoOrigem, 
                                            S.Num_docto AS NumDoctoOrigem, 
                                            S.Status AS StatusOrigem,
                                            S.Cod_filial as CodFilial                            
                                        FROM tbSaidas S
                                        INNER JOIN tbFilial FIL ON FIL.Cod_filial = S.Cod_filial
                                        LEFT JOIN tbTipoMvEstoque TMV ON TMV.Cod_tipo_mv = S.Cod_tipo_mv")
                           .Filter(f => f.Group(g => g
                               .And("Q.StatusOrigem", "A") // Obrigatório
                               .Group(sub => sub           // Agrupa os itens que dependem entre si
                                   .And("Q.NumDoctoOrigem", 104169)
                                   .Or("Q.NumDoctoOrigem", 104149)
                               )
                               .In("Q.CodFilial", new string[] { "001", "120", "110" })
                               .NotIn("Q.ChaveFatoOrigem", new string[] { "ZZZ012954", "ZZZ012953" })
                               .NotIn("Q.ChaveFatoOrigem", new string[] { "ZZZ011403" })
                               .Search("1403", "Q.ChaveFatoOrigem")
                           ))
                           .OrderBy("ChaveFatoOrigem", "DESC")
                           .Page(0, 100, PaginationMode.RowNumber)
                           .Count(CountStrategy.Window);

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Ignorar_In_Com_Lista_Vazia()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.In("Q.StatusDestino", new string[] { }));

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Misturar_Filtros_Raw_Com_Padrao()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance)
                .From("SELECT S.Data_movto  as DataMovtoOrigem, S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S")
                .Filter(f => f
                    .And("Q.StatusDestino", "A")
                    .Raw("DATEDIFF(day, Q.DataMovtoOrigem, GETDATE()) > 30")
                );

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Combinar_Multiplos_Search_Em_Campos_Diferentes()
        {
            // Valida se múltiplas chamadas de Search geram múltiplos blocos AND ( ... )
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance)
                .From("SELECT S.Data_movto  as DataMovtoOrigem, S.Cod_docto as CodDoctoOrigem, S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S")
                .Filter(f => f
                    .Search("001000039", "ChaveFatoOrigem")
                    .Search("PVE", "CodDoctoOrigem")
                );

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Estrutura_Booleana_Profunda()
        {
            // Cenário: AND (A AND (B OR C) AND (D OR E))
            // AND (Q.StatusDestino = 'A' AND (Q.NumDoctoOrigem = 4 OR Q.NumDoctoOrigem = 60922) AND (Q.CodFilial = '120' OR Q.CodFilial = '110')) ORDER BY 1 ASC
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance)
                .From("SELECT S.Chave_fato AS ChaveFatoOrigem, S.Cod_filial as CodFilial, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S")
                .Filter(f => f.Group(g => g
                    .And("Q.StatusDestino", "A")
                    .Group(b => b
                        .And("Q.NumDoctoOrigem", 4)
                        .Or("Q.NumDoctoOrigem", 60922)
                    )
                    .Group(c => c
                        .And("Q.CodFilial", "120")
                        .Or("Q.CodFilial", "110")
                    )
                ))
                .Count(CountStrategy.Window);

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Validar_Not_In_Com_Varios_Items()
        {
            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance).From(BaseSqlReal)
                .Filter(f => f.NotIn("NumDoctoOrigem", new int[] { 4, 5, 6, 7 }));

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);
            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Validar_Filtros_Em_Camadas_Com_Cte_Legada()
        {
            SqlFilter filtroSaida = new SqlFilter()
                .In("S.Cod_filial", new[] { 110, 120 })
                .And("S.Data_movto >=", new DateTime(2026, 01, 01));

            SqlFilter filtroEntrada = new SqlFilter()
                .In("E.Cod_filial", new[] { 110, 120 })
                .And("E.Data_movto >=", new DateTime(2026, 01, 01));

            SqlFilter filtroFinal = new SqlFilter()
                .Group(g => g
                    .And("StatusDestino", 0)
                    .Search("863", "ChaveFatoOrigem", "DescricaoTmvOrigem")
                );

            SqlQuery<DTODeListaGeracaoDeDocumentoDestino> query = new SqlQuery<DTODeListaGeracaoDeDocumentoDestino>(SqlServerProvider.Instance)
                    .FromCte(@"WITH base_origem AS (
                                SELECT
                                    'S' AS TipoDocumento,
                                    S.Chave_fato,
                                    S.Cod_filial,
                                    S.Data_movto
                                FROM tbSaidas S
                                WHERE 1=1 {{FiltroSaida}}

                                UNION ALL

                                SELECT
                                    'E' AS TipoDocumento,
                                    E.Chave_fato,
                                    E.Cod_filial,
                                    E.Data_movto
                                FROM tbEntradas E
                                WHERE 1=1 {{FiltroEntrada}}
                            ),

                            base_destino AS (
                                SELECT
                                    'S' AS TipoDocumento,
                                    S.Chave_fato,
                                    S.Chave_fato_orig_un
                                FROM tbSaidas S
                                WHERE S.Chave_fato_orig_un IS NOT NULL
                            ),

                            final AS (
                                SELECT
                                    ORIGEM.TipoDocumento AS TipoDocumentoOrigem,
                                    ORIGEM.Chave_fato AS ChaveFatoOrigem,
                                    ORIGEM.Cod_filial AS CodFilialOrigem,
                                    CASE
                                        WHEN DESTINO.Chave_fato IS NOT NULL THEN 1
                                        ELSE 0
                                    END AS StatusDestino,
                                    'TesteTmv' AS DescricaoTmvOrigem
                                FROM base_origem ORIGEM
                                LEFT JOIN base_destino DESTINO
                                    ON DESTINO.Chave_fato_orig_un = ORIGEM.Chave_fato
                            )

                            SELECT *
                            FROM final F
                            ")
                    .Pushdown("FiltroSaida", filtroSaida)
                    .Pushdown("FiltroEntrada", filtroEntrada)
                    .Filter(filtroFinal)
                    .OrderBy("ChaveFatoOrigem", "DESC")
                    .Page(0, 50, PaginationMode.RowNumber)
                    .Count(CountStrategy.Window);

            string sql = query.ToSqlFull();

            SqlAssert.Validate(sql);

            await Verify(sql, extension: "sql");
        }

        [Fact]
        public async Task Deve_Gerar_Sql_Cte_Com_Pushdown_Final_Com_Agregacao_Sum_Count_E_Where_Apos_Alias()
        {
            SqlFilter filtroSaida = new SqlFilter()
                .In("S.Cod_filial", new[] { 110, 120 });

            SqlFilter filtroEntrada = new SqlFilter()
                .In("E.Cod_filial", new[] { 110, 120 });

            SqlFilter filtroFinal = new SqlFilter()
                .And("StatusDestino", 1);

            string sql = @"
                        WITH base_origem AS (
                            SELECT
                                S.Chave_fato AS ChaveFatoOrigem,
                                S.Cod_filial AS CodFilialOrigem,
                                S.Valor_liquido AS ValorOrigem
                            FROM tbSaidas S
                            WHERE 1=1 {{filtroSaida}}

                            UNION ALL

                            SELECT
                                E.Chave_fato AS ChaveFatoOrigem,
                                E.Cod_filial AS CodFilialOrigem,
                                E.Valor_liquido AS ValorOrigem
                            FROM tbEntradas E
                            WHERE 1=1 {{filtroEntrada}}
                        ),

                        base_destino AS (
                            SELECT
                                S.Chave_fato,
                                S.Chave_fato_orig_un
                            FROM tbSaidas S
                            WHERE S.Chave_fato_orig_un IS NOT NULL
                        ),

                        final AS (
                            SELECT
                                ORIGEM.ChaveFatoOrigem,
                                ORIGEM.CodFilialOrigem,
                                ORIGEM.ValorOrigem,
                                CASE
                                    WHEN DESTINO.Chave_fato IS NOT NULL THEN 1
                                    ELSE 0
                                END AS StatusDestino
                            FROM base_origem ORIGEM
                            LEFT JOIN base_destino DESTINO
                                ON DESTINO.Chave_fato_orig_un = ORIGEM.ChaveFatoOrigem
                        )

                        SELECT
                            ISNULL(SUM(F.ValorOrigem), 0) AS ValorTotalOrigens,
                            COUNT(*) AS TotalDeDocumentosNoLote
                        FROM final F
                        WHERE 1=1 {{filtroFinal}}
                    ";

            SqlQuery<object> query =
                new SqlQuery<object>(SqlServerProvider.Instance)
                    .FromCte(sql)
                    .Pushdown("filtroSaida", filtroSaida)
                    .Pushdown("filtroEntrada", filtroEntrada)
                    .Pushdown("filtroFinal", filtroFinal);

            string result = query.ToSqlFull();

            SqlAssert.Validate(result);

            await Verify(result, extension: "sql");
        }

        [Fact]
        public async Task Deve_Garantir_Integridade_Do_Pipeline_Complexo_De_Tmv()
        {
            int offset = 0;
            int limit = 50;
            string filtroLike = "%TESTE%";
            string orderDir = "ASC";
            DateTime dataParaBusca = new DateTime(2026, 01, 01);

            SqlQuery<object> query = new SqlQuery<object>(SqlServerProvider.Instance)
                .Prepare(@"
                    IF OBJECT_ID('tempdb..#CFOP_INCIDENCIA') IS NOT NULL DROP TABLE #CFOP_INCIDENCIA;
                    IF OBJECT_ID('tempdb..#TMV_COM_IMPOSTOS') IS NOT NULL DROP TABLE #TMV_COM_IMPOSTOS;
                    IF OBJECT_ID('tempdb..#TMV_COM_INCIDENCIA_RECPAG') IS NOT NULL DROP TABLE #TMV_COM_INCIDENCIA_RECPAG;
                    IF OBJECT_ID('tempdb..#TMV_COM_INCIDENCIA_CFOP') IS NOT NULL DROP TABLE #TMV_COM_INCIDENCIA_CFOP;
                    IF OBJECT_ID('tempdb..#TMV_COM_INCIDENCIA_TMVFIXO') IS NOT NULL DROP TABLE #TMV_COM_INCIDENCIA_TMVFIXO;
                    IF OBJECT_ID('tempdb..#TMV_SUGERIDO_FINAL') IS NOT NULL DROP TABLE #TMV_SUGERIDO_FINAL;

                    CREATE TABLE #CFOP_INCIDENCIA (
                        COD_CFOP CHAR(5) COLLATE DATABASE_DEFAULT PRIMARY KEY
                    );

                    INSERT INTO #CFOP_INCIDENCIA (COD_CFOP) VALUES ('5101'),('5102'),('1101'),('1102');

                    SELECT COD_TIPO_MV INTO #TMV_COM_IMPOSTOS FROM TBIMPOSTOMVE WITH (NOLOCK) WHERE 1=1 {{FiltroImpostos}} GROUP BY COD_TIPO_MV;
                    CREATE CLUSTERED INDEX IX_TEMP_IMPOSTOS ON #TMV_COM_IMPOSTOS (COD_TIPO_MV);

                    SELECT COD_TIPO_MV INTO #TMV_COM_INCIDENCIA_RECPAG FROM (
                        SELECT T.COD_TIPO_MV FROM #TMV_COM_IMPOSTOS IMP
                        INNER JOIN TBTIPOMVESTOQUE T WITH (NOLOCK) ON T.COD_TIPO_MV = IMP.COD_TIPO_MV WHERE 1=1 {{FiltroRecPag}}
                    ) AS FonteDeIncidencia GROUP BY COD_TIPO_MV;
                    CREATE CLUSTERED INDEX IX_TEMP_INCIDENCIA ON #TMV_COM_INCIDENCIA_RECPAG (COD_TIPO_MV);

                    SELECT FonteTMVCFOPPrincipal.COD_TIPO_MV INTO #TMV_COM_INCIDENCIA_CFOP FROM (
                        SELECT FonteTMVCFOPAgrupados.COD_TIPO_MV, FonteTMVCFOPAgrupados.COD_CFOP, FonteTMVCFOPAgrupados.Qtde,
                            ROW_NUMBER() OVER (PARTITION BY FonteTMVCFOPAgrupados.COD_TIPO_MV ORDER BY FonteTMVCFOPAgrupados.Qtde DESC) AS Row
                        FROM (
                            SELECT FonteTMVCFOP.COD_TIPO_MV, FonteTMVCFOP.COD_CFOP, COUNT(FonteTMVCFOP.Num_item) Qtde FROM (
                                SELECT D.COD_TIPO_MV, DI.COD_CFOP COLLATE DATABASE_DEFAULT AS COD_CFOP, DI.Num_item FROM TBSAIDAS D WITH (NOLOCK)
                                INNER JOIN TBSAIDASITEM DI WITH (NOLOCK) ON DI.CHAVE_FATO = D.CHAVE_FATO
                                INNER JOIN #TMV_COM_IMPOSTOS IMP ON D.COD_TIPO_MV = IMP.COD_TIPO_MV WHERE 1=1 {{FiltroPushdownSaidas}}
                                UNION
                                SELECT D.COD_TIPO_MV, DI.COD_CFOP COLLATE DATABASE_DEFAULT AS COD_CFOP, DI.Num_item FROM TBENTRADAS D WITH (NOLOCK)
                                INNER JOIN TBENTRADASITEM DI WITH (NOLOCK) ON DI.CHAVE_FATO = D.CHAVE_FATO
                                INNER JOIN #TMV_COM_IMPOSTOS IMP ON D.COD_TIPO_MV = IMP.COD_TIPO_MV WHERE 1=1 {{FiltroPushdownEntradas}}
                            ) FonteTMVCFOP GROUP BY FonteTMVCFOP.COD_TIPO_MV, FonteTMVCFOP.COD_CFOP
                        ) FonteTMVCFOPAgrupados
                    ) FonteTMVCFOPPrincipal WHERE FonteTMVCFOPPrincipal.ROW = 1 AND FonteTMVCFOPPrincipal.COD_CFOP COLLATE DATABASE_DEFAULT IN (SELECT COD_CFOP COLLATE DATABASE_DEFAULT FROM #CFOP_INCIDENCIA) GROUP BY FonteTMVCFOPPrincipal.COD_TIPO_MV;
                    CREATE CLUSTERED INDEX IX_TEMP_INCIDENCIA ON #TMV_COM_INCIDENCIA_CFOP (COD_TIPO_MV);

                    SELECT T.COD_TIPO_MV INTO #TMV_COM_INCIDENCIA_TMVFIXO FROM TBTIPOMVESTOQUE T WITH (NOLOCK) INNER JOIN #TMV_COM_IMPOSTOS IMP ON T.COD_TIPO_MV = IMP.COD_TIPO_MV WHERE 1=1 {{FiltroTmvFixo}};
                    CREATE CLUSTERED INDEX IX_TEMP_INCIDENCIA ON #TMV_COM_INCIDENCIA_TMVFIXO (COD_TIPO_MV);

                    SELECT IMP.COD_TIPO_MV, CAST(CASE WHEN ISNULL(ISNULL(INC_RECPAG.COD_TIPO_MV, INC_CFOP.COD_TIPO_MV), INC_TMV.COD_TIPO_MV) IS NOT NULL THEN 1 ELSE 2 END AS TINYINT) AS IND_INCIDENCIA_CBSIBS_SUGERIDO INTO #TMV_SUGERIDO_FINAL FROM #TMV_COM_IMPOSTOS AS IMP
                    LEFT JOIN #TMV_COM_INCIDENCIA_RECPAG AS INC_RECPAG ON IMP.COD_TIPO_MV = INC_RECPAG.COD_TIPO_MV
                    LEFT JOIN #TMV_COM_INCIDENCIA_CFOP AS INC_CFOP ON IMP.COD_TIPO_MV = INC_CFOP.COD_TIPO_MV
                    LEFT JOIN #TMV_COM_INCIDENCIA_TMVFIXO AS INC_TMV ON IMP.COD_TIPO_MV = INC_TMV.COD_TIPO_MV;
                    CREATE CLUSTERED INDEX IX_TEMP_SUGERIDO_FINAL ON #TMV_SUGERIDO_FINAL (COD_TIPO_MV);
                ")
                .FromCte(@"
                    SELECT T.COD_TIPO_MV AS CodTipoMv, T.DESCRICAO AS Descricao, CAST(T.IND_INCIDENCIA_CBSIBS AS TINYINT) AS IndIncidenciaCBSIBS, S.IND_INCIDENCIA_CBSIBS_SUGERIDO AS IndIncidenciaCBSIBSSugerido FROM TBTIPOMVESTOQUE T WITH (NOLOCK)
                    INNER JOIN #TMV_SUGERIDO_FINAL S ON S.COD_TIPO_MV = T.COD_TIPO_MV WHERE 1=1 {{FiltroFinalPipeline}}
                ")
                .Pushdown("FiltroImpostos", f => f.In("COD_IMPOSTO", new string[] { "IBSUF", "IBSMUN", "CBS" }))
                .Pushdown("FiltroRecPag", f => f.In("T.GERA_PAGAR_RECEBER", new string[] { "R", "P" }))
                .Pushdown("FiltroPushdownSaidas", f => f.And("D.DATA_V1 >=", dataParaBusca).Raw("ISNULL(D.STATUS,'') <> 'C'").Raw("DI.COD_CFOP IS NOT NULL"))
                .Pushdown("FiltroPushdownEntradas", f => f.And("D.DATA_V1 >=", dataParaBusca).Raw("ISNULL(D.STATUS,'') <> 'C'").Raw("DI.COD_CFOP IS NOT NULL"))
                .Pushdown("FiltroTmvFixo", f => f.Raw("(LTRIM(RTRIM(T.CFOP_UF_PROD)) COLLATE DATABASE_DEFAULT IN (SELECT COD_CFOP COLLATE DATABASE_DEFAULT FROM #CFOP_INCIDENCIA)) OR (LTRIM(RTRIM(T.CFOP_UF_SERV)) COLLATE DATABASE_DEFAULT IN (SELECT COD_CFOP COLLATE DATABASE_DEFAULT FROM #CFOP_INCIDENCIA))"))
                .Filter(f => f.Group(g => g.Raw("T.COD_TIPO_MV LIKE @filtroLike COLLATE DATABASE_DEFAULT OR T.DESCRICAO LIKE @filtroLike COLLATE DATABASE_DEFAULT")))
                .OrderBy("teste", orderDir)
                .Count(CountStrategy.Window)
                .Page(offset, limit, PaginationMode.RowNumber);

            string sqlCompleto = query.ToSqlFull();

            SqlAssert.Validate(sqlCompleto);
            await Verify(sqlCompleto, extension: "sql");
        }
    }
}