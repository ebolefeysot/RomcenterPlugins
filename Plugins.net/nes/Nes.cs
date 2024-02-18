//Plug in for RomCenter
//
// (C) Copyright 2009-2024 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using PluginLib;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Nes;

public partial class RcPlugin
{
    private const string DllType = "romcenter signature calculator"; //Identification string, do not change
    private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

    private const string PlugInName = "Nintento nes crc calculator"; //full name of plug in
    private const string Author = "Eric Bole-Feysot"; //Author name
    private const string Version = "1.0"; //version of the plug in
    private const string WebPage = "www.romcenter.com"; //home page of plug in
    private const string Email = "eric@romcenter.com"; //Email of plug in support
    private const string Description = "Nintendo nes crc calculator. Support .nes (ines/nes2), .unf, .ffe";

    private const long RomMinSizeInBytes = 32 * 1024; //min size of the core rom, 32KB
    private const long RomMaxSizeInBytes = 4 * 1024 * 1024; //max size of the core rom, 4MB

    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.ines => "iNES",
            FormatEnum.unif => "UNIF",
            FormatEnum.ffe => "Super Magic Card",
            FormatEnum.nes2 => "NES 2.0",
            _ => ""
        };
    }
    private string GetFileExtension(FormatEnum romFormatType)
    {
        return romFormatType switch
        {
            FormatEnum.ines => ".nes",
            FormatEnum.unif => ".unf",
            FormatEnum.nes2 => ".nes",
            FormatEnum.ffe => ".ffe",
            _ => ""
        };
    }

    private byte[] GetUnifRom(Stream stream)
    {
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
    /// <returns></returns>
    private static RomFormat GetHeaderFormat(Stream stream)
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
        var format = new RomFormat();

        format.Format = FormatEnum.None;
        stream.Seek(headerStart, SeekOrigin.Begin);
        format.RomSizeInBytes = (int)stream.Length;

        if (stream.Length < romMinSizeInBytes)
        {
            //too small, not a genesis game
            format.Comment = Texts.TooSmallForAGame;
            format.Format = FormatEnum.TooSmall;
            return format;
        }

        if (stream.Length > romMaxSizeInBytes)
        {
            //too small, not a genesis game
            format.Comment = Texts.TooBigForAGame;
            format.Format = FormatEnum.TooBig;
            return format;
        }

        var br = new BinaryReader(stream, Encoding.ASCII);

        if (Helper.GetString(br, 0, 3) == "NES")
        {
            format.Format = FormatEnum.ines;
            format.FormatTxt = "iNES";
            format.HeaderSizeInBytes = 16;

            if ((Helper.GetByte(br, 7) & 0x0C) == 0x08) //NES 2.0
            {
                format.Format = FormatEnum.nes2;
                format.FormatTxt = "NES 2.0";
            }
        }
        else //unif, header is 32
        if (Helper.GetString(br, 0, 4) == "UNIF")
        {
            format.Format = FormatEnum.unif;
            format.FormatTxt = "UNIF";
            format.HeaderSizeInBytes = 32;
        }
        else
        {
            //ffe: header is 512
            //rom size is x*8KB. Check if it has 512 bytes more.
            if (format.RomSizeInBytes % 8192 == 512)
            {
                format.HeaderSizeInBytes = 512;
                format.Format = FormatEnum.ffe;
                format.FormatTxt = "FFE";
            }
        }

        format.RomSizeInBytes -= format.HeaderSizeInBytes;
        return format;
    }
}

internal struct Mapper
{
    public string Name;
    public int Length;
}