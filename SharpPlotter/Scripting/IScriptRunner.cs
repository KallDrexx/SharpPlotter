namespace SharpPlotter.Scripting
{
    public interface IScriptRunner
    {
        GraphedItems RunScript(string scriptContent);
    }
}