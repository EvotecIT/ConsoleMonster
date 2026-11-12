# Recording internals

How session recording redacts output and how export pacing computes reading time. The site's
[Recording and replay](https://consolemonster.dev/docs/recording/) page covers what to run; this file keeps the mechanics
behind it.

## Redaction

Redaction runs before writing the cast or chapter metadata, including secrets split across reads or colour codes. Long
lines and cursor-positioned repaints stream without a 64 KiB record cutoff. The recorder keeps raw and visible overlap
for built-in credentials and custom patterns whose maximum match span can be proved, up to 8,192 UTF-16 characters.
Rules match the original output, so overlapping patterns cannot hide a secret by replacing part of it first. Chapter
markers and resizes preserve their ordering without breaking redaction across them.

Patterns with unbounded quantifiers, anchors, lookarounds, backreferences or unsupported syntax use complete-record
matching. A record ends at a newline, carriage return, stop control or end of capture. If uncommitted redaction or
terminal context reaches 1,048,576 UTF-16 characters, the cast shows an explicit omission message and resumes after the
next line break. A pattern timeout also fails closed with a visible message. The companion `.cast.json` counts these
cases in `redactionFallbacks`. This buffer safeguard is separate from `-MaxBytes`, which caps the total cast bytes.

## Playback of synchronized redraws

Playback honours synchronized redraw boundaries, keeping completed frames within the export frame budget. When redraws
arrive together, it spaces them at the requested frame rate and moves later frames and chapters with them; the final
hold stays intact.

## Pacing

New visible text is counted across a burst of output, then the settled screen gets a bounded reading budget:

| Pace | Screen min–max | Per character | Per line |
|---|---:|---:|---:|
| Slow | 2–16 s | 35.71 ms | 140 ms |
| Normal | 1.2–8 s | 23.81 ms | 80 ms |
| Fast | 0.55–4 s | 13.89 ms | 40 ms |

The budget is the new visible character count times its per-character time, plus the allowance for each changed line,
bounded by the minimum and maximum. Slow gives about 1.5–2× Normal's reading time; Fast uses about half. The text rates
(28 / 42 / 72 characters per second), line allowances and minimum holds follow ChartForgeX's terminal playback presets.
The larger whole-screen maximum keeps Slow distinct on dense output.

Source time already spent on the settled frame counts towards its budget; export adds only the missing reading time.
Speed and per-chapter speed scale this budget. Explicit marker, chapter and final holds are additional and keep their
durations. Colour-only changes add no reading time.

For 225 new characters on three lines, the reading budgets are 8.456 / 5.597 / 3.245 seconds for Slow / Normal / Fast,
matching ChartForgeX's terminal typing and line timing for the same text.

In the pipeline recording used to check these presets, the same cast exports to a 1920×1080 GIF lasting 15.51 seconds
at Source, 19.51 at Fast, 23.51 at Normal and 31.51 at Slow. These exports disable idle capping and include a two-second
final hold and the playable cadence for retained instantaneous redraws.

## Hold markers in the cast

Holds are stored as `cm` metadata events alongside ordinary `m` chapter events; reading and writing a cast preserves
both, with no sidecar required. Other asciicast players may ignore the extra metadata.
