-- MAIN QUERY
SELECT *
                    
              FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q
              
              ORDER BY [NumDoctoOrigem] DESC
              OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY

-- COUNT QUERY
SELECT COUNT(*) FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q WHERE 1=1  
