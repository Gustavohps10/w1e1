-- MAIN QUERY
;WITH base_origem AS (
                                SELECT
                                    'S' AS TipoDocumento,
                                    S.Chave_fato,
                                    S.Cod_filial,
                                    S.Data_movto
                                FROM tbSaidas S
                                WHERE 1=1 AND S.Cod_filial IN (110,120) AND S.Data_movto >= '2026-01-01 00:00:00.000'

                                UNION ALL

                                SELECT
                                    'E' AS TipoDocumento,
                                    E.Chave_fato,
                                    E.Cod_filial,
                                    E.Data_movto
                                FROM tbEntradas E
                                WHERE 1=1 AND E.Cod_filial IN (110,120) AND E.Data_movto >= '2026-01-01 00:00:00.000'
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
SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY [ChaveFatoOrigem] DESC) AS RowNum
                                           , COUNT(*) OVER() AS TotalRegistros
                                    FROM (SELECT * FROM final F) Q
                                    WHERE 1=1  AND (StatusDestino = 0 AND (ChaveFatoOrigem LIKE '%863%' OR DescricaoTmvOrigem LIKE '%863%'))
                                ) X
                                WHERE RowNum BETWEEN 1 AND 50

