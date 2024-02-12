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
using System.Text;

namespace Nes;

public class RcPlugin : IRomcenterPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.0"; //version of romcenter plugin internal interface

    private const string PlugInName = "Nintento nes crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "1.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Nintendo nes crc calculator. Support .nes (ines/nes2), .unf, .ffe";

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
                switch (romFormat.Type)
                {
                    case FormatEnum.ines:
                    case FormatEnum.nes2:
                    case FormatEnum.ffe:
                        hash = GetCrc32(fs, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                        break;
                    case FormatEnum.unif:
                        var mem = GetUnifRom(fs);
                        romFormat.RomSizeInBytes = mem.Length;
                        hash = GetCrc32(new MemoryStream(mem), 0, romFormat.RomSizeInBytes);
                        break;
                    case FormatEnum.TooBig:
                    case FormatEnum.TooSmall:
                        hash = zipcrc;
                        break;
                    default:
                        if (string.IsNullOrEmpty(zipcrc))
                        {
                            zipcrc = GetCrc32(fs, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                        }
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
            case FormatEnum.ines:
                return ".nes";
            case FormatEnum.unif:
                return ".unf";
            case FormatEnum.nes2:
                return ".nes";
            case FormatEnum.ffe:
                return ".ffe";
            default:
                return "";
        }
    }

    private byte[] GetUnifRom(FileStream stream)
    {
        var mem = new MemoryStream();
        stream.CopyTo(mem);
        stream.Position = 0;
        var br = new BinaryReader(stream);

        Byte[] prg = new byte[0];
        Byte[] chr = new byte[0];
        var baseStreamLength = br.BaseStream.Length;
        br.BaseStream.Position = 32;
        while (br.BaseStream.Position < baseStreamLength)
        {
            var mapper = GetNextMapper(br);
            if (mapper.Name.StartsWith("PRG"))
            {
                var prg0 = br.ReadBytes(mapper.Length);
                var prg1 = prg.Concat(prg0).ToArray();
                prg = prg1;
                continue;
            }

            if (mapper.Name.StartsWith("CHR"))
            {
                var chr0 = br.ReadBytes(mapper.Length);
                var chr1 = chr.Concat(chr0).ToArray();
                chr = chr1;
                continue;
            }

            br.BaseStream.Seek(mapper.Length, SeekOrigin.Current);
        }

        var rom = new byte[0];
        rom = rom.Concat(prg).ToArray();
        rom = rom.Concat(chr).ToArray();
        return rom;
    }

    private Mapper GetNextMapper(BinaryReader br)
    {
        var mapper = new Mapper();
        mapper.Name = new string(br.ReadChars(4));
        mapper.Length = br.ReadInt32();
        return mapper;
    }

    /// <summary>
    /// Return rom format
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    private static void GetHeaderFormat(Stream stream, RomFormat format)
    {
        /*
         *Datas definitions
         * INES format
           //_________________
           // Format .nes (ines):
           // 0: N
           // 1: E
           // 2: S
           // 3: $1A
           // 4: Nb of 16KB block of the prg
           // 5: Nb of 16KB block of the chr
           // ...
           // $10: Start of prg blocks
           // only prg+chr blocks are read for crc calculations, 16 bytes header is skipped
           //a nes rom is > 16Kb
           //the header size is 16B

        FFE format: 512 bytes header
        NES2 format: =ines avec modification du byte 7
        UNIF format
         */
        const int headerStart = 0x0;
        const long romMinSizeInBytes = 8 * 1024; //min size of the core rom
        const long romMaxSizeInBytes = 2 * 1024 * 1024; //max size of the core rom

        format.Type = FormatEnum.None;
        stream.Seek(headerStart, SeekOrigin.Begin);
        format.RomSizeInBytes = (int)stream.Length;

        if (stream.Length < romMinSizeInBytes)
        {
            //too small, not a genesis game
            format.Comment = "Too small for a game";
            format.Type = FormatEnum.TooSmall;
            return;
        }

        if (stream.Length > romMaxSizeInBytes)
        {
            //too small, not a genesis game
            format.Comment = "Too big for a game";
            format.Type = FormatEnum.TooBig;
            return;
        }

        var br = new BinaryReader(stream, Encoding.ASCII);

        if (Helper.GetString(br, 0, 3) == "NES")
        {
            format.Type = FormatEnum.ines;
            format.Comment = "iNES";
            format.HeaderSizeInBytes = 16;

            if ((Helper.GetByte(br, 7) & 0x0C) == 0x08) //NES 2.0
            {
                format.Type = FormatEnum.nes2;
                format.Comment = "NES 2.0";
            }
        }
        else //unif, header is 32
        if (Helper.GetString(br, 0, 4) == "UNIF")
        {
            format.Type = FormatEnum.unif;
            format.Comment = "UNIF";
            format.HeaderSizeInBytes = 32;
        }
        else
        {
            //ffe: header is 512
            //rom size is x*8KB. Check if it has 512 bytes more.
            if (format.RomSizeInBytes % 8192 == 512)
            {
                format.HeaderSizeInBytes = 512;
                format.Type = FormatEnum.ffe;
                format.Comment = "FFE";
            }
        }

        format.RomSizeInBytes -= format.HeaderSizeInBytes;
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

internal struct Mapper
{
    public string Name;
    public int Length;
}