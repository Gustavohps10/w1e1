-- MAIN QUERY
;WITH base_origem AS (
                            SELECT
                                S.Chave_fato AS ChaveFatoOrigem,
                                S.Cod_filial AS CodFilialOrigem,
                                S.Valor_liquido AS ValorOrigem
                            FROM tbSaidas S
                            WHERE 1=1 AND S.Cod_filial IN (110,120)

                            UNION ALL

                            SELECT
                                E.Chave_fato AS ChaveFatoOrigem,
                                E.Cod_filial AS CodFilialOrigem,
                                E.Valor_liquido AS ValorOrigem
                            FROM tbEntradas E
                            WHERE 1=1 AND E.Cod_filial IN (110,120)
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
SELECT * FROM (SELECT
                            ISNULL(SUM(F.ValorOrigem), 0) AS ValorTotalOrigens,
                            COUNT(*) AS TotalDeDocumentosNoLote
                        FROM final F
                        WHERE 1=1 AND StatusDestino = 1) Q  ORDER BY 1 ASC
