using System.IO;

namespace snes
{
    public partial class RcPlugin
    {
        private const string DllType = "romcenter signature calculator"; //Identification string, do not change
        private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

        private const string PlugInName = "Sega Genesis Megadrive crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support
        private const string Description = "Sega Genesis Megadrive crc calculator. Support .32x, .bin, .smd and .md format.";

        private const long RomMinSizeInBytes = 32 * 1024; //min size of the core rom, 32KB
        private const long RomMaxSizeInBytes = 4 * 1024 * 1024; //max size of the core rom, 4MB

        private string GetFileExtension(FormatEnum romFormatType)
        {
            return romFormatType switch
            {
                FormatEnum.Sms => ".sms",
                _ => ""
            };
        }

        private string GetFormatText(FormatEnum romFormat)
        {
            return romFormat switch
            {
                FormatEnum.Sms => ".sms",
                _ => ""
            };
        }

        /// <summary>
        /// Return rom format
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private RomFormat GetHeaderFormat(Stream stream)
        {
        }
    }
}
