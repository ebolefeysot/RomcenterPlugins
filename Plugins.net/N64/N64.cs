//Plug in for RomCenter
//
// (C) Copyright 2009-2024 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using PluginLib;
using System.IO;

namespace N64;

public partial class RcPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

    private const string PlugInName = "Nintendo 64 crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Nintendo 64 crc calculator. Support n64, z64 and v64.";

    private const long RomMinSizeInBytes = 4 * 1024 * 1024; //min size of the core rom, 4MB
    private const long RomMaxSizeInBytes = 64 * 1024 * 1024; //max size of the core rom, 64MB

    private string GetFileExtension(FormatEnum romFormatType)
    {
        return romFormatType switch
        {
            FormatEnum.N64 => ".n64",
            FormatEnum.V64 => ".v64",
            FormatEnum.Z64 => ".z64",
            _ => ""
        };
    }

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.N64 => "",
            FormatEnum.V64 => "Doctor V64",
            FormatEnum.Z64 => "Mr. Backup Z64",
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
        //Each cartridge contains a 512 byte header. It is part of the rom.
        const int headerStart = 0x0;
        stream.Seek(headerStart, SeekOrigin.Begin);

        var format = new RomFormat
        {
            Format = FormatEnum.None,
            RomSizeInBytes = (int)stream.Length,
            HeaderSizeInBytes = 0x0
        };

        var br = new BinaryReader(stream);
        var piBytes = Helper.GetHexString(br, headerStart, 4);
        if (piBytes == "80371240")
        {
            format.Format = FormatEnum.Z64; //regular byte order
        }
        else if (piBytes == "37804012")
        {
            format.Format = FormatEnum.V64;
        }
        else if (piBytes == "40123780")
        {
            format.Format = FormatEnum.N64;
        }
        else
        {
            format.Format = FormatEnum.None;
        }

        switch (format.Format)
        {
            case FormatEnum.V64:
                format.HeaderRomSizeInBytes = Helper.GetLong(br, 0x2d1) + 1;
                format.RomSizeInBytes -= format.HeaderSizeInBytes;
                format.ByteOrder = Helper.ByteOrderEnum.ByteSwapped;
                break;
            case FormatEnum.Z64:
                format.ByteOrder = Helper.ByteOrderEnum.Regular;
                break;
            case FormatEnum.N64:
                format.ByteOrder = Helper.ByteOrderEnum.LittleEndian;
                break;
            case FormatEnum.None:
                format.ByteOrder = Helper.ByteOrderEnum.Regular;
                format.Comment = "Not a nintendo 64 rom (no header)";
                break;
        }
        return format;
    }
}