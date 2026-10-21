// A stable wide object whose nested data does not depend on process state, permissions or environment variables.
public sealed class ConsoleMonsterWideTreeFixture {
    public ConsoleMonsterTreeChildFixture P00 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P01 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P02 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P03 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P04 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P05 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P06 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P07 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P08 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P09 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P10 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P11 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P12 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P13 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P14 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P15 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P16 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P17 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P18 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P19 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P20 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P21 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P22 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P23 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P24 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P25 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P26 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P27 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P28 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P29 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P30 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P31 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P32 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P33 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P34 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P35 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P36 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P37 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P38 { get { return Values(); } }
    public ConsoleMonsterTreeChildFixture P39 { get { return Values(); } }

    private static ConsoleMonsterTreeChildFixture Values() { return new ConsoleMonsterTreeChildFixture(); }
}

public sealed class ConsoleMonsterTreeChildFixture {
    public string Name { get { return "child"; } }
    public ConsoleMonsterTreeDetailFixture Details { get { return new ConsoleMonsterTreeDetailFixture(); } }
}

public sealed class ConsoleMonsterTreeDetailFixture {
    public string A { get { return "known-value"; } }
    public int B { get { return 2; } }
    public int C { get { return 3; } }
    public int D { get { return 4; } }
    public int E { get { return 5; } }
    public int F { get { return 6; } }
    public int G { get { return 7; } }
    public int H { get { return 8; } }
    public int I { get { return 9; } }
    public int J { get { return 10; } }
    public int K { get { return 11; } }
    public int L { get { return 12; } }
}
