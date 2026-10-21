---
title: Live dashboards
description: Recorded runs of ConsoleMonster's live showcases, replayed frame by frame. Threads post updates; the session draws about 15 frames a second.
social_card_image: /site/nav/live-dark.svg
meta.social_title: "Live console dashboards"
slug: live
layout: live
meta.seo_title: Live console dashboards | ConsoleMonster
---

## How these replays were made

The stress test, the build pipeline and the stream-mode run were recorded by the site tool: it starts each showcase inside a Windows pseudo console of a fixed size, pins the terminal profile with `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_THEME` and `CONSOLEMONSTER_CULTURE`, and saves the VT byte stream with its timing as asciicast. The tool's own VT parser replays that stream into cells, samples it at a fixed rate and keeps only the rows that change. The assessment comes from its JSON-lines recording instead, played into the dashboard in-process, the way ConsoleMonster's recording export does.

Without JavaScript, each player shows its last frame. With reduced motion, nothing plays until you press play.