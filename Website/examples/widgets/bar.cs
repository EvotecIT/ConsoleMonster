using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new BarChart()
    .WithRole(ThemeToken.Fail)
    .Add("Domain controllers", 14)
    .Add("Group Policy", 9)
    .Add("Kerberos", 6)
    .Add("DNS", 4)
    .Add("PKI", 2));