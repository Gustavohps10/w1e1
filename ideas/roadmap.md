# W1E1 Query DSL — Roadmap & Visão de Futuro

> Documento de ideias e direção arquitetural.  
> Registro das evoluções planejadas sem compromisso de versão.

---

## Visão Geral

A W1E1 Query DSL nasceu como um **SQL pipeline organizado** para SQL Server.  
A evolução natural é torná-la uma **camada de acesso universal a qualquer storage**, mantendo sempre a filosofia central:

```
SQL First
Pipeline Driven
Compile-time Guided
```

---

## Arquitetura de Camadas (Alvo)

```
┌─────────────────────────────────────────────────────┐
│                 Query Pipeline DSL                  │
│     (FromEntity, Filter, Join, Sort, Page…)         │
│          nunca sabe o que está embaixo              │
└─────────────────────┬───────────────────────────────┘
                      │  plano lógico
                      ▼
┌─────────────────────────────────────────────────────┐
│              IMetadataProvider                      │
│   resolve: nomes de tabela, colunas, schemas,       │
│   tipos, convenções de nomenclatura                 │
└─────────────────────┬───────────────────────────────┘
                      │  nomes reais
                      ▼
┌─────────────────────────────────────────────────────┐
│              ISourceProvider                        │
│   traduz: dialeto SQL, paginação, parâmetros,       │
│   hints específicos do storage                      │
└─────────────────────┬───────────────────────────────┘
                      │  query compilada
                      ▼
┌─────────────────────────────────────────────────────┐
│              IExecutionEngine                       │
│   transporta: ADO.NET, HTTP, gRPC, REST API         │
└─────────────────────┬───────────────────────────────┘
                      │  resultado
                      ▼
┌─────────────────────────────────────────────────────┐
│              Application Layer                      │
│         API / Dashboard / Report / ML               │
└─────────────────────────────────────────────────────┘
```

**Princípio:** cada camada é substituível independentemente.  
Trocar o storage não muda o pipeline. Trocar o metadata provider não muda o dialeto.

---

## Camada 1 — IMetadataProvider

Responsável por **resolver mapeamento entre entidade C# e storage**.

### Interface

```csharp
public interface IMetadataProvider
{
    string GetTableName<T>();
    string GetColumnName<T>(Expression<Func<T, object>> prop);
    string GetSchema<T>();
    ColumnType GetColumnType<T>(Expression<Func<T, object>> prop);
    string GetFullyQualifiedName<T>(); // schema.tabela ou projeto.dataset.tabela
}
```

### Implementações planejadas

| Provider | Fonte do mapeamento |
|---|---|
| `EFCoreMetadataProvider` | Lê `DbContext.Model` (Metadata API oficial do EF Core) |
| `DapperMetadataProvider` | Lê atributos `[Table]` / `[Column]` |
| `FluentMetadataProvider` | Mapeamento manual fluente, sem dependência de ORM |
| `ConventionMetadataProvider` | Convenções: `PascalCase → snake_case`, prefixos, sufixos |

### EFCoreMetadataProvider — como funciona

```csharp
// EF Core expõe isso via Metadata API pública e estável
var entityType = db.Model.FindEntityType(typeof(TbMovimento));

var tableName = entityType.GetTableName();
// → "TBMOVIMENTO"

var columnName = entityType
    .FindProperty(nameof(TbMovimento.ChaveFato))
    .GetColumnName();
// → "CHAVE_FATO"
```

Qualquer convenção já configurada no EF — Fluent API, DataAnnotations, convenção de nome — é herdada automaticamente. **Zero duplicação de mapeamento.**

### FluentMetadataProvider — para storages sem ORM

```csharp
new FluentMetadataProvider(mapping =>
    mapping.Entity<TbMovimento>()
           .ToTable("projeto.dataset.tbmovimento")
           .Property(e => e.CodFilial, "cod_filial")
           .Property(e => e.DataMovto, "data_movto"))
```

---

## Camada 2 — ISourceProvider

Responsável por **traduzir o plano lógico para o dialeto correto do storage**.

### Interface

```csharp
public interface ISourceProvider
{
    // Dialeto
    string BuildPagination(int skip, int take);
    string BuildRowNumber(string orderBy);
    string BuildParameter(int index);        // @p0 vs $1 vs :p0 vs ?
    string BuildIdentifier(string name);     // [nome] vs `nome` vs "nome"
    string BuildTop(int take);               // TOP(n) vs LIMIT n

    // Execução
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object[] parameters);
    Task<int> ExecuteAsync(string sql, object[] parameters);
}
```

### Implementações relacionais (curto prazo)

| Provider | Banco | Particularidades |
|---|---|---|
| `SqlServerProvider` | SQL Server | `OFFSET/FETCH`, `@p0`, `[identifier]`, `TOP` |
| `PostgresProvider` | PostgreSQL | `LIMIT/OFFSET`, `$1`, `"identifier"` |
| `OracleProvider` | Oracle | `ROWNUM`, `:p0`, sem `OFFSET` nativo em versões antigas |
| `MySqlProvider` | MySQL / MariaDB | `LIMIT`, `?`, backtick identifier |

### Implementações analíticas (médio prazo)

| Provider | Storage | Particularidades |
|---|---|---|
| `BigQueryProvider` | Google BigQuery | Backtick identifiers, `projeto.dataset.tabela`, `@nome` |
| `RedshiftProvider` | AWS Redshift | Postgres-like, `DISTKEY`/`SORTKEY` hints |
| `SnowflakeProvider` | Snowflake | Standard SQL, warehouse hints, `SAMPLE` |
| `SynapseProvider` | Azure Synapse | SQL Server-like, `DISTRIBUTION` hints |

### Implementações big data (longo prazo)

| Provider | Storage | Particularidades |
|---|---|---|
| `DatabricksProvider` | Databricks / Delta Lake | Spark SQL dialect, Delta tables, `OPTIMIZE` hints |
| `HiveProvider` | Apache Hive | HiveQL, partições, `DISTRIBUTE BY` |
| `TrinoProvider` | Trino / Presto | SQL federado, múltiplos catalogs |

---

## Camada 3 — IExecutionEngine

Responsável pelo **transporte da query e retorno do resultado**.  
Completamente separado do dialeto — o mesmo `SqlServerProvider` pode usar ADO.NET ou Dapper.

### Interface

```csharp
public interface IExecutionEngine
{
    Task<IEnumerable<T>> ExecuteAsync<T>(
        CompiledQuery query,
        CancellationToken ct = default);

    Task<int> ExecuteNonQueryAsync(
        CompiledQuery query,
        CancellationToken ct = default);

    IAsyncEnumerable<T> StreamAsync<T>(
        CompiledQuery query,
        CancellationToken ct = default);
}
```

### Implementações

| Engine | Transporte |
|---|---|
| `AdoNetEngine` | `SqlConnection` clássico |
| `DapperEngine` | Dapper por baixo |
| `EFCoreRawEngine` | `db.Database.SqlQueryRaw` |
| `HttpApiEngine` | BigQuery REST API, Databricks SQL API |
| `GrpcEngine` | Spark Thrift Server, Hive JDBC via gRPC |
| `OdbcEngine` | ODBC genérico para storages sem SDK .NET |

---

## Evolução do FromEntity

### Hoje — strings inline

```csharp
new SqlQuery<MovimentoDTO>(provider)
    .FromTable("TBMOVIMENTO M")
    .Filter(f => f
        .Where("M.STATUS", 1)
        .And("M.COD_FILIAL", "110"))
```

### v2 — FromEntity com lambda de dois argumentos

```csharp
new SqlQuery<MovimentoDTO>(provider, db)
    .FromEntity<TbMovimento>("M")
    .Filter(f => f
        .Where<TbMovimento>(m => m.Status, 1)
        .And<TbMovimento>(m => m.CodFilial, "110"))
    .Sort<TbMovimento>(m => m.DataMovto, "DESC")
```

### v3 — Expression tree completo (sobrecarga bool)

```csharp
new SqlQuery<MovimentoDTO>(provider, db)
    .FromEntity<TbMovimento>("M")
    .Filter(f => f
        .Where<TbMovimento>(m => m.Status == 1 && m.CodFilial == "110")
        .And<TbItemMovimento>(i => i.Qtd > 0 || i.ValorTotal > 100)
        .And("ISNULL(M.STATUS, 0) = 0"))  // raw sempre disponível
```

### Operadores suportados no expression tree

| C# | SQL gerado |
|---|---|
| `m.Status == 1` | `M.STATUS = @p0` |
| `m.Status != 1` | `M.STATUS <> @p0` |
| `m.Valor > 100` | `M.VALOR > @p0` |
| `m.Valor >= 100` | `M.VALOR >= @p0` |
| `&&` | `AND` |
| `\|\|` | `OR` (com parênteses automáticos) |
| `!m.Ativo` | `M.ATIVO = 0` |
| `m.Ativo` (bool implícito) | `M.ATIVO = 1` |
| `lista.Contains(m.CodFilial)` | `M.COD_FILIAL IN (@p0, @p1)` |
| `m.Nome.StartsWith("ABC")` | `M.NOME LIKE @p0` → `"ABC%"` |
| `m.Nome.EndsWith("ABC")` | `M.NOME LIKE @p0` → `"%ABC"` |
| `m.Nome.Contains("ABC")` | `M.NOME LIKE @p0` → `"%ABC%"` |

### Coexistência dos três estilos

```csharp
.Filter(f => f

    // Estilo 1 — expression tree (ergonômico, compile-time safe)
    .Where<TbMovimento>(m => m.Status == 1 && m.CodFilial == "110")

    // Estilo 2 — dois argumentos (atual, ainda válido)
    .And<TbMovimento>(m => m.DataMovto, ">=", dataInicial)

    // Estilo 3 — raw string (escape hatch sempre disponível)
    .And("ISNULL(M.STATUS, 0) = 0")
)
```

> Os três estilos são sobrecargas do mesmo método.  
> O pipeline não sabe qual foi usado — só recebe o fragmento SQL resolvido.

---

## Posicionamento no Ecossistema Big Data

A DSL **não processa** big data. Ela **consulta** big data de forma organizada.

```
┌─────────────────────────────────────────┐
│           Processing Layer              │
│   Spark / Flink / Databricks / dbt      │
│   ETL, transformações, ML pipelines     │
└─────────────────┬───────────────────────┘
                  │ dados processados e consolidados
                  ▼
┌─────────────────────────────────────────┐
│            Storage Layer                │
│  BigQuery / Snowflake / Redshift        │
│  Synapse / Delta Lake / Hive            │
└─────────────────┬───────────────────────┘
                  │ query organizada e segura
                  ▼
┌─────────────────────────────────────────┐
│          W1E1 Query DSL                 │
│  pipeline tipado, compile-time,         │
│  dialeto certo, filtros seguros,        │
│  paginação, pushdowns                   │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│         Application Layer               │
│    API / Dashboard / Report             │
└─────────────────────────────────────────┘
```

### Problemas que a DSL resolve nesse contexto

Quem consome BigQuery ou Snowflake numa aplicação geralmente enfrenta:

| Problema | Com W1E1 DSL |
|---|---|
| SQL concatenado na mão | ✅ pipeline estruturado |
| SQL injection em filtros dinâmicos | ✅ parâmetros automáticos |
| Typo silencioso em nome de coluna | ✅ compile-time com FromEntity |
| Pushdown de filtro no lugar errado | ✅ Pushdown explícito |
| Paginação inadequada (traz tudo) | ✅ Page() com dialeto certo |
| Query hardcoded, impossível reutilizar | ✅ filtros composáveis |

### O que a DSL nunca fará

```
Definir particionamento de tabelas     → responsabilidade do storage
Orquestrar jobs de ETL                 → responsabilidade do Spark/dbt
Gerenciar clusters                     → responsabilidade da infra
Streaming de eventos em tempo real     → responsabilidade do Flink/Kafka
Treinamento de modelos ML              → responsabilidade do ML pipeline
```

> Tentar fazer isso quebraria a filosofia — viraria um framework, não uma DSL.

---

## Roadmap de Versões

### v1 — Atual
- ✅ SqlServer
- ✅ Pipeline completo (Filter, Join, Group, Window, Union, Page…)
- ✅ Pushdowns
- ✅ Temp tables + índices
- ✅ FromTable / FromRaw / From / FromCte
- ✅ Parâmetros automáticos

### v2 — Abstrações e Entity (curto prazo)
- [ ] `IMetadataProvider` — interface pública
- [ ] `ISourceProvider` — interface pública
- [ ] `IExecutionEngine` — interface pública
- [ ] `EFCoreMetadataProvider` — leitura do `DbContext.Model`
- [ ] `FromEntity<T>()` — resolve tabela via metadata
- [ ] Lambda de dois argumentos no Filter (`m => m.Status, 1`)
- [ ] `PostgresProvider`
- [ ] `OracleProvider`

### v3 — Expression Tree (médio prazo)
- [ ] `ExpressionVisitor` completo
- [ ] Sobrecarga `Where(m => m.Status == 1 && m.CodFilial == "110")`
- [ ] `Contains()` → `IN`
- [ ] `StartsWith/EndsWith/Contains` → `LIKE`
- [ ] Bool implícito
- [ ] `FluentMetadataProvider`
- [ ] `ConventionMetadataProvider`

### v4 — Analítico (médio/longo prazo)
- [ ] `BigQueryProvider`
- [ ] `SnowflakeProvider`
- [ ] `RedshiftProvider`
- [ ] `SynapseProvider`
- [ ] `HttpApiEngine`
- [ ] `DapperEngine`

### v5 — Big Data (longo prazo)
- [ ] `DatabricksProvider` (Spark SQL)
- [ ] `TrinoProvider`
- [ ] `HiveProvider`
- [ ] `GrpcEngine`
- [ ] `IAsyncEnumerable<T>` streaming de resultado
- [ ] Suporte a Delta tables como source

---

## Montagem do Pipeline por Cenário

### SQL Server + EF Core (hoje e v2)

```csharp
var provider = new SqlServerProvider(
    metadata: new EFCoreMetadataProvider(db),
    engine:   new DapperEngine(connection));
```

### BigQuery + mapeamento manual (v4)

```csharp
var provider = new BigQueryProvider(
    metadata: new FluentMetadataProvider(mapping =>
        mapping.Entity<TbMovimento>()
               .ToTable("projeto.dataset.tbmovimento")
               .Property(e => e.CodFilial, "cod_filial")),
    engine: new HttpApiEngine(bigQueryClient));
```

### Databricks + convenção snake_case (v5)

```csharp
var provider = new DatabricksProvider(
    metadata: new ConventionMetadataProvider(
        convention: NamingConvention.SnakeCase),
    engine: new GrpcEngine(sparkThriftServer));
```

### A query DSL não muda em nenhum cenário

```csharp
// Essa query roda em qualquer provider acima — sem alterar uma linha
new SqlQuery<MovimentoDTO>(provider)
    .FromEntity<TbMovimento>("M")
    .Filter(f => f
        .Where<TbMovimento>(m => m.Status == 1 && m.CodFilial == "110"))
    .Sort<TbMovimento>(m => m.DataMovto, "DESC")
    .Page(0, 100)
```

---

## Comparativo de Posicionamento

| | IQueryable / EF | Dapper | W1E1 DSL |
|---|---|---|---|
| Abstrai SQL | ✅ totalmente | ❌ | ❌ intencional |
| Organiza SQL | ❌ | ❌ | ✅ |
| CRUD simples | ✅ ótimo | ✅ ok | overhead |
| Query analítica complexa | ⚠️ SQL ruim | ⚠️ string manual | ✅ |
| Temp table + índice | ❌ | ⚠️ manual | ✅ nativo |
| Window functions | ⚠️ frágil | ⚠️ manual | ✅ primeira classe |
| CTE recursiva | ❌ | ⚠️ manual | ✅ |
| Multi-provider / Big Data | ❌ | ❌ | ✅ (roadmap) |
| Compile-time safety | ✅ | ❌ | ✅ (v2+) |
| Analista lê o código | ❌ LINQ | ✅ SQL inline | ✅ SQL inline |
| SQL injection seguro | ✅ | ✅ | ✅ |

---

## Princípios que Nunca Mudam

```
SQL First          — a DSL organiza SQL, não esconde SQL
Pipeline Driven    — fluxo previsível, ordem garantida
Compile-time Guided — erros em compilação, não em produção
Escape Hatch Always — raw string sempre disponível em qualquer ponto
No Magic           — o que entra é o que sai, sem surpresas
```

> A DSL NÃO é ORM.  
> A DSL NÃO substitui SQL.  
> A DSL NÃO processa big data.  
>  
> A DSL **organiza** SQL complexo sobre qualquer storage.