[Spectre.Console.AnsiConsole]::Status().Start("Thinking...", {
        #// Simulate some work
        [Spectre.Console.AnsiConsole]::MarkupLine("Doing some work...");
        #Thread.Sleep(1000);

        #// Update the status and spinner
        $ctx.Status("Thinking some more");
        $ctx.Spinner([Spectre.Console.Spinner+Known]::Star);
        $ctx.SpinnerStyle([Spectre.Console.Style]::Parse("green"));

        #// Simulate some work
        [Spectre.Console.AnsiConsole]::MarkupLine("Doing some more work...");
        #Thread.Sleep(2000);
    }
);