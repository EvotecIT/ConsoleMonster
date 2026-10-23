using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using SiteFrames.Capture;
using SiteFrames.Pages;

namespace SiteFrames {
    /// <summary>
    /// The consolemonster.dev site tool.
    /// <list type="bullet">
    /// <item><c>render [Website]</c>: renders every frame, replay, thumbnail and page data file from the ConsoleMonster sources
    /// and the committed captures. Deterministic: rerun it after a renderer change and commit what changed.</item>
    /// <item><c>capture [Website]</c>: runs the PowerShell examples on Windows PowerShell 5.1 and PowerShell 7 and records the
    /// live showcases, each inside a Windows pseudo console, and saves the VT streams under <c>Website/captures/</c>.</item>
                case "app-review":
                    if (only == null) {
                        Console.Error.WriteLine("Usage: SiteFrames app-review [WebsiteRoot] [ScratchOutputDirectory]");
                        return 2;
                    }
                    return AppPage.Review(new Site(website), only);
                default:
                    Console.Error.WriteLine("Usage: SiteFrames render|capture|readme|review-chart|review-text|review-object|app-review [WebsiteRoot] [area|output]");
