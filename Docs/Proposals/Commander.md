# ConsoleMonster Commander UX spec

Status: approved direction, 25 October 2026. ConsoleMonster Commander is the phase 3 flagship: a two-panel, keyboard-first file manager in the Norton/Midnight Commander tradition, built only on ConsoleMonster's own full-screen controls. This spec fixes what the app does so phase 3's data grid, tabs and terminal handoff are designed for a real consumer.

## Screen layout

Two equal panels fill the screen; the active one has the accent border and the highlighted row. One command line and one F-key bar sit below. At 120×30:

```text
╭─ ~/projects ─────────────────────────────── Name ↓ ─╮╭─ /srv/backup ───────────────────────────── Name ↓ ─╮
│ Name                     Size  Modified      ││ Name                     Size  Modified      │
│ ..                           ↑  2026-10-25 ││ ..                          ↑  2026-10-24 │
│ ▸ ConsoleMonster          …  2026-10-25 ││ ▸ daily                 4.1 G  2026-10-25 │
│ ▸ OfficeIMO            2.3 G  2026-10-24 ││ ▸ weekly               12.0 G  2026-10-20 │
│▉✓ report.csv           84 K  2026-10-25 ││   manifest.json         2 K  2026-10-25 │
│ ✓ notes.md              6 K  2026-10-23 ││                                        │
│   build.ps1             3 K  2026-10-22 ││                                        │
├─ 2 marked · 90 K ──────────── 7 items ─┤├────────────────────── 3 items · 16.1 G free ─┤
│ report.csv · 84 K · rw-r--r--          ││ /srv/backup · ext4                    │
╰────────────────────────────────────────╯╰────────────────────────────────────────╯
~/projects> _
 1Help 2Menu 3View 4Edit 5Copy 6Move 7Mkdir 8Delete 9Palette 10Quit
```

- **Panel header:** current path on the left (shortened in the middle when long), sort column and direction on the right.
- **Rows:** folders first with `▸`, marked rows with `✓` and the accent colour, the cursor row highlighted; size right-aligned, `…` while a folder size is still computing.
- **Panel footer:** marks and their total on the left, item count and free space on the right; a one-line detail of the current entry under it.
- **Command line:** the active panel's path as the prompt; output from commands appears after Ctrl+O.
- **Key bar:** numbers in the accent colour, labels change with context (a dialog shows its own keys).

## Keymap

Every action has a key; the mouse is never required. F-keys follow Norton/Midnight Commander so muscle memory carries over, and each has an alternative for terminals that swallow F-keys (macOS Terminal, some SSH clients).

| Key | Alternative | Action |
| --- | --- | --- |
| Tab | | Switch the active panel |
| ↑ ↓ PgUp PgDn Home End | | Move in the active panel |
| Enter | | Open folder; on a file, run it in the command line (asks first for scripts) |
| Backspace | Ctrl+PgUp | Parent folder |
| Insert | Space | Mark or unmark the row and move down |
| + / − / * | | Mark by pattern / unmark by pattern / invert marks |
| F3 | Alt+3 | View file (pager, syntax-highlighted via CodeView) |
| F4 | Alt+4 | Edit file (TextArea; external `$EDITOR` when set) |
| F5 | Alt+5 | Copy marked (or current) to the other panel's folder |
| F6 | Alt+6 | Move or rename |
| F7 | Alt+7 | New folder |
| F8 | Alt+8 / Delete | Delete marked (or current), to the recycle bin or trash where available |
| F9 | Alt+9 | Command palette (menu bar comes later) |
| F10 | Alt+0 / Ctrl+Q | Quit |
| Ctrl+O | | Hide panels and show the shell's output; any key returns |
| Ctrl+R | | Refresh both panels |
| Ctrl+S or typing letters | | Quick search in the active panel |
| Alt+F1 / Alt+F2 | | Choose drive or root for the left / right panel |
| Ctrl+\\ | | Swap panels |
| Ctrl+U | | Same folder in both panels |

The key bar always shows the ten F-key labels for the current context. A key that does nothing in the current context is shown dimmed rather than hidden, so the bar never shifts.

## Behaviour

The app never blocks: every file operation runs in the background with progress in a dialog, and the panels stay usable.

- **Marking.** Marked rows show a mark glyph and accent colour; the panel footer shows the count and total size (`12 marked · 340 MB`). F5/F6/F8 act on marked rows, or on the current row when nothing is marked.
- **Copy and move (F5/F6).** A dialog shows the source count and size, an editable destination (the other panel's folder), and options: overwrite policy (ask, skip, overwrite older, overwrite all) and preserve timestamps. Enter starts. Progress shows the current file, overall percent, bytes per second and time left, with Cancel always available. Cancel stops after the current file and reports what was done.
- **Conflicts.** For each existing destination file, ask once with both sizes and dates: Overwrite, Skip, Overwrite all, Skip all, Rename, Cancel. Never overwrite silently.
- **Delete (F8).** Default focus is No. Recycle bin or trash where the OS has one; permanent delete needs Shift+F8 and states it in the dialog title.
- **Errors.** Access denied, path too long, file in use: the operation continues with the next item, and a summary lists failures at the end with retry and skip-all choices. An error never closes the app or leaves a half-drawn screen.
- **Large folders.** A 10,000-entry folder opens without a pause: rows render as they scroll, sizes for folders are computed lazily (`…` until known), and sort happens once per listing.
- **Quick search.** Typing letters filters by prefix and jumps to the first match; Esc clears. The filter text shows in the panel's bottom border.
- **Command line.** One line at the bottom, always present. Enter runs the command in the active panel's folder: the app hands the terminal to the child process (panels hidden, real TTY, colours and prompts work), then takes it back and refreshes both panels. Ctrl+O shows the last output any time. Running `cd somewhere` changes the active panel's folder instead of spawning a shell.
- **Refresh.** Panels refresh after the app's own operations and on Ctrl+R; file-system watching is optional and off over network paths.
- **State.** Remembers each panel's last folder, sort and view per machine. Never stores file contents or commands with secrets.

## Degrading gracefully

The same app must be usable everywhere ConsoleMonster runs; each limitation removes polish, never a function.

| Situation | What changes |
| --- | --- |
| No mouse (or `-NoMouse`) | Nothing functional; every action has a key |
| F-keys swallowed (macOS Terminal, some SSH clients) | Alt+number alternatives; the key bar shows them when F-keys are detected missing or the user sets it |
| SSH | Works over a PTY; slower links get fewer redraws (coalesced updates), never dropped keys |
| tmux | Uses tmux's mouse routing when enabled; prefix key never captured by the app |
| Narrow terminal (< 80 columns) | One panel at a time, Tab flips between them; columns drop by priority (date, then size) |
| Tiny terminal (< 40×12) | A clear "make the window larger" message; no corrupted layout |
| ASCII glyph tier / NO_COLOR | Marks and states use ASCII glyphs and inverse video; nothing relies on colour alone |
| Windows conhost | Native console input; Quick Edit mode suspended while running and restored on exit |
| Windows PowerShell 5.1 | Fully supported; same behaviour as PowerShell 7 |

## What phase 3 must provide

These are the requirements the commander sets for the phase 3 controls and host. Each is a general capability, not commander-specific code.

| Requirement | Control or host piece | Why the commander needs it |
| --- | --- | --- |
| Two independent data grids with their own focus, selection and marks | Embeddable `DataGrid` extracted from `Show-ConsoleGrid` | The two panels |
| Virtualised rows (render only visible) with stable row identity | `DataGrid` + scroll view | 10,000-entry folders without a pause |
| Lazy cell values that fill in later | `DataGrid` column with async value and placeholder | Folder sizes |
| Prefix quick search and pattern marking | `DataGrid` search and mark API | Typing to jump; + / − / * |
| Context-dependent key bar with dimmed entries | `FooterBar` driven by the command registry | F-key labels that change with focus |
| Modal dialogs with typed results, default button and focus restore | Phase 2 dialog, plus form fields inside | Copy/move options, conflicts, delete confirm |
| Background operation with progress, cancel and a final summary | Phase 2 actions + progress widget in a dialog | F5/F6/F8 |
| Terminal handoff: suspend the app, give a child process the real TTY, restore | Host `SuspendForChild` / `Resume` | The command line and Ctrl+O |
| Captured last-output buffer | Host output pane | Ctrl+O |
| Panel swap and same-folder operations | Layout regions that can swap content | Ctrl+\\, Ctrl+U |
| Narrow mode: one region visible at a time | Responsive layout rule | < 80 columns |

If any of these turns out hard, raise it during phase 3 design rather than special-casing it in the commander.

## Out of scope for the first version

- Archive browsing (zip, tar) as folders
- FTP, SFTP or cloud storage panels
- A full-featured editor (F4 uses TextArea or `$EDITOR`)
- File comparison and folder sync
- A drop-down menu bar (F9 opens the command palette instead)
- User-defined menus and file-type associations
- Tree or quick-view panels

Shipping: first as a gallery showcase with all three code tabs, then as a `cmon commander` subcommand once `cmon` exists.
