-- MAIN QUERY
SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY [ChaveFatoOrigem] ASC) AS RowNum
                                           , COUNT(*) OVER() AS TotalRegistros
                                    FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q
                                    WHERE 1=1  AND Q.StatusDestino = 'A'
                                ) X
                                WHERE RowNum BETWEEN 11 AND 40
