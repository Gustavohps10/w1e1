-- MAIN QUERY
SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY [ChaveFatoOrigem] DESC) AS RowNum
                                           , COUNT(*) OVER() AS TotalRegistros
                                    FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusOrigem, S.Cod_filial as CodFilial FROM tbSaidas S INNER JOIN tbFilial FIL ON FIL.Cod_filial = S.Cod_filial LEFT JOIN tbTipoMvEstoque TMV ON TMV.Cod_tipo_mv = S.Cod_tipo_mv) Q
                                    WHERE 1=1  AND (Q.StatusOrigem = 'A' AND (Q.NumDoctoOrigem = 104169 OR Q.NumDoctoOrigem = 104149) AND Q.CodFilial IN ('001','120','110') AND Q.ChaveFatoOrigem NOT IN ('ZZZ012954','ZZZ012953','ZZZ011403') AND Q.ChaveFatoOrigem LIKE '%1403%')
                                ) X
                                WHERE RowNum BETWEEN 1 AND 100

