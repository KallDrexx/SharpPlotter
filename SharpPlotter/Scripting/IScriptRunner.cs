namespace SharpPlotter.Scripting
{
    public interface IScriptRunner
    {
        string NewFileHeaderContent { get; }
        GraphedItems RunScript(string scriptContent);
    }
}