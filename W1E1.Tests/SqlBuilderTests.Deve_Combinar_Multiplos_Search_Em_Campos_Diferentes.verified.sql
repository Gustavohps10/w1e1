-- MAIN QUERY
SELECT * FROM (SELECT S.Data_movto  as DataMovtoOrigem, S.Cod_docto as CodDoctoOrigem, S.Chave_fato AS ChaveFatoOrigem, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q WHERE 1=1  AND (ChaveFatoOrigem LIKE '%001000039%') AND (CodDoctoOrigem LIKE '%PVE%')  ORDER BY 1 ASC

