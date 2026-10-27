using ConsoleMonster.Extensions.Logging;
using Microsoft.Extensions.Logging;

// Every level, a scope, a structured message and an exception.
using ILoggerFactory factory = LoggerFactory.Create(
    logging => logging.SetMinimumLevel(LogLevel.Trace)
        .AddConsoleMonster());
ILogger orders = factory.CreateLogger("Shop.Services.Orders");
ILogger cache = factory.CreateLogger("Shop.Infrastructure.Cache");

cache.LogTrace("Probing {Count} cache shards", 4);
cache.LogDebug("Shard {Shard} answered in {Ms} ms", "eu-1", 12);
using (orders.BeginScope("order {Id}", 1042)) {
    orders.LogInformation("Payment accepted: {Amount} EUR", 129.90m);
    orders.LogWarning("Stock is low for {Sku}: {Left} left", "A-17", 3);
}
try {
    throw new TimeoutException("The payment gateway did not answer within 30 s.");
} catch (TimeoutException ex) {
    orders.LogError(ex, "Refund for order {Id} failed", 1038);
}
orders.LogCritical("Queue {Queue} is not draining", "orders-eu");
orders.LogInformation("Done; {Processed} orders processed", 57);
