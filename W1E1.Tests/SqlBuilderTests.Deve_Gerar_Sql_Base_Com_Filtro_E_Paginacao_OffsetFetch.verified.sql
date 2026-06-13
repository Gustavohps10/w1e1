-- MAIN QUERY
SELECT *
                    , COUNT(*) OVER() AS TotalRegistros
              FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q
              WHERE 1=1  AND Q.NumDoctoOrigem > 1000
              ORDER BY [ChaveFatoOrigem] DESC
              OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY

