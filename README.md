# W1E1 Query DSL - Structured SQL Pipeline

## Objetivo

A W1E1 Query DSL foi criada para organizar SQL complexo utilizando pipelines estruturados, mantendo SQL como cidadão de primeira classe.

### Objetivos principais

- SQL semi-puro
- Curva de aprendizado baixa
- Fácil leitura por analistas
- Fluxo previsível
- Compile-time guidance via IntelliSense
- Menos erro humano
- Permitir queries extremamente complexas sem virar ORM

### Princípios

```
Menos magia
Mais previsibilidade
Mais SQL
Mais manutenção
```

---

## Filosofia

A DSL **NÃO** tenta esconder SQL. Ela tenta:

```
Organizar SQL
↓
Padronizar fluxo
↓
Guiar construção
↓
Reduzir erro humano
↓
Permitir otimizações futuras
```

O modelo mental é:

```
Query Pipeline
=
Graph de datasets + transformações
```

---

## Fluxo Geral

```
Prepare?
   ↓
Pushdown*
   ↓
Source (obrigatório)
   ↓
Join / Apply*
   ↓
Filter?
   ↓
Project?
   ↓
GroupBy?
   ↓
Having?
   ↓
Window?
   ↓
Set Operators?
   ↓
Global Filters?
   ↓
Sort?
   ↓
Count?
   ↓
Page?
   ↓
Options?
   ↓
ToSql()
```

---

## Regras de Fluxo

- `Prepare` → pode existir apenas antes do `Source`
- `Source` é **obrigatório**
- Somente **um** Source principal:
  - ✔ `FromTable()`
  - ✔ `FromRaw()`
  - ✔ `FromCte()`
  - ✔ `From()`
- Após Source: `Join`, `Filter`, `Group`, `Union`, `Page`
- **Não permitido:**
  ```
  FromTable()
  FromRaw()
  ```
  ou
  ```
  FromTable()
  From()
  ```

---

## Source Types

A DSL possui múltiplos modos de origem.

### `FromTable()`

Modo estruturado. Ideal para joins controlados, pipeline otimizado e compile-time flow.

```csharp
new SqlQuery<DTO>(provider)
    .FromTable("TBMOVIMENTO M")
```

### `FromRaw()`

Modo SQL manual clássico. Ideal para consultas analíticas simples, uso por analistas, SQL existente e migração gradual.

```csharp
new SqlQuery<DTO>(provider)
    .FromRaw(@"
        SELECT *
        FROM TBMOVIMENTO
    ")
```

### `From()`

Modo Híbrido / Bloco Livre de Alta Complexidade. Ideal para múltiplas CTEs manuais ou subqueries complexas aninhadas diretamente, isolando blocos brutos complexos e garantindo que o pipeline gerencie filtros externos e paginação sem quebrar o escopo.

```csharp
new SqlQuery<DTO>(provider)
    .From(@"
        WITH origem AS (
            SELECT D.CHAVE_FATO, D.NUM_DOCTO FROM #TMP D
        )
        SELECT * FROM origem Q
    ")
```

### `FromCte()`

CTEs estruturadas via Fluent Builder.

```csharp
new SqlQuery<DTO>(provider)
    .FromCte(cte => cte
        .With("movs", @"SELECT * FROM TBMOVIMENTO")
        .With("base", @"SELECT * FROM movs"))
```

---

## Pushdowns

Pushdowns permitem reutilização de lógica.

```csharp
var filtroPeriodo =
    new SqlFilter()
        .Where("M.DATA >=", inicio)
        .And("M.DATA <=", fim);

query.Pushdown("Periodo", filtroPeriodo);
```

SQL gerado:

```sql
WHERE 1=1 {{Periodo}}
```

---

## Filtros

**Regra:** Todo grupo inicia com `Where()`. Após o primeiro `Where()`, somente: `And()`, `Or()`, subgrupos e expressões de texto puro (raw expressions).

```csharp
.Filter(f => f
    .Where("STATUS", 1)
    .And(group => group
        .Where("FILIAL", "110")
        .Or("FILIAL", "120")))
```

---

## Joins

Suporta: `Inner Join`, `Left Join`, `Right Join`, `Full Join`, `Cross Apply`, `Outer Apply`.

```csharp
.InnerJoin("TBITEM I", j => j
    .On("I.ID", "P.ID")
    .And("I.FILIAL", "P.FILIAL"))
```

Join composto:

```csharp
.On("A.ID","B.ID")
.And("A.FILIAL","B.FILIAL")
```

---

## Grouping

```csharp
.GroupBy(g => g
    .Column("FILIAL")
    .Column("CLIENTE"))
```

Having:

```csharp
.Having(h => h
    .Where("SUM(VALOR) >", 1000))
```

---

## Window Functions

```csharp
.Window(w => w
    .RowNumber()
    .PartitionBy("FILIAL")
    .OrderBy("TOTAL DESC")
    .As("RN"))
```

Suportado: `RowNumber`, `Rank`, `DenseRank`, `Lag`, `Lead`, `Aggregate Over()`

---

## Set Operators

Suporta: `Union`, `UnionAll`, `Intersect`, `Except`.

```csharp
.UnionAll(branch => branch
    .FromTable("TBMOVIMENTO"))
```

---

## Exists / Subqueries

Exists:

```csharp
.WhereExists(sub => sub
    .FromTable("TBITEM I")
    .Where("I.ID","P.ID"))
```

Scalar:

```csharp
.Where("VALOR >", sub => sub
    .FromTable("TBITEM")
    .Scalar("AVG(VALOR)"))
```

---

## CASE Expressions

```csharp
.Case()
.When("STATUS",1,"'OK'")
.Else("'ERRO'")
.As("DESC_STATUS")
```

---

## Pagination

```csharp
.Page(0,100)
```

ou com modo explícito:

```csharp
.Page(
    skip:0,
    take:100,
    mode:PaginationMode.RowNumber)
```

---

## Query Options

```csharp
.Option(QueryOption.Recompile)
.Raw("OPTION(MAXDOP 4)")
```

---

## Coverage Matrix

| Feature | Suportado |
|---|---|
| Temp Tables | ✅ |
| CTE | ✅ |
| Multiple CTE | ✅ |
| Index Creation | ✅ |
| Join | ✅ |
| Composite Join | ✅ |
| Exists | ✅ |
| Not Exists | ✅ |
| Scalar Subquery | ✅ |
| Group By | ✅ |
| Having | ✅ |
| Window Functions | ✅ |
| Union | ✅ |
| Union All | ✅ |
| Intersect | ✅ |
| Except | ✅ |
| Pagination | ✅ |
| Pushdowns | ✅ |
| Aggregates | ✅ |
| CASE | ✅ |
| Cross Apply | ✅ |
| Outer Apply | ✅ |
| Raw SQL Escape Hatch | ✅ |
| Hints | ✅ |
| Sort | ✅ |
| Count | ✅ |
| Distinct | ✅ |

---

## Modelo Mental Final

```
Prepare
↓
Pushdown
↓
Source
↓
Join / Apply
↓
Filter
↓
Project
↓
Group
↓
Having
↓
Window
↓
Set Operators
↓
Global Filters
↓
Sort
↓
Count
↓
Page
↓
Options
↓
SQL
```

---

## Exemplos Reais de Uso

### Exemplo 1 — Consulta Estruturada Simples (`FromTable`)

**DSL:**

```csharp
SqlQuery<MovimentoDTO> query =
    new SqlQuery<MovimentoDTO>(provider)
        .FromTable("TBMOVIMENTO M")
        .InnerJoin("TBITEMMOVIMENTO I", j => j
            .On("I.CHAVE_FATO", "M.CHAVE_FATO")
            .And("I.COD_FILIAL", "M.COD_FILIAL"))
        .Filter(f => f
            .Where("M.STATUS", 1)
            .And("I.QTD >", 0))
        .Project(@"
            M.CHAVE_FATO,
            M.DATA_MOVTO,
            SUM(I.VALOR_TOTAL) TOTAL")
        .GroupBy(g => g
            .Column("M.CHAVE_FATO")
            .Column("M.DATA_MOVTO"))
        .Sort("M.DATA_MOVTO", "DESC")
        .Page(0,100);

string sql = query.ToSql();
```

**SQL Gerado:**

```sql
SELECT
    M.CHAVE_FATO,
    M.DATA_MOVTO,
    SUM(I.VALOR_TOTAL) TOTAL

FROM TBMOVIMENTO M

INNER JOIN TBITEMMOVIMENTO I
    ON I.CHAVE_FATO = M.CHAVE_FATO
   AND I.COD_FILIAL = M.COD_FILIAL

WHERE
    M.STATUS = @p0
    AND I.QTD > @p1

GROUP BY
    M.CHAVE_FATO,
    M.DATA_MOVTO

ORDER BY
    M.DATA_MOVTO DESC

OFFSET 0 ROWS
FETCH NEXT 100 ROWS ONLY
```

---

### Exemplo 2 — SQL Manual (`FromRaw`)

**DSL:**

```csharp
SqlFilter filtroPeriodo =
    new SqlFilter()
        .Where("M.DATA >=", dataInicial)
        .And("M.DATA <=", dataFinal);

SqlQuery<FaturamentoDTO> query =
    new SqlQuery<FaturamentoDTO>(provider)
        .Pushdown("Periodo", filtroPeriodo)
        .FromRaw(@"
            SELECT
                M.COD_FILIAL,
                M.VALOR_TOTAL
            FROM TBMOVIMENTO M
            WHERE 1=1
                {{Periodo}}
        ")
        .Filter(f => f
            .Where("VALOR_TOTAL >", 0))
        .Sort("VALOR_TOTAL", "DESC");

string sql = query.ToSql();
```

**SQL Gerado:**

```sql
SELECT *

FROM (
    SELECT
        M.COD_FILIAL,
        M.VALOR_TOTAL
    FROM TBMOVIMENTO M
    WHERE 1=1
        AND M.DATA >= @p0
        AND M.DATA <= @p1
) X

WHERE
    VALOR_TOTAL > @p2

ORDER BY
    VALOR_TOTAL DESC
```

---

### Exemplo 3 — Temp Tables + Pushdown + Índices

**DSL:**

```csharp
SqlFilter filtroMovimento =
    new SqlFilter()
        .Where("M.COD_FILIAL IN", filiais)
        .And("M.DATA >=", inicio)
        .And("M.DATA <=", fim);

SqlQuery<MovimentoDTO> query =
    new SqlQuery<MovimentoDTO>(provider)
        .Prepare(@"
            IF OBJECT_ID('tempdb..#TMP') IS NOT NULL
                DROP TABLE #TMP;

            SELECT *
            INTO #TMP
            FROM TBMOVIMENTO M
            WHERE 1=1
                {{FiltroMovimento}};

            CREATE CLUSTERED INDEX IX_TMP
                ON #TMP(CHAVE_FATO,COD_FILIAL);
        ")
        .Pushdown("FiltroMovimento", filtroMovimento)
        .FromTable("#TMP T")
        .Filter(f => f
            .Where("T.STATUS", 1))
        .Project(@"
            T.CHAVE_FATO,
            T.COD_FILIAL");

string sql = query.ToSql();
```

**SQL Gerado:**

```sql
IF OBJECT_ID('tempdb..#TMP') IS NOT NULL
    DROP TABLE #TMP;

SELECT *
INTO #TMP
FROM TBMOVIMENTO M
WHERE 1=1
    AND M.COD_FILIAL IN (@p0,@p1)
    AND M.DATA >= @p2
    AND M.DATA <= @p3;

CREATE CLUSTERED INDEX IX_TMP
ON #TMP(CHAVE_FATO,COD_FILIAL);

SELECT
    T.CHAVE_FATO,
    T.COD_FILIAL

FROM #TMP T

WHERE
    T.STATUS = @p4
```

---

### Exemplo 4 — GroupBy + Having + Window

**DSL:**

```csharp
SqlQuery<RankingDTO> query =
    new SqlQuery<RankingDTO>(provider)
        .FromTable("TBITEMMOVIMENTO I")
        .Project(p => p
            .Column("I.COD_FILIAL")
            .Aggregate("SUM(I.VALOR_TOTAL)", "TOTAL")
            .Window(w => w
                .DenseRank()
                .OrderBy("SUM(I.VALOR_TOTAL) DESC")
                .As("RANKING")))
        .GroupBy(g => g
            .Column("I.COD_FILIAL"))
        .Having(h => h
            .Where("SUM(I.VALOR_TOTAL) >", 10000))
        .Sort("TOTAL", "DESC");

string sql = query.ToSql();
```

**SQL Gerado:**

```sql
SELECT
    I.COD_FILIAL,
    SUM(I.VALOR_TOTAL) TOTAL,
    DENSE_RANK() OVER(
        ORDER BY SUM(I.VALOR_TOTAL) DESC
    ) RANKING

FROM TBITEMMOVIMENTO I

GROUP BY
    I.COD_FILIAL

HAVING
    SUM(I.VALOR_TOTAL) > @p0

ORDER BY
    TOTAL DESC
```

---

### Exemplo 5 — Exists + Subqueries

**DSL:**

```csharp
SqlQuery<PedidoDTO> query =
    new SqlQuery<PedidoDTO>(provider)
        .FromTable("TBPEDIDO P")
        .Filter(f => f
            .WhereExists(sub => sub
                .FromTable("TBITEM I")
                .Where("I.PEDIDO_ID", "P.ID"))
            .AndNotExists(sub => sub
                .FromTable("TBCANCELAMENTO C")
                .Where("C.PEDIDO_ID", "P.ID"))
            .And("P.VALOR_TOTAL >", sub => sub
                .FromTable("TBPEDIDO")
                .Scalar("AVG(VALOR_TOTAL)")))
        .Project(@"
            P.ID,
            P.CLIENTE_ID,
            P.VALOR_TOTAL");

string sql = query.ToSql();
```

**SQL Gerado:**

```sql
SELECT
    P.ID,
    P.CLIENTE_ID,
    P.VALOR_TOTAL

FROM TBPEDIDO P

WHERE
EXISTS(
    SELECT 1
    FROM TBITEM I
    WHERE I.PEDIDO_ID = P.ID
)
AND NOT EXISTS(
    SELECT 1
    FROM TBCANCELAMENTO C
    WHERE C.PEDIDO_ID = P.ID
)
AND
P.VALOR_TOTAL >
(
    SELECT AVG(VALOR_TOTAL)
    FROM TBPEDIDO
)
```

---

### Exemplo 6 — Union + CTE + Pipeline Complexo

**DSL:**

```csharp
SqlQuery<DocumentoDTO> query =
    new SqlQuery<DocumentoDTO>(provider)
        .FromCte(cte => cte
            .With("base", @"
                SELECT *
                FROM TBMOVIMENTO"))
        .FromTable("base M")
        .Project(@"
            M.CHAVE_FATO,
            'SAIDA' AS ORIGEM")
        .UnionAll(branch => branch
            .FromTable("TBENTRADA E")
            .Project(@"
                E.CHAVE_FATO,
                'ENTRADA' AS ORIGEM"))
        .Filter(f => f
            .Where("ORIGEM", "SAIDA")
            .Or("ORIGEM", "ENTRADA"))
        .Count(CountStrategy.Window)
        .Page(0,50);

string sql = query.ToSql();
```

**SQL Gerado:**

```sql
WITH base AS (
    SELECT *
    FROM TBMOVIMENTO
)

SELECT *
FROM (
    SELECT
        M.CHAVE_FATO,
        'SAIDA' ORIGEM
    FROM base M

    UNION ALL

    SELECT
        E.CHAVE_FATO,
        'ENTRADA' ORIGEM
    FROM TBENTRADA E
) X

WHERE
    ORIGEM='SAIDA'
    OR ORIGEM='ENTRADA'

COUNT(*) OVER() TOTAL_ROWS

OFFSET 0 ROWS
FETCH NEXT 50 ROWS ONLY
```

---

### Exemplo 7 — Modelo `From` Fluido com Bloco Híbrido Complexo e Predicados Livres

**DSL:**

```csharp
SqlFilter filtroMovimento = new SqlFilter()
    .Where("D.COD_FILIAL IN", filiais)
    .And("D.DATA >=", dataInicial);

SqlQuery<DTO> query = new SqlQuery<DTO>(SqlServerProvider.Instance)
    .Prepare(@"
        IF OBJECT_ID('tempdb..#TMP') IS NOT NULL
            DROP TABLE #TMP;

        SELECT *
        INTO #TMP
        FROM TBMOVIMENTO
        WHERE 1=1 {{FiltroMovimento}};
    ")
    .From(@"
        WITH origem AS (
            SELECT
                D.CHAVE_FATO AS ChaveFatoOrigem,
                D.NUM_DOCTO AS NumDoctoOrigem,
                D.STATUS AS StatusDestino
            FROM #TMP D
        )
        SELECT *
        FROM origem Q
    ")
    .Pushdown("FiltroMovimento", filtroMovimento)
    .Filter(f => f
        .Where("Q.StatusDestino", 0)
        .And(group => group
            .Where("Q.CodFilial", "110")
            .Or("Q.CodFilial", "120"))
        .And(group => group
            .Where("Q.NumDoctoOrigem >", 1000)
            .Or("Q.NumDoctoOrigem", 4))
        .And("Q.TipoDocumento IN", tipos)
        .And("Q.DataMovto >=", dataInicial)
        .And("Q.DataMovto <=", dataFinal)
        .And("ISNULL(Q.StatusDestino,0)=0")
    )
    .Sort("Q.ChaveFatoOrigem", "DESC")
    .Count(CountStrategy.Window)
    .Page(0, 50, PaginationMode.RowNumber);

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
IF OBJECT_ID('tempdb..#TMP') IS NOT NULL
    DROP TABLE #TMP;

SELECT *
INTO #TMP
FROM TBMOVIMENTO
WHERE 1=1 
    AND D.COD_FILIAL IN (@p0, @p1)
    AND D.DATA >= @p2;

SELECT *
FROM (
    SELECT 
        *,
        ROW_NUMBER() OVER (ORDER BY Q.ChaveFatoOrigem DESC) AS RowNum,
        COUNT(*) OVER() AS TOTAL_ROWS
    FROM (
        WITH origem AS (
            SELECT
                D.CHAVE_FATO AS ChaveFatoOrigem,
                D.NUM_DOCTO AS NumDoctoOrigem,
                D.STATUS AS StatusDestino
            FROM #TMP D
        )
        SELECT *
        FROM origem Q
    ) X
    WHERE
        X.StatusDestino = @p3
        AND (X.CodFilial = @p4 OR X.CodFilial = @p5)
        AND (X.NumDoctoOrigem > @p6 OR X.NumDoctoOrigem = @p7)
        AND X.TipoDocumento IN (@p8, @p9)
        AND X.DataMovto >= @p10
        AND X.DataMovto <= @p11
        AND ISNULL(X.StatusDestino,0)=0
) Y
WHERE 
    Y.RowNum BETWEEN 1 AND 50
```

---

### Exemplo 8 — Fechamento de Estoque Mensal (Híbrido com `From`)

**DSL:**

```csharp
SqlFilter filtroFilial = new SqlFilter()
    .Where("E.COD_FILIAL IN", listaFiliais)
    .And("E.DATA_FECHAMENTO =", dataCorte);

SqlQuery<EstoqueDTO> query = new SqlQuery<EstoqueDTO>(SqlServerProvider.Instance)
    .Prepare(@"
        IF OBJECT_ID('tempdb..#TMP_SALDOS') IS NOT NULL
            DROP TABLE #TMP_SALDOS;

        SELECT 
            E.COD_FILIAL,
            E.ID_PRODUTO,
            E.QTD_FINAL,
            E.CUSTO_MEDIO
        INTO #TMP_SALDOS
        FROM TBESTOQUE_MENSAL E
        WHERE 1=1 {{FiltroEstoque}};

        CREATE CLUSTERED INDEX IX_TMP_SALDOS ON #TMP_SALDOS(COD_FILIAL, ID_PRODUTO);
    ")
    .From(@"
        WITH calculo_Kardex AS (
            SELECT 
                S.COD_FILIAL,
                S.ID_PRODUTO,
                (S.QTD_FINAL * S.CUSTO_MEDIO) AS VALOR_PATRIMONIAL,
                P.CONTA_CONTABIL
            FROM #TMP_SALDOS S
            INNER JOIN TBPRODUTO P ON P.ID = S.ID_PRODUTO
        )
        SELECT * FROM calculo_Kardex K
    ")
    .Pushdown("FiltroEstoque", filtroFilial)
    .Filter(f => f
        .Where("Q.VALOR_PATRIMONIAL >", 50000)
        .And("Q.CONTA_CONTABIL NOT LIKE", "'99%'")
    )
    .Sort("Q.VALOR_PATRIMONIAL", "DESC");

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
IF OBJECT_ID('tempdb..#TMP_SALDOS') IS NOT NULL
    DROP TABLE #TMP_SALDOS;

SELECT 
    E.COD_FILIAL,
    E.ID_PRODUTO,
    E.QTD_FINAL,
    E.CUSTO_MEDIO
INTO #TMP_SALDOS
FROM TBESTOQUE_MENSAL E
WHERE 1=1 
    AND E.COD_FILIAL IN (@p0, @p1)
    AND E.DATA_FECHAMENTO = @p2;

CREATE CLUSTERED INDEX IX_TMP_SALDOS ON #TMP_SALDOS(COD_FILIAL, ID_PRODUTO);

SELECT *
FROM (
    WITH calculo_Kardex AS (
        SELECT 
            S.COD_FILIAL,
            S.ID_PRODUTO,
            (S.QTD_FINAL * S.CUSTO_MEDIO) AS VALOR_PATRIMONIAL,
            P.CONTA_CONTABIL
        FROM #TMP_SALDOS S
        INNER JOIN TBPRODUTO P ON P.ID = S.ID_PRODUTO
    )
    SELECT * FROM calculo_Kardex K
) X

WHERE 
    X.VALOR_PATRIMONIAL > @p3
    AND X.CONTA_CONTABIL NOT LIKE @p4

ORDER BY 
    X.VALOR_PATRIMONIAL DESC
```

---

### Exemplo 9 — Auditoria de Notas Fiscais e Impostos Divergentes

**DSL:**

```csharp
SqlFilter filtroPeriodo = new SqlFilter()
    .Where("N.DATA_EMISSAO >=", dataInicial)
    .And("N.DATA_EMISSAO <=", dataFinal);

SqlQuery<AuditoriaDTO> query = new SqlQuery<AuditoriaDTO>(SqlServerProvider.Instance)
    .From(@"
        SELECT 
            N.ID AS NOTA_ID,
            N.NUMERO,
            N.SERIE,
            I.VALOR_ICMS,
            I.VALOR_PIS,
            (SELECT SUM(A.VALOR) FROM TBCALCULO_AUDIT A WHERE A.NOTA_ID = N.ID) AS ICMS_AUDITADO
        FROM TBNOTA_FISCAL N
        INNER JOIN TBNOTA_IMPOSTO I ON I.NOTA_ID = N.ID
        WHERE 1=1 {{FiltroPeriodo}}
    ")
    .Pushdown("FiltroPeriodo", filtroPeriodo)
    .Filter(f => f
        .Where("VALOR_ICMS <> ICMS_AUDITADO")
        .And("VALOR_PIS >", 0)
    )
    .Sort("NOTA_ID", "ASC")
    .Page(0, 25);

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
SELECT *
FROM (
    SELECT 
        N.ID AS NOTA_ID,
        N.NUMERO,
        N.SERIE,
        I.VALOR_ICMS,
        I.VALOR_PIS,
        (SELECT SUM(A.VALOR) FROM TBCALCULO_AUDIT A WHERE A.NOTA_ID = N.ID) AS ICMS_AUDITADO
    FROM TBNOTA_FISCAL N
    INNER JOIN TBNOTA_IMPOSTO I ON I.NOTA_ID = N.ID
    WHERE 1=1 
        AND N.DATA_EMISSAO >= @p0
        AND N.DATA_EMISSAO <= @p1
) X

WHERE 
    X.VALOR_ICMS <> X.ICMS_AUDITADO
    AND X.VALOR_PIS > @p2

ORDER BY 
    X.NOTA_ID ASC

OFFSET 0 ROWS
FETCH NEXT 25 ROWS ONLY
```

---

### Exemplo 10 — CTE de Recursividade de Hierarquia de Filiais

**DSL:**

```csharp
SqlQuery<HierarquiaDTO> query = new SqlQuery<HierarquiaDTO>(SqlServerProvider.Instance)
    .From(@"
        WITH hierarquia_filiais AS (
            SELECT 
                F.ID,
                F.ID_PAI,
                F.NOME,
                1 AS NIVEL
            FROM TBFILIAL F
            WHERE F.ID_PAI IS NULL

            UNION ALL

            SELECT 
                H.ID,
                H.ID_PAI,
                H.NOME,
                H.NIVEL + 1
            FROM TBFILIAL H
            INNER JOIN hierarquia_filiais P ON H.ID_PAI = P.ID
        )
        SELECT * FROM hierarquia_filiais
    ")
    .Filter(f => f
        .Where("NIVEL >", 1)
        .And("ID IN", listaPermissao)
    )
    .Sort("NIVEL", "ASC");

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
SELECT *
FROM (
    WITH hierarquia_filiais AS (
        SELECT 
            F.ID,
            F.ID_PAI,
            F.NOME,
            1 AS NIVEL
        FROM TBFILIAL F
        WHERE F.ID_PAI IS NULL

        UNION ALL

        SELECT 
            H.ID,
            H.ID_PAI,
            H.NOME,
            H.NIVEL + 1
        FROM TBFILIAL H
        INNER JOIN hierarquia_filiais P ON H.ID_PAI = P.ID
    )
    SELECT * FROM hierarquia_filiais
) X

WHERE 
    X.NIVEL > @p0
    AND X.ID IN (@p1, @p2)

ORDER BY 
    X.NIVEL ASC
```

---

### Exemplo 11 — Agrupamentos Complexos e Multi-Joins em Bloco Livre

**DSL:**

```csharp
SqlFilter filtroProdutos = new SqlFilter()
    .Where("P.STATUS =", "ATIVO")
    .And("P.CATEGORIA IN", categorias);

SqlQuery<ResumoVendasDTO> query = new SqlQuery<ResumoVendasDTO>(SqlServerProvider.Instance)
    .From(@"
        SELECT 
            C.REGIAO,
            P.MARCA,
            SUM(I.QUANTIDADE) AS QTD_TOTAL,
            SUM(I.VALOR_LIQUIDO) AS LIQUIDO_TOTAL
        FROM TBITEM_VENDA I
        INNER JOIN TBPRODUTO P ON P.ID = I.PRODUTO_ID
        INNER JOIN TBCLIENTE C ON C.ID = I.CLIENTE_ID
        WHERE 1=1 {{FiltroProdutos}}
        GROUP BY C.REGIAO, P.MARCA
    ")
    .Pushdown("FiltroProdutos", filtroProdutos)
    .Filter(f => f
        .Where("LIQUIDO_TOTAL >=", 100000)
        .And("REGIAO <>", "'NORTE'")
    )
    .Sort("LIQUIDO_TOTAL", "DESC");

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
SELECT *
FROM (
    SELECT 
        C.REGIAO,
        P.MARCA,
        SUM(I.QUANTIDADE) AS QTD_TOTAL,
        SUM(I.VALOR_LIQUIDO) AS LIQUIDO_TOTAL
    FROM TBITEM_VENDA I
    INNER JOIN TBPRODUTO P ON P.ID = I.PRODUTO_ID
    INNER JOIN TBCLIENTE C ON C.ID = I.CLIENTE_ID
    WHERE 1=1 
        AND P.STATUS = @p0
        AND P.CATEGORIA IN (@p1, @p2)
    GROUP BY C.REGIAO, P.MARCA
) X

WHERE 
    X.LIQUIDO_TOTAL >= @p3
    AND X.REGIAO <> @p4

ORDER BY 
    X.LIQUIDO_TOTAL DESC
```

---

### Exemplo 12 — Auditoria de Logs de Status com Window Functions Aninhadas

**DSL:**

```csharp
SqlQuery<LogStatusDTO> query = new SqlQuery<LogStatusDTO>(SqlServerProvider.Instance)
    .From(@"
        SELECT 
            L.TIPO_DOCUMENTO,
            L.DOCUMENTO_ID,
            L.STATUS_ORIGEM,
            L.STATUS_DESTINO,
            L.DATA_LOG,
            LAG(L.STATUS_DESTINO) OVER (PARTITION BY L.DOCUMENTO_ID ORDER BY L.DATA_LOG ASC) AS STATUS_ANTERIOR
        FROM TBLOG_PROCESSO L
    ")
    .Filter(f => f
        .Where("TIPO_DOCUMENTO =", "'NFE'")
        .And("STATUS_DESTINO <> STATUS_ANTERIOR")
        .And("ISNULL(STATUS_ANTERIOR,'') <> ''")
    )
    .Sort("DATA_LOG", "DESC")
    .Count(CountStrategy.Window)
    .Page(0, 100, PaginationMode.RowNumber);

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
SELECT *
FROM (
    SELECT 
        *,
        ROW_NUMBER() OVER (ORDER BY X.DATA_LOG DESC) AS RowNum,
        COUNT(*) OVER() AS TOTAL_ROWS
    FROM (
        SELECT 
            L.TIPO_DOCUMENTO,
            L.DOCUMENTO_ID,
            L.STATUS_ORIGEM,
            L.STATUS_DESTINO,
            L.DATA_LOG,
            LAG(L.STATUS_DESTINO) OVER (PARTITION BY L.DOCUMENTO_ID ORDER BY L.DATA_LOG ASC) AS STATUS_ANTERIOR
        FROM TBLOG_PROCESSO L
    ) X
    WHERE 
        X.TIPO_DOCUMENTO = @p0
        AND X.STATUS_DESTINO <> X.STATUS_ANTERIOR
        AND ISNULL(X.STATUS_ANTERIOR,'') <> ''
) Y

WHERE 
    Y.RowNum BETWEEN 1 AND 100
```

---

### Exemplo 13 — Acompanhamento de Metas de Vendedores com Subqueries Dependentes

**DSL:**

```csharp
SqlFilter filtroMes = new SqlFilter()
    .Where("M.ANO_MES =", "2026-05");

SqlQuery<MetaDTO> query = new SqlQuery<MetaDTO>(SqlServerProvider.Instance)
    .From(@"
        SELECT 
            V.ID_VENDEDOR,
            V.NOME_VENDEDOR,
            M.VALOR_META,
            COALESCE((
                SELECT SUM(F.VALOR_LIQUIDO) 
                FROM TBFATURAMENTO F 
                WHERE F.VENDEDOR_ID = V.ID_VENDEDOR 
                  AND F.SITUACAO = 'EFETIVADO'
            ), 0) AS VALOR_REALIZADO
        FROM TBVENDEDOR V
        INNER JOIN TBMETA_VENDAS M ON M.VENDEDOR_ID = V.ID_VENDEDOR
        WHERE 1=1 {{FiltroMes}}
    ")
    .Pushdown("FiltroMes", filtroMes)
    .Filter(f => f
        .Where("VALOR_REALIZADO < VALOR_META")
        .And("V.SITUACAO_ATIVO =", 1)
    )
    .Sort("VALOR_REALIZADO", "ASC");

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
SELECT *
FROM (
    SELECT 
        V.ID_VENDEDOR,
        V.NOME_VENDEDOR,
        M.VALOR_META,
        COALESCE((
            SELECT SUM(F.VALOR_LIQUIDO) 
            FROM TBFATURAMENTO F 
            WHERE F.VENDEDOR_ID = V.ID_VENDEDOR 
              AND F.SITUACAO = 'EFETIVADO'
        ), 0) AS VALOR_REALIZADO
    FROM TBVENDEDOR V
    INNER JOIN TBMETA_VENDAS M ON M.VENDEDOR_ID = V.ID_VENDEDOR
    WHERE 1=1 
        AND M.ANO_MES = @p0
) X

WHERE 
    X.VALOR_REALIZADO < X.VALOR_META
    AND X.SITUACAO_ATIVO = @p1

ORDER BY 
    X.VALOR_REALIZADO ASC
```

---

### Exemplo 14 — Unificação de Fluxos Financeiros (`Union All` Interno no `From`)

**DSL:**

```csharp
SqlQuery<FinanceiroDTO> query = new SqlQuery<FinanceiroDTO>(SqlServerProvider.Instance)
    .From(@"
        SELECT 
            R.ID,
            R.VALOR,
            R.DATA_VENCIMENTO AS DATA_FINANCEIRO,
            'RECEBER' AS TIPO
        FROM TBLANCAMENTO_RECEBER R
        
        UNION ALL
        
        SELECT 
            P.ID,
            P.VALOR * -1 AS VALOR,
            P.DATA_VENCIMENTO AS DATA_FINANCEIRO,
            'PAGAR' AS TIPO
        FROM TBLANCAMENTO_PAGAR P
    ")
    .Filter(f => f
        .Where("DATA_FINANCEIRO >=", dataCorte)
        .And(group => group
            .Where("TIPO =", "'RECEBER'")
            .Or("VALOR <", -5000)
        )
    )
    .Sort("DATA_FINANCEIRO", "ASC")
    .Page(0, 50);

string sql = query.ToSqlFull();
```

**SQL Gerado:**

```sql
SELECT *
FROM (
    SELECT 
        R.ID,
        R.VALOR,
        R.DATA_VENCIMENTO AS DATA_FINANCEIRO,
        'RECEBER' AS TIPO
    FROM TBLANCAMENTO_RECEBER R
    
    UNION ALL
    
    SELECT 
        P.ID,
        P.VALOR * -1 AS VALOR,
        P.DATA_VENCIMENTO AS DATA_FINANCEIRO,
        'PAGAR' AS TIPO
    FROM TBLANCAMENTO_PAGAR P
) X

WHERE 
    X.DATA_FINANCEIRO >= @p0
    AND (X.TIPO = @p1 OR X.VALOR < @p2)

ORDER BY 
    X.DATA_FINANCEIRO ASC

OFFSET 0 ROWS
FETCH NEXT 50 ROWS ONLY
```

---

## Convenções do SQL Gerado

**Parâmetros:**

```sql
@p0, @p1, @p2, ...
```

**Pushdowns:**

```sql
{{Periodo}}
-- substituído antes da geração final
```

**Providers:**

```
SqlServerProvider
PostgresProvider
OracleProvider
```

**Pipeline interno:**

```
DSL
↓
Logical Plan
↓
Optimization Layer
↓
Provider Translation
↓
Final SQL
```

---

## Objetivo Final

> A DSL **NÃO** é ORM.  
> A DSL **NÃO** substitui SQL.  
> A DSL **organiza** SQL complexo.

```
SQL First
Pipeline Driven
Compile-time Guided
Big Data Inspired
```
