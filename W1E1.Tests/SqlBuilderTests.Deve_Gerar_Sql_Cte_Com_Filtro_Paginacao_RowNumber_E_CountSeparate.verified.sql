-- MAIN QUERY
;WITH CTE_Origem AS (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S)
SELECT * FROM (
                                    SELECT *,
                                           ROW_NUMBER() OVER (ORDER BY [ChaveFatoOrigem] ASC) AS RowNum
                                           
                                    FROM (SELECT * FROM CTE_Origem) Q
                                    WHERE 1=1  AND NumDoctoOrigem > 555
                                ) X
                                WHERE RowNum BETWEEN 1 AND 20

-- COUNT QUERY
;WITH CTE_Origem AS (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S)
SELECT COUNT(*) FROM (SELECT * FROM CTE_Origem) Q WHERE 1=1  AND NumDoctoOrigem > 555
