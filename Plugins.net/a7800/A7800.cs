//Plug in for RomCenter
//
// (C) Copyright 2009-2024 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using System;
using System.IO;
using System.Linq;

namespace a7800;

public partial class RcPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

    private const string PlugInName = "Atari 7800 crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Atari 7800 crc calculator. Skip the Atari 7800 file header to calculate the crc32. Support a78, bin format.";

    private const long RomMinSizeInBytes = 1024; //min size of the core rom, 1KB
    private const long RomMaxSizeInBytes = 512 * 1024; //max size of the core rom, 512KB

    private string GetFileExtension(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.A78 => ".a78",
            FormatEnum.Bin => ".bin",
            _ => ""
        };
    }

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.A78 => "A78",
            FormatEnum.Bin => "RAW",
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
        //format available:
        // type 1: normal header
        // https://7800.8bitdev.org/index.php/A78_Header_Specification
        // 00000000  01 41 54 41 52 49 37 38 30 30 37 00 00 00 00 00  .ATARI7800......
        // 00000010  6F 43 6F 6C 6F 72 20 47 72 69 64 00 00 00 00 00  .Color Grid.....
        // 00000020  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00  ................
        // 00000030  00 00 00 10 00 00 00 01 01 00 00 00 00 00 00 00  ................ 31...34: rom size
        // 00000040  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00  ................
        // 00000050  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00  ................
        // 00000060  00 00 00 00 41 43 54 55 41 4C 20 43 41 52 54 20  ....ACTUAL CART
        // 00000070  44 41 54 41 20 53 54 41 52 54 53 20 48 45 52 45  DATA STARTS HERE
        // 00000080  rom start here

        const string headerText = "ATARI7800";
        const int headerSizeInBytes = 0x80;

        var format = new RomFormat();
        var br = new BinaryReader(stream);

        format.HeaderVersion = br.ReadByte();
        format.Format = FormatEnum.Bin;

        var magicString = new string(br.ReadChars(9)).ToUpper();
        if (magicString == headerText)
        {
            format.Format = FormatEnum.A78;
            format.HeaderSizeInBytes = headerSizeInBytes;
            format.RomSizeInBytes = (int)stream.Length - headerSizeInBytes;

            //Get rom size stored in header
            br.BaseStream.Seek(0x31, SeekOrigin.Begin);
            format.HeaderRomSizeInBytes = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

            //check header size match rom size
            if (format.RomSizeInBytes != format.HeaderRomSizeInBytes)
            {
                format.Comment = $"Rom size stored in header ({format.HeaderRomSizeInBytes} bytes) doesn't match real rom size ({format.RomSizeInBytes} bytes)";
            }
        }
        else
        {
            // No header
            format.HeaderSizeInBytes = 0;
            format.RomSizeInBytes = (int)stream.Length;

            //check size multiple of 1KB
            if (format.RomSizeInBytes % 1024 != 0)
            {
                format.Comment = "Not an atari 7800 rom (invalid size)";
                format.Format = FormatEnum.None;
                format.FormatTxt = "";
                return format;
            }
        }

        return format;
    }
}