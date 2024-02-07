//Plug in for RomCenter
//
// (C) Copyright 2009-2024 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using PluginBase;

using System;
using System.IO;

namespace gbx;

public class RcPlugin : IRomcenterPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.0"; //version of romcenter plugin internal interface

    private const string PlugInName = "Gameboy color crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Gameboy crc calculator. Skip the Gameboy file header to calculate the crc32. Support .gb, .gbc and .sgb format.";

    public string GetSignature(string filename, string zipcrc, out string format, out long size, out string comment,
        out string errorMessage)
    {
        var romFormat = new RomFormat();

        format = "";
        size = 0;
        comment = "";
        errorMessage = "";

        try
        {
            var fs = new FileStream(filename, FileMode.Open);
            try
            {
                GetHeaderFormat(fs, romFormat);

                format = romFormat.Type == FormatEnum.None ? "" : "." + romFormat.Type.ToString().ToLowerInvariant();
                size = romFormat.RomSizeInBytes;
                comment = romFormat.Comment;
                errorMessage = romFormat.Error;

                var hash = GetCrc32(fs, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                return hash;
            }
            finally
            {
                fs.Close();
            }
        }
        catch (Exception e)
        {
            if (File.Exists(filename))
            {
                size = (int)new FileInfo(filename).Length;
            }

            errorMessage = e.Message;
        }

        return "";
    }

    /// <summary>
    /// Return rom format
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    private static void GetHeaderFormat(Stream stream, RomFormat format)
    {
        //https://gbdev.io/pandocs/The_Cartridge_Header.html
        //Each cartridge contains a header, located at the address range $0100—$014F

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
        const long romMinSizeInBytes = 32768; //min size of the core rom

        var totalStreamSize = stream.Length;
        stream.Seek(headerStart, SeekOrigin.Begin);

        if (stream.Length < romMinSizeInBytes)
        {
            //too small, not a gb game
            format.Comment = "Too small for a game";
            format.RomSizeInBytes = (int)stream.Length;
            return;
        }

        var br = new BinaryReader(stream);

        //look for nintendo header
        var result = Helper.GetHexString(br, headerStart, 4);
        if (result != logo)
        {
            format.RomSizeInBytes = (int)stream.Length;
            format.Comment = "Not an gameboy rom (missing header)";
            format.RomSizeInBytes = (int)stream.Length;
            return;
        }

        format.RomSizeInBytes = (int)(totalStreamSize - headerSize);
        format.Type = FormatEnum.Gb;
        byte cgb = Helper.GetByte(br, cgbFlag);
        if (cgb is 0x80 or 0xC0)
        {
            //Color Gameboy or super gameboy
            format.Type = FormatEnum.Gbc;
            byte sgb = Helper.GetByte(br, sgbFlag);
            if (sgb == 0x03)
            {
                //Super Gameboy
                format.Type = FormatEnum.Sgb;
            }
            else
            {
                format.Type = FormatEnum.Gbc;
            }
        }

        short romSize = Helper.GetByte(br, romSizeOffset);
        format.HeaderRomSizeInBytes = 2 << (romSize + 14);

        if (format.HeaderRomSizeInBytes != format.RomSizeInBytes)
        {
            format.Comment = $"Rom size stored in header ({format.HeaderRomSizeInBytes} bytes) doesn't match real rom size ({format.RomSizeInBytes} bytes)";
        }
    }

    /// <summary>
    /// return crc32
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="offset">Start offset</param>
    /// <param name="length">Bytes count</param>
    /// <returns></returns>
    private string GetCrc32(Stream fileStream, int offset, int length)
    {
        var fileBuf = new byte[length];

        //Go to rom start
        fileStream.Seek(offset, SeekOrigin.Begin);
        fileStream.Read(fileBuf, 0, length);

        //calculate crc32
        var hashAlgorithm = new Crc32HashAlgorithm();

        // get array of 4 8 bits crc values
        byte[] crc = hashAlgorithm.ComputeHash(fileBuf);

        // convert to a 32bits hex value
        return Crc32HashAlgorithm.ToHex(crc);
    }

    public string GetAuthor()
    {
        return Author;
    }

    public string GetDescription()
    {
        return Description;
    }

    public string GetDllInterfaceVersion()
    {
        return InterfaceVersion;
    }

    public string GetDllType()
    {
        return DllType;
    }

    public string GetEmail()
    {
        return Email;
    }

    public string GetPlugInName()
    {
        return PlugInName;
    }

    public string GetVersion()
    {
        return Version;
    }

    public string GetWebPage()
    {
        return WebPage;
    }
}