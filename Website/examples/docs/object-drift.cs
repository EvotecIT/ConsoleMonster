using System.Text.RegularExpressions;
using ConsoleMonster;
using ConsoleMonster.Changes;

// Four domain controllers that should be configured alike.
object[] controllers = {
    new {
        Name = "dc01", Site = "HQ", LdapPort = 389, GlobalCatalog = true, DnsEnabled = true,
        Spn = "ldap/dc01.ad.evotec.xyz",
        Security = new { Signing = "Required", ChannelBinding = "Always" }
    },
    new {
        Name = "dc02", Site = "HQ", LdapPort = 636, GlobalCatalog = true, DnsEnabled = true,
        Spn = "ldap/dc02.ad.evotec.xyz",
        Security = new { Signing = "Required", ChannelBinding = "Always" }
    },
    new {
        Name = "dc03", Site = "Branch", LdapPort = 389, GlobalCatalog = false, DnsEnabled = true,
        Spn = "ldap/dc03.ad.evotec.xyz",
        Security = new { Signing = "Negotiate", ChannelBinding = "Always" }
    },
    new {
        Name = "dc04", Site = "Branch", LdapPort = 389, GlobalCatalog = true,
        Spn = "ldap/dc04.ad.evotec.xyz",
        Security = new { Signing = "Required", ChannelBinding = "Never" }
    }
};

// dc01 is the reference; each SPN is compared without its host name, so only real drift remains.
var options = new DiffOptions();
options.Ignore.Add("Name");
options.Normalize["Spn"] = value => Regex.Replace(value?.ToString() ?? "", "/[^.]+", "/HOST");

BaselineComparison comparison = ObjectDiff.CompareBaseline(
    controllers, options, baselineName: "dc01");
Terminal.Write(new BaselineView(comparison) { Title = "Domain controller drift" });
