-- MAIN QUERY
SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY [NumDoctoOrigem] ASC) AS RowNum
                                           
                                    FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q
                                    WHERE 1=1  AND Q.StatusDestino IN ('F','A')
                                ) X
                                WHERE RowNum BETWEEN 1 AND 15


-- COUNT QUERY
SELECT COUNT(*) FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q WHERE 1=1  AND Q.StatusDestino IN ('F','A')

