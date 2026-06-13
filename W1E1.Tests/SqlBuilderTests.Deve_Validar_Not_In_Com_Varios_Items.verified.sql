-- MAIN QUERY
SELECT * FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q WHERE 1=1  AND NumDoctoOrigem NOT IN (4,5,6,7)  ORDER BY 1 ASC

