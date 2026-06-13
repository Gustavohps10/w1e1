-- MAIN QUERY
SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY [NumDoctoOrigem] ASC) AS RowNum
                                           , COUNT(*) OVER() AS TotalRegistros
                                    FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q
                                    
                                ) X
                                WHERE RowNum BETWEEN 1 AND 50

