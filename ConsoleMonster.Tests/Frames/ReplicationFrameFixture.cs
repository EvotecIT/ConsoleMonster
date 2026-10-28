using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Frames;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Tests.Frames;

/// <summary>
/// A seeded replication-shaped estate and the frame a product would declare for it, built only from the public API.
/// 5, 50 and 100 domain controllers match the scale set in the dashboard design.
/// </summary>
internal static class ReplicationFrameFixture {
    internal sealed record Link(string Destination, string DestinationSite, string Source, string SourceSite, string NamingContext,
        TimeSpan? Age, int Failures, int? Result, string? Error, bool QueryFailure, int Pending);

    internal sealed record Partnership(string Destination, string DestinationSite, string Source, string SourceSite, IReadOnlyList<Link> Links) {
        public bool NotReached => Links.All(link => link.QueryFailure);
        public bool Failing => Links.Any(link => link.Failures > 0);
        public HealthState State => Failing ? HealthState.Failing : NotReached ? HealthState.NotEvaluated : HealthState.Healthy;
        public TimeSpan? Oldest => Links.Where(link => link.Age.HasValue).Select(link => link.Age!.Value).DefaultIfEmpty().Max() is TimeSpan max && max > TimeSpan.Zero ? max : null;
    }

    internal sealed class Estate {
        public List<Link> Links { get; } = new List<Link>();
        public List<(string Name, string Site)> Controllers { get; } = new List<(string, string)>();
    }

    private sealed record DcRow(string Name, string Site, Partnership[] Inbound) {
        public bool NotReached => Inbound.Length > 0 && Inbound.All(p => p.NotReached);
        public bool Failing => Inbound.Any(p => p.Failing);
    }

    internal static Estate Create(int count) {
        (string Site, string Prefix, int Count)[] all = {
            ("LON-HQ", "LON", 10), ("AMS-DC", "AMS", 6), ("FRA-DC", "FRA", 6), ("NYC-BR", "NYC", 5), ("TOR-BR", "TOR", 4), ("CHI-BR", "CHI", 4),
            ("SIN-BR", "SIN", 4), ("SYD-BR", "SYD", 3), ("TYO-BR", "TYO", 4), ("DUB-BR", "DUB", 4), ("MEX-BR", "MEX", 5), ("SAO-BR", "SAO", 5),
            ("JNB-BR", "JNB", 5), ("DXB-BR", "DXB", 5), ("MUC-BR", "MUC", 5), ("MAD-BR", "MAD", 5), ("WAW-BR", "WAW", 5), ("BOM-BR", "BOM", 5),
            ("HKG-BR", "HKG", 5), ("SEA-BR", "SEA", 5)
        };
        (string Site, string Prefix, int Count)[] sites = count <= 5
            ? new[] { ("LON-HQ", "LON", 2), ("AMS-DC", "AMS", 2), ("NYC-BR", "NYC", 1) }
            : all.Take(count <= 50 ? 10 : 20).ToArray();
        Estate estate = new Estate();
        foreach (var site in sites) for (int i = 1; i <= site.Count; i++) estate.Controllers.Add((site.Prefix + "-DC" + i.ToString("00"), site.Site));
        Random random = new Random(4242 + count);
        string[] contexts = { "Schema", "Configuration", "corp", "DomainDnsZones", "ForestDnsZones" };
        HashSet<string> unreachable = count <= 5 ? new HashSet<string>() : count <= 50 ? new HashSet<string> { "TYO-DC04" } : new HashSet<string> { "TYO-DC04", "BOM-DC03" };
        var failing = new Dictionary<(string, string), (TimeSpan Age, int Failures, int Result, string Error)>();
        if (count <= 5) {
            failing[("NYC-DC01", "LON-DC01")] = (TimeSpan.FromHours(51), 38, 8524, "DNS lookup failure");
            failing[("LON-DC01", "NYC-DC01")] = (TimeSpan.FromHours(51), 41, 8524, "DNS lookup failure");
        } else {
            failing[("SIN-DC01", "LON-DC01")] = (TimeSpan.FromMinutes(372), 19, 1722, "RPC server unavailable");
            failing[("LON-DC01", "SIN-DC01")] = (TimeSpan.FromMinutes(368), 18, 1722, "RPC server unavailable");
        }
        if (count > 50) {
            failing[("MEX-DC02", "MEX-DC03")] = (TimeSpan.FromHours(26), 31, 5, "Access is denied");
            failing[("JNB-DC01", "LON-DC01")] = (TimeSpan.FromHours(9), 12, 8524, "DNS lookup failure");
        }
        void Add(string destination, string source) {
            string ds = estate.Controllers.First(c => c.Name == destination).Site, ss = estate.Controllers.First(c => c.Name == source).Site;
            failing.TryGetValue((destination, source), out var bad);
            foreach (string nc in contexts) {
                if (unreachable.Contains(destination)) { estate.Links.Add(new Link(destination, ds, source, ss, nc, null, 0, null, "query timed out", true, 0)); continue; }
                double minutes = Math.Exp(random.NextDouble() * Math.Log(ds == ss ? 16 : 70)) + 0.5;
                bool isBad = bad.Failures > 0 && nc != "Schema";
                estate.Links.Add(new Link(destination, ds, source, ss, nc, isBad ? bad.Age : TimeSpan.FromMinutes(minutes), isBad ? bad.Failures : 0,
                    isBad ? bad.Result : 0, isBad ? bad.Error : null, false, isBad ? random.Next(2, 6) : 0));
            }
        }
        foreach (var site in sites) {
            List<string> names = estate.Controllers.Where(c => c.Site == site.Site).Select(c => c.Name).ToList();
            for (int i = 0; i < names.Count && names.Count > 1; i++) {
                Add(names[i], names[(i + 1) % names.Count]);
                if (names.Count > 2) Add(names[i], names[(i + names.Count - 1) % names.Count]);
            }
            if (site.Site != sites[0].Site) { Add(names[0], sites[0].Prefix + "-DC01"); Add(sites[0].Prefix + "-DC01", names[0]); }
        }
        return estate;
    }

    internal static Partnership[] Partnerships(Estate estate) => estate.Links.GroupBy(link => (link.Destination, link.Source))
        .Select(group => new Partnership(group.Key.Destination, group.First().DestinationSite, group.Key.Source, group.First().SourceSite, group.ToList())).ToArray();

    private static string Age(TimeSpan age) => age.TotalDays >= 1 ? (int)age.TotalDays + "d " + age.Hours + "h" : age.TotalHours >= 1 ? (int)age.TotalHours + "h " + age.Minutes + "m" : Math.Max(1, (int)age.TotalMinutes) + "m";
    private static string Largest(TimeSpan age) => age.TotalDays >= 1 ? (int)age.TotalDays + "d" : age.TotalHours >= 1 ? (int)age.TotalHours + "h" : Math.Max(1, (int)age.TotalMinutes) + "m";

    internal static DashboardFrame Build(Estate estate) {
        Partnership[] partnerships = Partnerships(estate);
        string[] sites = estate.Controllers.Select(c => c.Site).Distinct().ToArray();
        int failing = partnerships.Count(p => p.Failing);
        int notReached = estate.Links.Where(l => l.QueryFailure).Select(l => l.Destination).Distinct().Count();
        Partnership? oldest = partnerships.Where(p => p.Oldest.HasValue).OrderByDescending(p => p.Oldest).FirstOrDefault();
        int pending = estate.Links.Sum(l => l.Pending);

        DashboardFrame frame = new DashboardFrame("Replication") { Compact = scale => new TextBlock("compact replication view") };
        frame.Header.Facts.Add("corp.contoso.com");
        frame.Header.Facts.Add(estate.Controllers.Count + " DCs");
        frame.Header.Facts.Add(sites.Length + " sites");
        frame.Header.Facts.Add(partnerships.Length + " partnerships");
        frame.Header.States.Add(FrameState.Of(HealthState.Failing, failing, "failing"));
        frame.Header.States.Add(FrameState.Of(HealthState.NotEvaluated, notReached, "not reached"));

        frame.Meters.Add(Meter.Composition("Partnerships healthy", partnerships.Count(p => p.State == HealthState.Healthy), failing: failing, notEvaluated: partnerships.Count(p => !p.Failing && p.NotReached)));
        frame.Meters.Add(Meter.Ratio("DCs reached", estate.Controllers.Count - notReached, estate.Controllers.Count, HealthState.NotEvaluated));
        frame.Meters.Add(oldest == null ? Meter.Missing("Oldest inbound success") : Meter.Fact("Oldest inbound success", Age(oldest.Oldest!.Value), oldest.Destination + " ← " + oldest.Source, oldest.State));
        frame.Meters.Add(Meter.Count("Pending operations", pending));
        frame.Meters.Add(Meter.Composition("Naming contexts current", estate.Links.Count(l => !l.QueryFailure && l.Failures == 0), failing: estate.Links.Count(l => l.Failures > 0), notEvaluated: estate.Links.Count(l => l.QueryFailure)));

        frame.Attention.Noun = "partnership";
        frame.Attention.OutOf = partnerships.Length;
        foreach (Partnership p in partnerships.Where(p => p.Failing)) {
            Link worst = p.Links.First(l => l.Failures > 0);
            AttentionItem item = new AttentionItem(HealthState.Failing, p.Destination + " ← " + p.Source, worst.Result + " " + worst.Error, -(p.Oldest?.Ticks ?? 0));
            item.Values.Add(Age(p.Oldest!.Value));
            item.Values.Add(p.Links.Max(l => l.Failures) + " fails");
            frame.Attention.Items.Add(item);
        }
        foreach (string dc in estate.Links.Where(l => l.QueryFailure).Select(l => l.Destination).Distinct()) {
            AttentionItem item = new AttentionItem(HealthState.NotEvaluated, dc, "not reached (30 s timeout)");
            item.Values.Add("—"); item.Values.Add(string.Empty);
            frame.Attention.Items.Add(item);
        }

        bool dcMatrix = 16 + 7 * estate.Controllers.Count <= 220;
        FrameSection matrix = new FrameSection("Matrix", context => Matrix(estate, partnerships, sites, context)) { Meta = "inbound" };
        matrix.Facts.Add(dcMatrix ? "worst naming context · age" : "worst state · partnerships in that state");
        if (dcMatrix) matrix.MinWidth = 26 + 8 * estate.Controllers.Count;
        frame.Sections.Add(matrix);
        FrameSection distribution = new FrameSection("Last success", context => Distribution(estate)) { MinWidth = 44, MaxWidth = 52, Meta = "age" };
        distribution.Facts.Add("all naming contexts");
        frame.Sections.Add(distribution);
        FrameSection inbound = new FrameSection("Inbound by DC", context => Inbound(estate, partnerships, context)) { Placement = FramePlacement.Detail };
        inbound.Facts.Add(estate.Controllers.Count + " DCs by site");
        inbound.Facts.Add("log axis");
        inbound.States.Add(FrameState.Of(HealthState.Failing, partnerships.Where(p => p.Failing).Select(p => p.Destination).Distinct().Count(), "failing"));
        inbound.States.Add(FrameState.Of(HealthState.NotEvaluated, notReached, "not reached"));
        frame.Sections.Add(inbound);

        frame.Legend.Add(HealthState.Healthy, "current");
        if (failing > 0) frame.Legend.Add(HealthState.Failing, "failing");
        if (notReached > 0) frame.Legend.Add(HealthState.NotEvaluated, "not reached");
        frame.Legend.Add("·", "no partnership");
        frame.Footer.Counts.Add(estate.Links.Count + " naming-context links");
        return frame;
    }

    private static StatusKind Status(HealthState state) => state == HealthState.Failing ? StatusKind.Fail : state == HealthState.NotEvaluated ? StatusKind.Info : StatusKind.Pass;

    private static IWidget Matrix(Estate estate, Partnership[] partnerships, string[] sites, FrameSectionContext context) {
        if (context.Scale.MatrixCapacity(16, 7, context.Width) >= estate.Controllers.Count) {
            MatrixLabel[] labels = estate.Controllers.Select(c => new MatrixLabel(c.Name, c.Site) { ShortText = c.Name.Split('-')[1] }).ToArray();
            Matrix grid = new Matrix(labels, labels) { InlineAxes = true, ShowLegend = false, MinCellWidth = 6, MaxGridSize = labels.Length,
                RowAxisCaption = "destination", ColumnAxisCaption = "source", CellNote = "cell: age of the oldest naming context" };
            for (int i = 0; i < labels.Length; i++) grid.SetCell(i, i, null, diagonal: true);
            foreach (Partnership p in partnerships) {
                int r = estate.Controllers.FindIndex(c => c.Name == p.Destination), c = estate.Controllers.FindIndex(x => x.Name == p.Source);
                grid.SetCell(r, c, Status(p.State), p.Oldest.HasValue ? Largest(p.Oldest.Value) : "?", p.State == HealthState.NotEvaluated ? "?" : null);
            }
            return grid;
        }
        MatrixLabel[] siteLabels = sites.Select(s => new MatrixLabel(s) { ShortText = s.Substring(0, 3) }).ToArray();
        Matrix rollup = new Matrix(siteLabels, siteLabels) { InlineAxes = true, ShowLegend = false, MinCellWidth = 3, MaxGridSize = siteLabels.Length,
            RowAxisCaption = "destination", ColumnAxisCaption = "source", CellNote = "cell: worst state · partnerships in that state" };
        foreach (var pair in partnerships.GroupBy(p => (p.DestinationSite, p.SourceSite))) {
            Partnership worst = pair.OrderByDescending(p => p.Failing ? 2 : p.NotReached ? 1 : 0).First();
            rollup.SetCell(Array.IndexOf(sites, pair.Key.DestinationSite), Array.IndexOf(sites, pair.Key.SourceSite), Status(worst.State),
                worst.State == HealthState.Healthy ? string.Empty : pair.Count(p => p.State == worst.State).ToString(), worst.State == HealthState.NotEvaluated ? "?" : null);
        }
        return rollup;
    }

    private static IWidget Distribution(Estate estate) {
        TimeSpan[] ages = estate.Links.Where(l => l.Age.HasValue).Select(l => l.Age!.Value).ToArray();
        Distribution distribution = new Distribution()
            .Add("<15m", ages.Count(a => a < TimeSpan.FromMinutes(15)))
            .Add("15m to 1h", ages.Count(a => a >= TimeSpan.FromMinutes(15) && a < TimeSpan.FromHours(1)))
            .Add("1h to 1d", ages.Count(a => a >= TimeSpan.FromHours(1) && a < TimeSpan.FromDays(1)))
            .Add(">1d", ages.Count(a => a >= TimeSpan.FromDays(1)));
        int unread = estate.Links.Count(l => !l.Age.HasValue);
        if (unread > 0) distribution.AddNote("unread", unread, HealthState.NotEvaluated, "DC not reached");
        return distribution;
    }

    private static IWidget Inbound(Estate estate, Partnership[] partnerships, FrameSectionContext context) {
        LogScale axis = new LogScale(1, 4 * 24 * 60);
        DcRow[] rows = estate.Controllers.Select(c => new DcRow(c.Name, c.Site, partnerships.Where(p => p.Destination == c.Name).ToArray())).ToArray();
        GroupedRows<DcRow> table = new GroupedRows<DcRow>(rows, row => row.Site, context.Scale) {
            Noun = "DC", HealthyWord = "current", IsHealthy = row => !row.Failing && !row.NotReached,
            Heading = (site, group) => {
                GroupHeading heading = new GroupHeading();
                heading.Facts.Add(group.Count + (group.Count == 1 ? " DC" : " DCs"));
                heading.States.Add(FrameState.Of(HealthState.Failing, group.Count(r => r.Failing), "failing"));
                heading.States.Add(FrameState.Of(HealthState.NotEvaluated, group.Count(r => r.NotReached), "not reached"));
                if (group.All(r => !r.Failing && !r.NotReached)) heading.States.Add(FrameState.Healthy("all current"));
                return heading;
            }
        };
        table.Columns.Add(new GroupedColumn<DcRow>("DC", (row, _) => Markup.Escape(row.Name)));
        table.Columns.Add(new GroupedColumn<DcRow>("In", (row, _) => row.Inbound.Length.ToString()) { AlignRight = true });
        table.Columns.Add(new GroupedColumn<DcRow>("Fail", (row, _) => row.Inbound.Count(p => p.Failing).ToString()) { AlignRight = true });
        table.Columns.Add(new GroupedColumn<DcRow>("Last success, all naming contexts", (row, render) => {
            if (row.NotReached) return FrameBars.Note("not reached", HealthState.NotEvaluated, 40, render);
            TimeSpan[] ages = row.Inbound.SelectMany(p => p.Links).Where(l => l.Age.HasValue).Select(l => l.Age!.Value).ToArray();
            return ages.Length == 0 ? FrameBars.Note("no inbound partners", HealthState.Unknown, 40, render)
                : FrameBars.Range(axis.Fraction(ages.Min().TotalMinutes), axis.Fraction(ages.Max().TotalMinutes), 40, row.Failing ? HealthState.Failing : (HealthState?)null, null, render);
        }) { Width = 40, Axis = axis.Labels(40, new Dictionary<double, string> { [1] = "1m", [10] = "10m", [60] = "1h", [1440] = "1d", [5760] = "4d" }) });
        table.Columns.Add(new GroupedColumn<DcRow>("Worst", (row, _) => row.NotReached ? "?" : row.Inbound.Select(p => p.Oldest).Max() is TimeSpan worst ? Age(worst) : "unknown") { AlignRight = true });
        return table;
    }
}
