//Plug in for RomCenter
//
// (C) Copyright 2009-2025 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace sms
{
    public partial class RcPlugin
    {
        private const string DllType = "romcenter signature calculator"; //Identification string, do not change
        private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

        private const string PlugInName = "Sega Genesis Megadrive crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support

        private const string Description =
            "Sega Genesis Megadrive crc calculator. Support .32x, .bin, .smd and .md format.";

        private const long RomMinSizeInBytes = 32 * 1024; //min size of the core rom, 32KB
        private const long BiosMinSizeInBytes = 8 * 1024; //min size of bios files, 8KB
        private const long RomMaxSizeInBytes = 1 * 1024 * 1024; //max size of the core rom, 1MB

        public RcPlugin()
        {
            knownBiosCrcs = LoadKnownBiosCrcs("SmsBiosCrcs.json");
        }

        private static HashSet<string> LoadKnownBiosCrcs(string path)
        {
            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<BiosCrcConfig>(json);
            return new HashSet<string>(config?.KnownBiosCrcs?.Select(c => c.ToLowerInvariant()) ?? []);
        }

        private class BiosCrcConfig
        {
            public List<string> KnownBiosCrcs { get; set; } = [];
        }

        private string GetFileExtension(FormatEnum romFormatType)
        {
            return romFormatType switch
            {
                FormatEnum.Sms => ".sms",
                _ => ""
            };
        }

        private string GetFormatText(FormatEnum romFormat)
        {
            return romFormat switch
            {
                FormatEnum.Sms => "sms",
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
            var romFormat = new RomFormat();

            try
            {
                if (!stream.CanSeek)
                {
                    romFormat.Error = "Stream must support seeking.";
                    return romFormat;
                }

                long romSize = stream.Length;
                romFormat.RomSizeInBytes = (int)romSize;

                // Read ROM into memory
                byte[] romData = new byte[romSize];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(romData, 0, (int)romSize);

                // Check for "TMR SEGA" marker
                byte[] marker = Encoding.ASCII.GetBytes("TMR SEGA");
                int[] possibleOffsets = [0x7FF0, 0xBFF0, 0x1FFF0, 0x3FFF0];

                foreach (int offset in possibleOffsets)
                {
                    if (offset + marker.Length > romData.Length)
                        continue;

                    bool match = true;
                    for (int i = 0; i < marker.Length; i++)
                    {
                        if (romData[offset + i] != marker[i])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        romFormat.Format = FormatEnum.Sms;
                        romFormat.FormatTxt = "Master System";
                        return romFormat;
                    }
                }

                // If size is valid but no marker found
                romFormat.Format = FormatEnum.None;
                romFormat.FormatTxt = "Unknown or unsupported SMS ROM";
            }
            catch (Exception ex)
            {
                romFormat.Error = "Exception reading ROM: " + ex.Message;
            }

            return romFormat;
        }
    }
}
