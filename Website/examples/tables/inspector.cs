using ConsoleMonster;
using ConsoleMonster.Data;
using ConsoleMonster.Widgets;

Dictionary<string, object?> zone = new() {
    ["Name"] = "evotec.xyz",
    ["Serial"] = 2351234567L,
    ["Signed"] = false,
    ["Soa"] = new Dictionary<string, object?> {
        ["Primary"] = "dana.ns.cloudflare.com",
        ["Refresh"] = TimeSpan.FromSeconds(10000),
        ["Expire"] = TimeSpan.FromSeconds(604800)
    },
    ["NameServers"] = new List<Dictionary<string, object?>> {
        new()
        {
            ["Host"] = "dana.ns.cloudflare.com",
            ["Address"] = "108.162.192.103",
            ["LatencyMs"] = 12
        },
        new()
        {
            ["Host"] = "kirk.ns.cloudflare.com",
            ["Address"] = "172.64.33.149",
            ["LatencyMs"] = 14
        }
    },
    ["Tags"] = new[] { "production", "cloudflare", "doh" }
};
Terminal.Write(new InspectorView(zone, new InspectorOptions { MaxDepth = 3 }, "evotec.xyz zone"));
