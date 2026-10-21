---
title: Benchmarks
description: What a ConsoleMonster frame costs, measured by the repository's own benchmark program and stress test. Every number on this page comes from their output.
slug: benchmarks
layout: benchmarks
meta.seo_title: ConsoleMonster rendering benchmarks
---

## Run them yourself

The benchmark program and the large-table example live in the repository, so these steps start with a clone (they need the .NET 8 SDK):

1. Get the source:

   ```bash
   git clone https://github.com/EvotecIT/ConsoleMonster.git
   cd ConsoleMonster
   ```

2. Measure 600 frames; this prints the per-frame figures above:

   ```bash
   dotnet run --project ConsoleMonster.Benchmarks -c Release -- 600
   ```

3. Print a long table in blocks and measure it:

   ```bash
   dotnet run --project ConsoleMonster.Examples -c Release -f net8.0 -- large-table 50000
   ```

The stress test needs no clone: it is a complete program on the [live dashboards page](/live/) (`dotnet run -- 12`), and it prints one `STRESS` line with the session's counters when it ends.