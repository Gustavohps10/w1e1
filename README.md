# W1E1 Query DSL - Pipeline Style

## Objetivo

A DSL do W1E1 foi projetada para seguir um modelo inspirado em pipelines de Big Data, priorizando:

* SQL semi-puro
* Fácil leitura por analistas
* Curva de aprendizado baixa
* Ordem previsível das etapas
* Pipeline fluente
* Restrições de fluxo em compile-time (IntelliSense guiando o uso)

A ideia principal é:

```text
Prepare
→ From
→ Pushdown
→ Filter
→ Sort
→ Count
→ Page
→ Build
```

---

# Filosofia

Ao invés de esconder SQL atrás de ASTs complexas ou LINQ excessivamente abstrato:

* SQL continua existindo
* JOIN continua SQL
* CTE continua SQL
* Alias continuam SQL
* Condições são organizadas via pipeline

Objetivo:

```text
Menos magia
Mais previsibilidade
Mais manutenção
```

---

# Fluxo Obrigatório

Fluxo permitido:

```text
Prepare? 
   ↓
From / FromCte (obrigatório)
   ↓
Pushdown*
   ↓
Filter?
   ↓
Sort?
   ↓
Count?
   ↓
Page?
   ↓
ToSql()
```

Restrições:

```text
Prepare → permite From
From → NÃO permite Prepare
Filter → somente após From
Count → opcional
Page → opcional
ToSql → finaliza pipeline
```

---

# Estrutura dos Filtros

Regra principal:

* Todo grupo inicia com Where()
* Após Where(), NÃO existe novo Where()
* Depois do primeiro Where(), apenas operadores relacionais

Exemplo válido:

```csharp
.Filter(f => f
    .Where("Q.StatusDestino", 0)
    .And("Q.CodFilial", "110")
    .Or("Q.CodFilial", "120")
)
```

Exemplo inválido:

```csharp
.Filter(f => f
    .Where("Status", 1)
    .Where("Filial", 110)
)
```

---

# Operadores

Operadores suportados:

```csharp
.Where("Campo", valor)

.Where("Campo >", valor)
.Where("Campo >=", valor)
.Where("Campo <", valor)
.Where("Campo <=", valor)

.And("Campo", valor)
.Or("Campo", valor)

.And("Campo IN", lista)
.Or("Campo IN", lista)

.And("Campo NOT IN", lista)
.Or("Campo NOT IN", lista)

.And("SQL RAW")
.Or("SQL RAW")
```

Exemplos:

```csharp
.Where("Q.StatusDestino", 0)

.And("Q.DataMovto >=", dataInicial)

.And("Q.TipoDocumento IN", tipos)

.And("ISNULL(Q.StatusDestino,0)=0")
```

---

# Subgrupos Booleanos

Subgrupos sempre iniciam novamente com Where().

Exemplo:

```csharp
.Filter(f => f
    .Where("Q.StatusDestino", 0)

    .And(group => group
        .Where("Q.CodFilial", "110")
        .Or("Q.CodFilial", "120"))

    .And(group => group
        .Where("Q.NumDoctoOrigem >", 4)
        .Or("Q.NumDoctoOrigem", 60922))
)
```

Resultado:

```sql
WHERE
(
    Q.StatusDestino = 0
    AND (Q.CodFilial='110' OR Q.CodFilial='120')
    AND (Q.NumDoctoOrigem > 4 OR Q.NumDoctoOrigem=60922)
)
```

---

# Pushdown Filters

Pushdowns permitem reutilização de filtros em blocos SQL.

Exemplo:

```csharp
SqlFilter filtroMovimento =
    new SqlFilter()
        .Where("D.COD_FILIAL IN", filiais)
        .And("D.DATA >=", dataInicial);
```

Uso:

```csharp
.Pushdown("FiltroSaidas", filtroMovimento)
.Pushdown("FiltroEntradas", filtroMovimento)
```

SQL:

```sql
WHERE 1=1 {{FiltroSaidas}}

WHERE 1=1 {{FiltroEntradas}}
```

---

# Exemplo Completo

```csharp
SqlFilter filtroMovimento =
    new SqlFilter()
        .Where("D.COD_FILIAL IN", filiais)
        .And("D.DATA >=", dataInicial);

SqlQuery<DTO> query =
    new SqlQuery<DTO>(SqlServerProvider.Instance)

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

---

# Filosofia Final

A DSL NÃO tenta substituir SQL.

Ela tenta:

```text
Organizar SQL
↓
Padronizar filtros
↓
Guiar fluxo
↓
Reduzir erro humano
```

Modelo mental:

```text
Dataset
→ Pushdown
→ Filter
→ Sort
→ Count
→ Page
→ SQL
```

Inspirado em engines de Big Data, porém mantendo SQL como cidadão de primeira classe.
