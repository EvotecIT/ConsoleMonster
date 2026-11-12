# Live session internals

How a `LiveSession` applies updates and redraws. The site's
[Live sessions](https://consolemonster.dev/docs/live-sessions/) page covers what to build; this file keeps the mechanics.

## Update queue and render thread

Updates are `LiveEvent`s addressed to widgets by `WidgetId`. They go into a lock-free queue and are applied on the
render thread, which composes the widget tree, diffs it against the previous frame and writes only the cells that
changed, about 15 times a second. A `RegionWidget` swap goes through the same queue, so it is thread-safe and lands
between frames.

## Resizing

A size change clears the screen and redraws it. While a window is dragged, redraws are coalesced to at most one every
100 ms.
