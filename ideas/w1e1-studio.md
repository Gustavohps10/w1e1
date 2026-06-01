# W1E1 Studio — Query Performance Lab

## Visão Geral

O **W1E1 Studio** é uma ferramenta de desenvolvimento baseada em execução dinâmica de código C# com foco em análise de SQL gerado, performance de execução e comparação de providers.

Ele funciona como um **"Swagger para Query Builders"**, permitindo que desenvolvedores experimentem, meçam e otimizem consultas sem recompilar o projeto principal.

---

## Objetivo

Permitir que o desenvolvedor:

- Edite queries dinamicamente em um editor tipo IDE
- Execute código C# sem recompilar o projeto principal
- Gere e visualize SQL instantaneamente
- Meça performance de geração e execução
- Compare diferentes estratégias de execução (EF Core, Dapper, ADO.NET, W1E1)

---

## Interface (Frontend)

### Editor

- Baseado em **Monaco Editor**
- Suporte a C# com tipagem básica
- Um único arquivo de experimentação por sessão
- Execução controlada (sandbox)

### Painel de Resultados

- SQL gerado final
- Tempo de geração do SQL
- Tempo de execução no banco
- Métricas de memória
- Logs de execução detalhados

### Modos de Execução

| Modo | Descrição |
|---|---|
| **Run** | Execução única |
| **Benchmark** | Execução repetida com estatísticas |
| **Compare** | Execução entre múltiplos providers |

---

## Backend Architecture

### 1. Roslyn Script Engine

Responsável por executar código C# dinamicamente:

- Compila código em memória (sem gerar arquivo físico)
- Converte script em Assembly runtime
- Gera delegate executável

**Fluxo:**

```
C# Text → Roslyn Compilation → In-memory Assembly → Executable Delegate
```

---

### 2. Execution Engine

Responsável por executar o código compilado:

- Instancia `SqlQuery<T>`
- Executa `.ToSqlFull()`
- Opcionalmente executa SQL no banco real
- Captura SQL final e parâmetros

---

### 3. Benchmark Engine

Responsável por medições de performance:

- Executa múltiplas iterações (ex: 10–100)
- Remove warm-up inicial
- Calcula:
  - Média
  - P95 / P99
  - Min / Max
- Coleta GC / allocations (opcional)

---

### 4. Metrics Collector

Coleta métricas do pipeline:

- Tempo de geração de SQL
- Tempo de execução no banco
- Tempo total do pipeline
- Allocations em memória
- Interceptação de queries

---

## Regra Crítica de Métricas

### ❌ Não medir

- Tempo de compilação Roslyn
- Inicialização do runtime
- Setup de assemblies

### ✅ Medir

- Execução pós-compilação
- Geração de SQL
- Execução no banco
- Pipeline completo da query

---

## Warm-up Strategy

A primeira execução **nunca** entra no benchmark.

**Fluxo correto:**

```
1. Compilação         → ignorada
2. Warm-up execution  → ignorada
3. Execuções reais    → medidas
```

---

## Exemplo de Script

```csharp
var query = new SqlQuery<DTO>(SqlServerProvider.Instance)
    .From(BaseSqlReal)
    .Filter(f => f.And("Status", "A"))
    .OrderBy(x => x.Id, "ASC")
    .Page(0, 50, PaginationMode.RowNumber);

return query.ToSqlFull();
```

---

## Benchmark Output

```
SQL Generation:
  Avg: 0.42 ms
  P95: 0.51 ms

SQL Execution:
  Avg: 3.8 ms
  P95: 5.1 ms

Memory:
  Allocations: 12 KB

Total Pipeline:
  Avg: 4.3 ms
```

---

## Roslyn Execution Flow

```
1. Usuário escreve código no editor
        ↓
2. Backend envia string para Roslyn
        ↓
3. Roslyn compila em Assembly em memória
        ↓
4. Engine executa delegate gerado
        ↓
5. Resultados são interceptados e retornados
```

---

## Segurança

Como o código é dinâmico, a execução é controlada:

- Execução isolada (sandbox)
- Sem acesso irrestrito ao filesystem
- Sem acesso livre à rede
- API limitada ao W1E1 DSL e query builder

---

## Comparação de Providers (Futuro)

O Studio poderá executar a mesma query em múltiplos providers simultaneamente e comparar os resultados lado a lado.

**Providers planejados:**

- EF Core
- Dapper
- ADO.NET
- W1E1 Provider

**Métricas comparadas:**

| Métrica | EF Core | Dapper | ADO.NET | W1E1 |
|---|---|---|---|---|
| Tempo de geração SQL | ? ms | ? ms | ? ms | ? ms |
| Tempo de execução | ? ms | ? ms | ? ms | ? ms |
| Allocations | ? KB | ? KB | ? KB | ? KB |
| SQL gerado | auto | manual | manual | pipeline |

---

## Valor do Projeto

O W1E1 Studio funciona como:

- **Laboratório de performance SQL** — dados reais, não estimativas
- **Ferramenta de debugging avançado** — visualiza SQL gerado instantaneamente
- **Ambiente de experimentação DSL** — itera sem recompilar o projeto
- **Vitrine técnica** — demonstra maturidade e diferencial do ecossistema W1E1

---

## Resultado Final

Um ambiente onde o desenvolvedor pode:

- Escrever queries fluent em tempo real
- Visualizar SQL gerado instantaneamente
- Medir performance real de geração e execução
- Comparar estratégias de acesso a dados com dados concretos
- Otimizar o builder com base em evidências, não suposições

```
Escreve query fluent
        ↓
Visualiza SQL gerado
        ↓
Mede performance real
        ↓
Compara providers
        ↓
Otimiza com dados concretos
```