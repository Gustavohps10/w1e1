-- MAIN QUERY
;WITH base_origem AS (
                    SELECT
                        S.Chave_fato AS ChaveFatoOrigem,
                        S.Cod_filial AS CodFilialOrigem
                    FROM tbSaidas S
                    WHERE 1=1 AND S.Cod_filial IN (110,120)

                    UNION ALL

                    SELECT
                        E.Chave_fato AS ChaveFatoOrigem,
                        E.Cod_filial AS CodFilialOrigem
                    FROM tbEntradas E
                    WHERE 1=1 AND E.Cod_filial IN (110,120)
                ),

                final AS (
                    SELECT
                        ORIGEM.ChaveFatoOrigem,
                        ORIGEM.CodFilialOrigem,
                        CASE WHEN DESTINO.Chave_fato IS NOT NULL THEN 1 ELSE 0 END AS StatusDestino
                    FROM base_origem ORIGEM
                    LEFT JOIN tbSaidas DESTINO
                        ON DESTINO.Chave_fato_orig_un = ORIGEM.ChaveFatoOrigem
                )
SELECT * FROM (SELECT *
                FROM final F
                WHERE 1=1 AND StatusDestino = 0) Q  ORDER BY [ChaveFatoOrigem] DESC
