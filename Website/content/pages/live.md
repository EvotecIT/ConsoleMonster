---
title: Live dashboards
description: Recorded runs of ConsoleMonster's live showcases, replayed frame by frame. Threads post updates; the session draws about 15 frames a second.
social_card_image: /site/nav/live-dark.svg
meta.social_title: "Live console dashboards"
slug: live
layout: live
meta.seo_title: Live console dashboards | ConsoleMonster
---

## Run them yourself

Every dashboard on this page is a complete program; the recordings are what those programs drew in a terminal window of the size shown.

1. `dotnet new console -n Live`, then `cd Live` and `dotnet add package ConsoleMonster`.
2. Replace `Program.cs` with a program from this page (Copy on its panel).
3. `dotnet run`. The stress test takes the seconds to run (`dotnet run -- 12`); the replay takes the recording and the speed (`dotnet run -- assessment-run.jsonl 2`), so download the recording into the project folder first.

The PowerShell build pipeline runs with `Install-Module ConsoleMonster -Scope CurrentUser`, then the script.

Without JavaScript, each player shows a frame from the middle of its run. With reduced motion, nothing plays until you press play.