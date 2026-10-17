---
title: Benchmarks
description: What a ConsoleMonster frame costs, measured by the repository's own benchmark program and stress test. Every number on this page comes from their output.
slug: benchmarks
layout: benchmarks
meta.seo_title: ConsoleMonster rendering benchmarks
---

## Run them yourself

```powershell
dotnet run --project ConsoleMonster.Benchmarks -c Release -- 600
dotnet run --project ConsoleMonster.Examples -c Release -f net8.0 -- stress 20
dotnet run --project ConsoleMonster.Examples -c Release -f net8.0 -- large-table 50000
```

The first prints the per-frame figures above. The stress test opens a live dashboard and prints one `STRESS` line with the session's counters when it ends. `large-table` measures printing a long table in blocks.