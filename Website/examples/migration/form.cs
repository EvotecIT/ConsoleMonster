using ConsoleMonster.Interactive;

Form form = new Form("Service inventory");
TextField name = form.Text("Name", "Service name");
name.Default = "WinRM";
name.Required = true;
FormResult result = form.Show();
// Collect answers only. The caller decides whether to execute a command.
