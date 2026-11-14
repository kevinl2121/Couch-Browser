namespace CouchBrowser;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        bool startPrivate = !args.Any(a => a.Equals("--normal", StringComparison.OrdinalIgnoreCase));
        string startUrl = args.FirstOrDefault(a => !a.StartsWith("--")) ?? BrowserForm.Home;
        Application.Run(new BrowserForm(startPrivate, startUrl));
    }
}