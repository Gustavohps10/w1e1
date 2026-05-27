-- MAIN QUERY
;WITH CTE_Origem AS (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S)
SELECT *
                    
              FROM (SELECT * FROM CTE_Origem) Q
              
              ORDER BY [NumDoctoOrigem] ASC
              OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
