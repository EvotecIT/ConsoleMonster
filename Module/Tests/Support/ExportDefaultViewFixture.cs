using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Display;
using ConsoleMonster.Widgets;

namespace ExportDefaultViews {
    public sealed class Record {
        public string Name { get; set; }
        public string Detail { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class RecordList : List<Record> { }

    // Product-shaped callbacks exercise the public registration and export boundaries.
    public static class Fixture {
        public static ModuleLease RegisterCollection() {
            ModuleLease lease = DefaultDisplay.RegisterModule("ExportCollection", new Version(1, 0), new[] { typeof(RecordList).FullName });
            lease.Mode = DefaultDisplayMode.Auto;
            lease.Register(typeof(RecordList).FullName, "ExportCollection.List", new Version(1, 0), DefaultDisplayBatch.Single,
                null, (rows, context) => TextBlock.Plain("collection-marker"), runtimeType: typeof(RecordList));
            return lease;
        }

        public static ModuleLease Register(string batch, long bytes, string failure) {
            ModuleLease lease = DefaultDisplay.RegisterModule("ExportDefaultViews", new Version(1, 0), new[] { typeof(Record).FullName },
                source: failure.StartsWith("resolve-") ? DefaultDisplaySource.CommandModule : DefaultDisplaySource.Product);
            lease.Mode = DefaultDisplayMode.Auto;
            if (failure.StartsWith("resolve-")) {
                lease.JoinCommandModule((owner, topLevel, transcript) => { throw new InvalidOperationException("Export must not create a host sink"); },
                    value => {
                        if (((Record)value.BaseObject).Name == "bad") {
                            if (failure == "resolve-member") { throw new MemberAccessException("application override"); }
                            throw new TypeLoadException("application override");
                        }
                        return false;
                    });
            }
            DefaultDisplayRegistration view = lease.Register(typeof(Record).FullName, "ExportDefaultViews.Record", new Version(1, 0),
                (DefaultDisplayBatch)Enum.Parse(typeof(DefaultDisplayBatch), batch), null,
                (rows, context) => Build(failure), new DefaultDisplayBudget(records: 1, bytes: bytes),
                value => {
                    if (((Record)value).Name == "bad") {
                        if (failure == "scope-member") { throw new MemberAccessException("application selector"); }
                        if (failure == "scope-type") { throw new TypeLoadException("application selector"); }
                    }
                    return string.Empty;
                }, typeof(Record));
            view.SingleRecordBuilder = (row, context) => Build(failure);
            view.CollectedBuilder = (rows, context) => Build(failure);
            return lease;
        }

        private static IWidget Build(string failure) {
            if (failure == "builder") { throw new InvalidOperationException("application builder"); }
            if (failure == "output") { return TextBlock.Plain(new string('x', 2000)); }
            if (failure == "measure" || failure == "render" || failure == "late-render" || failure == "cancel-render") { return new FaultWidget(failure); }
            return TextBlock.Plain("registered-marker");
        }

        private sealed class FaultWidget : IWidget {
            private readonly string _failure;
            private int _renders;
            internal FaultWidget(string failure) { _failure = failure; }
            public Size Measure(Constraints constraints, RenderContext context) {
                if (_failure == "measure") { throw new InvalidOperationException("application measure"); }
                return new Size(20, 1);
            }
            public void Render(Region region, RenderContext context) {
                if (++_renders > 1 && _failure == "cancel-render") { throw new OperationCanceledException("application rendering canceled"); }
                if (_failure == "render" || _renders > 1) { throw new InvalidOperationException("application render"); }
                TextBlock.Plain("partial-view-marker").Render(region, context);
            }
        }
    }
}
