using ConsoleMonster.Live.Tails;

// Run beside synthetic.log. Omit Count to keep following until q or Escape.
using var source = new FileLogTail("synthetic.log", initialLineCount: 16);
ConsoleLogTail.Run(source.ReadAvailable, new LogTailOptions {
    Highlight = "database",
    Count = 1
});
