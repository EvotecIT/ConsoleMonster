using ConsoleMonster;
using ConsoleMonster.Widgets;

// Illustrative completed-command data; the PowerShell feature supplies these values from the session.
Terminal.Write(new ShellPrompt {
    Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "src", "Operations", "Deployment", "Scripts"),
    Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    Branch = "feature/themed-prompt",
    Dirty = true,
    Succeeded = true
});
Terminal.Write(new ShellPrompt {
    Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "src", "Operations", "Deployment", "Scripts"),
    Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    Branch = "feature/themed-prompt",
    Dirty = true,
    Succeeded = false,
    Duration = TimeSpan.FromSeconds(3.4)
});
