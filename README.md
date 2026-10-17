# DotnetLeet Trade

Back office of a computer hardware reseller, built in public as the system behind the
dotnetleet.com series "LLM Inside the Monolith". One .NET modular monolith, one PostgreSQL
database, one local model, and an LLM added only where it does a job.

The rule that holds across every level: the model reads and proposes, the code checks,
computes and decides. Prices, totals, stock and approvals never come from the model.

## Level 1: a supplier email becomes a draft offer

Paste a supplier's email. The model extracts the fields into a typed draft. The code matches
every line to the catalog by supplier code, part number or SKU, computes line totals and the
subtotal, compares prices to what we paid last time, and lists what is wrong. A person corrects
and confirms. Unknown values stay unknown. The same email twice does not call the model twice.
No purchase order is created at this level.

## Run it

Requires Docker Desktop and the .NET 10 SDK. Everything else comes from the AppHost.

    dotnet run --project DotnetLeet.Trade.AppHost

The Aspire dashboard opens with PostgreSQL, pgAdmin, Ollama with the `llama3.2` model, the API
and the Blazor web app. The first start downloads the model (about 2 GB) into a Docker volume.
The web app seeds the catalog on first run: 42 products, 5 suppliers, purchase prices, stock
and two open purchase requests.

## Projects

| Project | What it holds |
|---|---|
| `DotnetLeet.Trade.Domain` | Catalog, Procurement and Inventory modules. Entities, rules, matching. No package references. |
| `DotnetLeet.Trade.Application` | Use cases, ports, queries, and the model instructions as a versioned embedded resource. References Domain only. |
| `DotnetLeet.Trade.Infrastructure` | EF Core persistence, the model adapter (`Microsoft.Extensions.AI`), seed data, the composition of ports to adapters. |
| `DotnetLeet.Trade.ApiService` | Minimal API host. Gets the database and the model from Aspire. |
| `DotnetLeet.Trade.Web` | Blazor Server review UI. Talks to the API, references Application for the DTOs only. |
| `DotnetLeet.Trade.AppHost` | Aspire orchestration: PostgreSQL, Ollama, API, Web. |
| `DotnetLeet.Trade.ServiceDefaults` | OpenTelemetry, health checks, service discovery. |
| `DotnetLeet.Trade.Evals` | Architecture tests, unit tests with a canned model, and the eval set. |
| `DotnetLeet.Trade.Tests` | Aspire integration smoke test. |

## Tests and evals

    dotnet test DotnetLeet.Trade.Evals            # architecture + unit tests, no model, no database

    set TRADE_EVALS=1
    dotnet test DotnetLeet.Trade.Evals            # the twelve supplier emails against the real model

The evals need an Ollama endpoint. `TRADE_OLLAMA_ENDPOINT` (default `http://localhost:11434`)
and `TRADE_MODEL` (default `llama3.2:latest`). Under Aspire the Ollama port is random, `docker ps`
shows it. Each case is one test. Every field is one check, and `bin/.../evals-report.md` holds the
per-case table after a run. The history of runs and what each change bought is in `docs/evals/`.

Level 1 result on the development set: 215 of 231 field checks with llama3.2 on CPU, up from 155 on the
first run, scored with one checker across all five runs. The five held-out emails, run once with the
system frozen: 78 of 89 fields, 0 of 5 emails fully correct. The suppliers, emails, catalog and purchase
history are synthetic demo data. `docs/evals/README.md` explains how the run comparison was reconstructed
from the saved reports, because the raw model answers of the first two runs were not kept.

## Levels

| Level | Capability | Status |
|---|---|---|
| 0 | Catalog, suppliers, purchase requests, approval rules, stock | done |
| 1 | Supplier email to draft offer, matching, totals, review | done |
| 2 | Clarification by email thread, working memory | |
| 3 | PDF offers and contract retrieval with sources | |
| 4 | Offer comparison and purchase order draft, tools with approval | |
| 5 | Agent for a complex purchase request | |
| 6 | Reliability: CI evals, model comparison, cost per task | |
| 7 | Sales and returns | |
| 8 | MCP server, optional subagents | |
