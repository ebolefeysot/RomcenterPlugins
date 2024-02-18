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

namespace gbx;

public partial class RcPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

    private const string PlugInName = "Gameboy color crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Gameboy crc calculator. Skip the Gameboy file header to calculate the crc32. Support .gb, .gbc and .sgb format.";

    private const long RomMinSizeInBytes = 32 * 1024; //min size of the core rom, 32KB
    private const long RomMaxSizeInBytes = 4 * 1024 * 1024; //max size of the core rom, 4MB

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Gb => "Gameboy",
            FormatEnum.Gbc => "Gameboy color",
            FormatEnum.Sgb => "Super Gameboy",
            _ => ""
        };
    }

    private string GetFileExtension(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.Gb => ".gb",
            FormatEnum.Gbc => ".gbc",
            FormatEnum.Sgb => ".sgb",
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
        //https://gbdev.io/pandocs/The_Cartridge_Header.html
        //Each cartridge contains a header, located at the address range $0100—$014F. It is part of the rom.
        //No additional header to skip

        //format available: .gb, .gbc, .sgb
        //0104 - 0133 : Nintendo Logo: CE ED 66 66 CC 0D 00 0B 03 73 00 83 00 0C 00 0D...
        //0143 - CGB Flag: 80h/C0h = .gbc
        //0146 - SGB Flag: 03h: .sgb
        //0148 — ROM size 00=32KB, 01=64KB...08=8MB

        const int headerStart = 0x104;
        const int cgbFlag = 0x143;
        const int sgbFlag = 0x146;
        const int romSizeOffset = 0x148;
        const int headerSize = 0x0;
        const string logo = "CEED6666";

        var format = new RomFormat();
        var totalStreamSize = stream.Length;
        stream.Seek(headerStart, SeekOrigin.Begin);

        var br = new BinaryReader(stream);

        //look for nintendo header
        var result = Helper.GetHexString(br, headerStart, 4);
        if (result != logo)
        {
            format.Comment = "Not a gameboy rom (missing header)";
            format.RomSizeInBytes = (int)stream.Length;
            return format;
        }

        format.RomSizeInBytes = (int)(totalStreamSize - headerSize);
        format.HeaderSizeInBytes = headerSize;
        format.Format = FormatEnum.Gb;
        byte cgb = Helper.GetByte(br, cgbFlag);
        if (cgb is 0x80 or 0xC0)
        {
            //Color Gameboy or super gameboy
            format.Format = FormatEnum.Gbc;
            byte sgb = Helper.GetByte(br, sgbFlag);
            if (sgb == 0x03)
            {
                //Super Gameboy
                format.Format = FormatEnum.Sgb;
            }
            else
            {
                format.Format = FormatEnum.Gbc;
            }
        }

        short romSize = Helper.GetByte(br, romSizeOffset);
        format.HeaderRomSizeInBytes = 2 << (romSize + 14);

        if (format.HeaderRomSizeInBytes != format.RomSizeInBytes)
        {
            format.Comment = $"Rom size stored in header ({format.HeaderRomSizeInBytes} bytes) doesn't match real rom size ({format.RomSizeInBytes} bytes)";
        }

        return format;
    }
}