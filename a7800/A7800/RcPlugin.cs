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
using System.Linq;

namespace a7800;

public class RcPlugin : IRomcenterPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.0"; //version of romcenter plugin internal interface

    private const string PlugInName = "Atari 7800 crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "2.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Atari 7800 crc calculator. Skip the Atari 7800 file header to calculate the crc32. Support a78, bin format.";

    private const long RomMinSizeInBytes = 1024; //min size of the core rom

    public string GetSignature(string filename, string zipcrc, out string format, out long size, out string comment,
        out string errorMessage)
    {
        var fs = new FileStream(filename, FileMode.Open);
        try
        {
            var romFormat = GetHeaderFormat(fs);

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

    /// <summary>
    /// Return rom format
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private static RomFormat GetHeaderFormat(Stream stream)
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
        format.Type = FormatEnum.Bin;

        var magicString = new string(br.ReadChars(9)).ToUpper();
        if (magicString == headerText)
        {
            format.Type = FormatEnum.A78;
            format.HeaderSizeInBytes = headerSizeInBytes;
            format.RomSizeInBytes = (int)stream.Length - headerSizeInBytes;

            //Get rom size stored in header
            br.BaseStream.Seek(0x31, SeekOrigin.Begin);
            format.HeaderRomSizeInBytes = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

            //check rom size is above min
            if (format.RomSizeInBytes < RomMinSizeInBytes)
            {
                format.Comment = $"Rom is too small ({format.RomSizeInBytes} bytes), must be {RomMinSizeInBytes} bytes or more";
                format.Type = FormatEnum.None;
            }
            //check header size match rom size
            else if (format.RomSizeInBytes != format.HeaderRomSizeInBytes)
            {
                format.Comment = $"Rom size stored in header ({format.HeaderRomSizeInBytes} bytes) doesn't match real rom size ({format.RomSizeInBytes} bytes)";
            }
        }
        else
        {
            // No header
            format.HeaderSizeInBytes = 0;
            format.RomSizeInBytes = (int)stream.Length;
        }

        //check size multiple of 1KB
        if (format.RomSizeInBytes % 1024 != 0)
        {
            format.Comment = "Not an atari 7800 rom (invalid size)";
            format.Type = FormatEnum.None;
            return format;
        }

        return format;
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

        //var crcInt = new Crc32().CalculateHash(fileStream, offset, length fileBuf);

        //format result
        //var crc = BitConverter.ToString(BitConverter.GetBytes(crcInt).Reverse().ToArray());
        //return crc.Replace("-", "");
    }
}