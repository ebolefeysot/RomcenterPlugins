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

namespace SegaGenesis;

public class RcPlugin : IRomcenterPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.0"; //version of romcenter plugin internal interface

    private const string PlugInName = "Sega Genesis Megadrive crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Sega Genesis Megadrive crc calculator. Support .32x, .bin, .smd and .md format.";

    public string GetSignature(string filename, string zipcrc, out string format, out long size, out string comment,
        out string errorMessage)
    {
        var romFormat = new RomFormat();

        format = "";
        size = 0;
        comment = "";
        errorMessage = "";
        zipcrc = zipcrc?.ToLowerInvariant() ?? "";

        try
        {
            var fs = new FileStream(filename, FileMode.Open);
            try
            {
                GetHeaderFormat(fs, romFormat);

                string hash;
                MemoryStream stream2;
                MemoryStream ms;
                switch (romFormat.Type)
                {
                    case FormatEnum._32x:
                    case FormatEnum.smd:
                        //interleaved/swapped 8192 bytes (8KB)
                        ms = new MemoryStream();
                        fs.Position = 0;
                        fs.CopyTo(ms);
                        stream2 = Helper.DeInterleave(ms, 8192, romFormat.HeaderSizeInBytes, true);
                        hash = GetCrc32(stream2, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                        break;
                    case FormatEnum.md:
                        //interleaved/swapped rom size/2 (2 blocks)
                        ms = new MemoryStream();
                        fs.Position = 0;
                        fs.CopyTo(ms);
                        stream2 = Helper.DeInterleave(ms, romFormat.RomSizeInBytes / 2, romFormat.HeaderSizeInBytes, true);
                        hash = GetCrc32(stream2, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                        break;
                    default:
                        hash = zipcrc;
                        break;
                }

                format = GetFileExtension(romFormat.Type);
                size = romFormat.RomSizeInBytes;
                comment = romFormat.Comment;
                errorMessage = romFormat.Error;

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

    private string GetFileExtension(FormatEnum romFormatType)
    {
        switch (romFormatType)
        {
            case FormatEnum._32x:
                return ".32x";
            case FormatEnum.md:
                return ".gen";
            case FormatEnum.None:
                return "";
            default:
                return "." + romFormatType.ToString().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Return rom format
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    private static void GetHeaderFormat(Stream stream, RomFormat format)
    {
        //Each cartridge contains a 512 byte header. It is part of the rom.
        const int headerStart = 0x0;
        const int romSizeOffset = 0x148;
        const long romMinSizeInBytes = 2048; //min size of the core rom

        format.Type = FormatEnum.None;
        stream.Seek(headerStart, SeekOrigin.Begin);
        format.RomSizeInBytes = (int)stream.Length;

        if (stream.Length < romMinSizeInBytes)
        {
            //too small, not a genesis game
            format.Comment = "Too small for a game";
            return;
        }

        var br = new BinaryReader(stream);
        if (Helper.GetHexString(br, 0xd4, 4) == "FF00FFFF")
        {
            format.Type = FormatEnum.md; //interleaved without header
        }
        else if (Helper.GetHexString(br, 0x105, 3) == "333258") //32X
        {
            //sega 32x format bin
            format.Type = FormatEnum.gen;
            format.HeaderSizeInBytes = 0;
        }
        else if (Helper.GetHexString(br, 0x100, 4) == "53454741") //SEGA
        {
            format.Type = FormatEnum.gen;
        }
        else if (Helper.GetHexString(br, 0x1a8, 4) == "00FF0000")
        {
            format.Type = FormatEnum.gen;
        }
        else if (Helper.GetHexString(br, 0x490, 4) == "53637274")
        {
            //sega 32x format .32x
            format.Type = FormatEnum._32x;
            format.HeaderSizeInBytes = 512;
        }
        else if (Helper.GetHexString(br, 0x520, 4) == "20536563")
        {
            format.Type = FormatEnum.gen;
        }
        else if (Helper.GetHexString(br, 0x2d4, 4) == "FF00FFFF") //(32x also have these values)
        {
            format.Type = FormatEnum.smd;
            format.HeaderSizeInBytes = 512;
        }

        switch (format.Type)
        {
            case FormatEnum._32x:
            case FormatEnum.smd:
                format.HeaderRomSizeInBytes = (int)Helper.GetLong(br, 0x2d1) + 1;
                format.RomSizeInBytes -= format.HeaderSizeInBytes;
                break;
        }

        short romSize = Helper.GetByte(br, romSizeOffset);


        //if (format.Type is FormatEnum.smd or FormatEnum._32x && format.HeaderRomSizeInBytes != format.RomSizeInBytes)
        //{
        //    format.Comment = $"Rom size stored in header ({format.HeaderRomSizeInBytes} bytes) doesn't match real rom size ({format.RomSizeInBytes} bytes)";
        //}
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