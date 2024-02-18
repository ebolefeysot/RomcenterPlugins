using System.IO;

namespace PluginCommon;

public interface IPluginMaker
{
    /// <summary>
    /// Return rom format
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    RomFormat GetHeaderFormat(Stream stream);
}