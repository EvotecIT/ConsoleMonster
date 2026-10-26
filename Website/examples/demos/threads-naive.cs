// Twelve servers patched in parallel, each writing its progress straight to the console.
string[] servers = Enumerable.Range(1, 12).Select(n => $"web{n:00}").ToArray();
string[] steps = { "copying", "stopping", "swapping", "starting" };
await Parallel.ForEachAsync(
    servers,
    new ParallelOptions { MaxDegreeOfParallelism = 12 },
    async (server, token) => {
    foreach (string step in steps) {
        Console.Write($"{server} {step}");
        await Task.Delay(Random.Shared.Next(40, 240), token);
        Console.WriteLine(" ... done");
    }
}
);
