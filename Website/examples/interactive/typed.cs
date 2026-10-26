using ConsoleMonster;
using ConsoleMonster.Interactive;

bool remove = new TypedConfirmPrompt("Remove FS01 from the inventory?", "FS01") {
    Warning = "Its history and reports are deleted too. This cannot be undone."
}.Show();
if (remove) {
    Terminal.WriteMarkupLine("[warn]FS01 removed[/]");
}
