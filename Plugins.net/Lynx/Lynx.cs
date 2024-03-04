//Plug in for RomCenter
//
// (C) Copyright 2009-2024 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using System.IO;

namespace Lynx;

public partial class RcPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

    private const string PlugInName = "Lynx crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Lynx crc calculator. Skip the lynx file header to calculate the crc32. Support .lnx and .lyx format.";

    private const long RomMinSizeInBytes = 128 * 1024; //min size of the core rom
    private const long RomMaxSizeInBytes = 512 * 1024; //max size of the core rom

    private string GetFileExtension(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Lnx => ".lnx",
            FormatEnum.Lyx => ".lyx",
            _ => ""
        };
    }

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Lnx => "Lnx",
            FormatEnum.Lyx => "Lyx",
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
        //format available: LNX; header $40 bytes
        //00000000  4C 59 4E 58 00 04 00 00 01 00 68 64 72 69 76 69  LYNX......hdrivi
        //00000010  6E 67 2E 6C 79 78 00 00 00 00 00 00 00 00 00 00  ng.lnx..........
        //00000020  00 00 00 00 00 00 00 00 00 00 41 74 61 72 69 00  ..........Atari.
        //00000030  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00  ................
        //format available: LYX raw binary; no header

        const string headerText = "LYNX";
        const int headerSizeInBytes = 0x40;

        var format = new RomFormat();
        var br = new BinaryReader(stream);

        format.HeaderVersion = br.ReadByte();
        format.Format = FormatEnum.Lyx;

        var magicString = new string(br.ReadChars(4)).ToUpper();
        if (magicString == headerText)
        {
            format.Format = FormatEnum.Lnx;
            format.HeaderSizeInBytes = headerSizeInBytes;
            format.RomSizeInBytes = (int)stream.Length - headerSizeInBytes;
            format.HeaderRomSizeInBytes = 0;
        }
        else
        {
            // No header
            format.HeaderSizeInBytes = 0;
            format.RomSizeInBytes = (int)stream.Length;

            //check size multiple of 1KB
            if (format.RomSizeInBytes % 1024 != 0)
            {
                format.Comment = "Not a atari Lynx rom (invalid size)";
                format.Format = FormatEnum.None;
                format.FormatTxt = "";
                return format;
            }
        }

        return format;
    }
}