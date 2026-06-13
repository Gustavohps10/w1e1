-- MAIN QUERY
SELECT *, COUNT(*) OVER() AS TotalRegistros FROM (SELECT S.Chave_fato AS ChaveFatoOrigem, S.Cod_filial as CodFilial, S.Num_docto AS NumDoctoOrigem, S.Status AS StatusDestino FROM tbSaidas S) Q WHERE 1=1  AND (Q.StatusDestino = 'A' AND (Q.NumDoctoOrigem = 4 OR Q.NumDoctoOrigem = 60922) AND (Q.CodFilial = '120' OR Q.CodFilial = '110'))  ORDER BY 1 ASC

