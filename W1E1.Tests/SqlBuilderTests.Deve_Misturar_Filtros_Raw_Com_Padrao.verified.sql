-- MAIN QUERY
SELECT * FROM (SELECT S.Data_movto  as DataMovtoOrigem, S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q WHERE 1=1  AND Q.StatusDestino = 'A' AND DATEDIFF(day, Q.DataMovtoOrigem, GETDATE()) > 30 ORDER BY 1 ASC
