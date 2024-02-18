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

namespace SegaGenesis;

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
            FormatEnum._32x => ".32x",
            FormatEnum.Smd => ".smd",
            FormatEnum.Md => ".gen",
            FormatEnum.Gen => ".gen",
            _ => ""
        };
    }

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Md => "Mega Drive",
            FormatEnum.Gen => "Genesis",
            FormatEnum.Smd => "Super Magic Drive",
            FormatEnum._32x => "32X",
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
            RomSizeInBytes = (int)stream.Length
        };

        var br = new BinaryReader(stream);
        if (Helper.GetHexString(br, 0xd4, 4) == "FF00FFFF")
        {
            format.Format = FormatEnum.Md; //interleaved without header
        }
        else if (Helper.GetHexString(br, 0x105, 3) == "333258") //32X
        {
            //sega 32x format bin
            format.Format = FormatEnum.Gen;
            format.HeaderSizeInBytes = 0;
        }
        else if (Helper.GetHexString(br, 0x100, 4) == "53454741") //SEGA
        {
            format.Format = FormatEnum.Gen;
        }
        else if (Helper.GetHexString(br, 0x1a8, 4) == "00FF0000")
        {
            format.Format = FormatEnum.Gen;
        }
        else if (Helper.GetHexString(br, 0x490, 4) == "53637274")
        {
            //sega 32x format .32x
            format.Format = FormatEnum._32x;
            format.HeaderSizeInBytes = 512;
        }
        else if (Helper.GetHexString(br, 0x520, 4) == "20536563")
        {
            format.Format = FormatEnum.Gen;
        }
        else if (Helper.GetHexString(br, 0x2d4, 4) == "FF00FFFF") //(32x also have these values)
        {
            format.Format = FormatEnum.Smd;
            format.HeaderSizeInBytes = 512;
        }

        switch (format.Format)
        {
            case FormatEnum._32x:
            case FormatEnum.Smd:
                format.HeaderRomSizeInBytes = Helper.GetLong(br, 0x2d1) + 1;
                format.RomSizeInBytes -= format.HeaderSizeInBytes;
                //interleaved/swapped 8192 bytes (8KB)
                format.InterleavedBlockSize = 8192;
                format.Swapped = true;
                break;
            case FormatEnum.Md:
                //interleaved/swapped rom size/2 (2 blocks)
                format.InterleavedBlockSize = format.RomSizeInBytes / 2;
                format.Swapped = true;
                break;
        }

        //short romSize = Helper.GetByte(br, romSizeOffset);

        //if (format.Format is FormatEnum.Smd or FormatEnum._32x && format.HeaderRomSizeInBytes != format.RomSizeInBytes)
        //{
        //    format.Comment = $"Rom size stored in header ({format.HeaderRomSizeInBytes} bytes) doesn't match real rom size ({format.RomSizeInBytes} bytes)";
        //}
        return format;
    }
}