using System.IO;

namespace PluginLib
{

    public interface IRomcenterPlugin
    {
        PluginResult? GetSignature(Stream romStream, string? zipCrc);

        string GetAuthor();

        string GetDescription();

        string GetDllInterfaceVersion();

        string GetDllType();

        string GetEmail();

        string GetPlugInName();

        string GetVersion();

        string GetWebPage();
    }
}