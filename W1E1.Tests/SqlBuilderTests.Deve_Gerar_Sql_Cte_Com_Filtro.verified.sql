-- MAIN QUERY
;WITH CTE_Origem AS (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S)
SELECT * FROM (SELECT * FROM CTE_Origem) Q WHERE 1=1  AND StatusDestino = 'F' ORDER BY 1 ASC
