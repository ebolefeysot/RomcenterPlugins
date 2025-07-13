//Plug in for RomCenter
//
// (C) Copyright 2009-2024 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using System.IO;

namespace fds;

public partial class RcPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

    private const string PlugInName = "Famicom Disk System (FDS) crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Fds crc calculator. Skip the Fds file header to calculate the crc32. " +
                                       "Support .fds format with and without header.";

    private const long RomMinSizeInBytes = 64 * 1024; //min size of the core rom
    private const long RomMaxSizeInBytes = 256 * 1024; //max size of the core rom

    /// <summary>
    /// Minimum size of BIOS files in bytes.
    /// </summary>
    private const long BiosMinSizeInBytes = 8 * 1024;

    private string GetFileExtension(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Fds => ".fds",
            FormatEnum.FdsRaw => ".fds",
            FormatEnum.Bios => ".rom",
            _ => ""
        };
    }

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Fds => "Fds with header",
            FormatEnum.FdsRaw => "Fds raw data",
            FormatEnum.Bios => "Fds Bios",
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
        //format available: FDS with 16b header
        //00000000  46 44 53 1A 01 00 00 00 00 00 00 00 00 00 00 00  FDS.............
        //00000010  01 2A 4E 49 4E 54 45 4E 44 4F 2D 48 56 43 2A A4  .*NINTENDO-HVC*¤

        //format available: FDS raw binary; no header
        //00000000  01 2A 4E 49 4E 54 45 4E 44 4F 2D 48 56 43 2A A4  .*NINTENDO-HVC*¤

        const string headerText = "*NINTENDO-HVC*";
        const int headerSizeInBytes = 16;
        var format = new RomFormat();

        //check size multiple of 1KB
        if (format.RomSizeInBytes % 1024 != 0)
        {
            format.Comment = "Not a fds rom (invalid size)";
            format.Format = FormatEnum.None;
            format.FormatTxt = "";
            return format;
        }
        var br = new BinaryReader(stream);
        stream.Seek(1, SeekOrigin.Begin);
        var magicString = br.ReadBytes(headerText.Length);
        if (System.Text.Encoding.UTF8.GetString(magicString) == headerText)
        {
            // Raw FDS
            format.Format = FormatEnum.FdsRaw;
            format.HeaderSizeInBytes = 0;
            format.RomSizeInBytes = (int)stream.Length;
        }
        else
        {
            stream.Seek(1 + headerSizeInBytes, SeekOrigin.Begin);
            magicString = br.ReadBytes(headerText.Length);

            if (System.Text.Encoding.UTF8.GetString(magicString) == headerText)
            {
                // FDS with header
                format.Format = FormatEnum.Fds;
                format.HeaderSizeInBytes = headerSizeInBytes;
                format.RomSizeInBytes = (int)stream.Length - headerSizeInBytes;
            }
        }


        return format;
    }
}