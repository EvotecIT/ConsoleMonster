using ConsoleMonster;
using ConsoleMonster.Widgets;

ExceptionInfo error = new("ItemNotFoundException",
    "Cannot load the inventory: the source file is missing.")
{
    CommandName = "Get-Item",
    SourceLine = "Get-Item -LiteralPath './inventory.json'",
    ScriptPath = "deploy.ps1",
    LineNumber = 12,
    ColumnNumber = 1,
    ErrorLength = 38,
    Category = "ObjectNotFound",
    Target = "./inventory.json",
    ErrorId = "PathNotFound",
    Inner = new ExceptionInfo("IOException", "The inventory source is unavailable.")
    {
        Inner = new ExceptionInfo("Exception", "The source was removed.")
    }
};
Terminal.Write(new ExceptionView(error)
{
    Compact = true,
    ShowStack = false,
    DetailsHint = "Get-Error for details"
});
