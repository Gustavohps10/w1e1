-- MAIN QUERY
SELECT *
                    , COUNT(*) OVER() AS TotalRegistros
              FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q
              WHERE 1=1  AND Q.ChaveFatoOrigem LIKE '%50%'
              ORDER BY [NumDoctoOrigem] DESC
              OFFSET 50 ROWS FETCH NEXT 25 ROWS ONLY

