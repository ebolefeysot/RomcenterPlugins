namespace PluginTest.TestBase
{
    public interface IRomcenterPlugin
    {
        string? GetSignature(string filename, string zipcrc, out string format, out long size, out string comment, out string errorMessage);

        string? GetAuthor();

        string GetDescription();

        string GetDllInterfaceVersion();

        string GetDllType();

        string GetEmail();

        string GetPlugInName();

        string GetVersion();

        string GetWebPage();
    }
}