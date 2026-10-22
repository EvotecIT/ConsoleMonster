using ConsoleMonster.Interactive;

var server = new {
    Name = "web-01",
    Owner = new { Name = "Ada", Groups = new[] { "Operators", "Readers" } },
    Services = new[] {
        new { Name = "web", Status = "Running", Port = 443 },
        new { Name = "metrics", Status = "Running", Port = 9090 }
    },
    Enabled = true
};
new ObjectExplorer(server, rootPath: "$server")
    .WithTitle("Server configuration")
    .Show();
