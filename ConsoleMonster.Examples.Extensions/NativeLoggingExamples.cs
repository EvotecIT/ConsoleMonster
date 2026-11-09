using System;
using ConsoleMonster.Extensions.NLog;
using NLog.Config;
using Serilog;

namespace ConsoleMonster.Examples.Extensions {
    internal static class NativeLoggingExamples {
        internal static void Serilog() {
            using var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.ConsoleMonster().CreateLogger();
            var log = logger.ForContext("SourceContext", "Shop.Orders").ForContext("Host", "node-1");
            log.Verbose("Probing {Count} cache shards", 4);
            log.Debug("Shard {Shard} answered in {Ms} ms", "eu-1", 12);
            log.Information("Order {OrderId} for {Customer} totals {Total:0.00}", 1042, "Ada", 42.50m);
            log.Warning("Stock is low for {Sku}: {Left} left", "A-17", 3);
            log.Information("Next attempt at {When:yyyy-MM-dd HH:mm}", new DateTime(2026, 11, 9, 15, 30, 0));
            log.Information("Shipment {@Shipment}", new { OrderId = 1042, Destination = "Warsaw" });
            try { throw new TimeoutException("The payment gateway did not answer within 30 s."); }
            catch (TimeoutException ex) { log.Error(ex, "Refund for order {OrderId} failed", 1038); }
            log.Fatal("Queue {Queue} is not draining", "orders-eu");
        }

        internal static void NLog() {
            var configuration = new LoggingConfiguration();
            configuration.AddRule(global::NLog.LogLevel.Trace, global::NLog.LogLevel.Fatal,
                new ConsoleMonsterTarget { Name = "readable" });
            using var factory = new global::NLog.LogFactory { Configuration = configuration };
            var log = factory.GetLogger("Shop.Orders").WithProperty("Host", "node-1");
            log.Trace("Probing {Count} cache shards", 4);
            log.Debug("Shard {Shard} answered in {Ms} ms", "eu-1", 12);
            log.Info("Order {OrderId} for {Customer} totals {Total:0.00}", 1042, "Ada", 42.50m);
            log.Warn("Stock is low for {Sku}: {Left} left", "A-17", 3);
            log.Info("Next attempt at {When:yyyy-MM-dd HH:mm}", new DateTime(2026, 11, 9, 15, 30, 0));
            try { throw new TimeoutException("The payment gateway did not answer within 30 s."); }
            catch (TimeoutException ex) { log.Error(ex, "Refund for order {OrderId} failed", 1038); }
            log.Fatal("Queue {Queue} is not draining", "orders-eu");
        }
    }
}
