using System.IO;

namespace PluginBase;

public interface IRomcenterPluginNew
{
    PluginResult? GetSignature2(Stream romStream, string zipCrc);

    string GetAuthor();

    string GetDescription();

    string GetDllInterfaceVersion();

    string GetDllType();

    string GetEmail();

    string GetPlugInName();

    string GetVersion();

    string GetWebPage();
}