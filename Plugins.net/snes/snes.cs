//Plug in for RomCenter
//
// (C) Copyright 2009-2025 Eric Bole-Feysot
// All Rights Reserved.
//
//The dll produced by this code is a signature plug in for RomCenter application
//www.romcenter.com

//The main function (GetSignature) calculates a signature (crc...) of a rom given in parameters.

//Super NES ROM files are usually found in one of 2 variations of the same format.
//The most common filename extension is .SFC, followed by .SMC. Less common extensions include: .FIG, .SWC.

using Newtonsoft.Json;
using PluginLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace snes;

/// <summary>
/// RomCenter plugin for Super NES ROM files.
/// Handles various Super NES ROM formats including SFC, SMC, FIG, SWC, etc.
/// </summary>
public partial class RcPlugin
{
    /// <summary>
    /// Identification string for the plugin. Do not change.
    /// </summary>
    private const string DllType = "romcenter signature calculator";

    /// <summary>
    /// Version of RomCenter plugin internal interface.
    /// </summary>
    private const string InterfaceVersion = "4.2";

    /// <summary>
    /// Full name of the plugin.
    /// </summary>
    private const string PlugInName = "Sega Genesis Megadrive crc calculator";

    /// <summary>
    /// Author name of the plugin.
    /// </summary>
    private const string Author = "Eric Bole-Feysot";

    /// <summary>
    /// Version of the plugin.
    /// </summary>
    private const string Version = "2.0";

    /// <summary>
    /// Home page of the plugin.
    /// </summary>
    private const string WebPage = "www.romcenter.com";

    /// <summary>
    /// Email for plugin support.
    /// </summary>
    private const string Email = "eric@romcenter.com";

    /// <summary>
    /// Description of the plugin functionality.
    /// </summary>
    private const string Description = "Sega Genesis Megadrive crc calculator. Support .32x, .bin, .smd and .md format.";

    /// <summary>
    /// Minimum size of BIOS files in bytes.
    /// </summary>
    private const long BiosMinSizeInBytes = 256;

    /// <summary>
    /// Minimum size of the core ROM in bytes (32KB).
    /// </summary>
    private const long RomMinSizeInBytes = 32 * 1024;

    /// <summary>
    /// Maximum size of the core ROM in bytes (4MB).
    /// </summary>
    private const long RomMaxSizeInBytes = 4 * 1024 * 1024;

    /// <summary>
    /// Filename for the known BIOS CRCs JSON file.
    /// </summary>
    private const string KnownCrcsFile = "SegaGenesisBiosCrcs.json";


    /// <summary>
    /// Initializes a new instance of the <see cref="RcPlugin"/> class.
    /// Loads known BIOS CRCs from the configuration file.
    /// </summary>
    public RcPlugin()
    {
        knownBiosCrcs = LoadKnownCrcs(KnownCrcsFile);
    }

    /// <summary>
    /// Loads known BIOS CRCs from a JSON configuration file.
    /// </summary>
    /// <param name="path">Path to the JSON configuration file.</param>
    /// <returns>A HashSet containing known BIOS CRCs in lowercase format.</returns>
    private static HashSet<string> LoadKnownCrcs(string path)
    {
        if (File.Exists(KnownCrcsFile))
        {
            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<BiosCrcConfig>(json);
            return new HashSet<string>(config?.KnownBiosCrcs?.Select(c => c.ToLowerInvariant()) ?? []);
        }
        return new HashSet<string>([]);
    }

    /// <summary>
    /// Configuration class for storing known BIOS CRCs.
    /// </summary>
    private class BiosCrcConfig
    {
        /// <summary>
        /// Gets or sets the list of known BIOS CRCs.
        /// </summary>
        public List<string> KnownBiosCrcs { get; set; } = [];
    }

    /// <summary>
    /// Gets the file extension associated with a specific ROM format type.
    /// </summary>
    /// <param name="romFormatType">The ROM format type.</param>
    /// <returns>The file extension including the dot (e.g., ".sfc").</returns>
    private string GetFileExtension(FormatEnum romFormatType)
    {
        return romFormatType switch
        {
            FormatEnum.fig => ".fig",
            FormatEnum.sfc => ".sfc",
            FormatEnum.smc => ".smc",
            FormatEnum.GameDoctor => ".gd3",
            FormatEnum.ufo => ".ufo",
            FormatEnum.ufos => ".ufo",
            FormatEnum.Bios => ".rom",
            _ => ""
        };
    }

    /// <summary>
    /// Gets a human-readable description of the ROM format.
    /// </summary>
    /// <param name="romFormat">The ROM format enum value.</param>
    /// <returns>A string describing the ROM format.</returns>
    private string GetFormatText(FormatEnum romFormat)
    {
        return romFormat switch
        {
            FormatEnum.fig => "Pro Fighter",
            FormatEnum.GameDoctor => "Game Doctor",
            FormatEnum.sfc => "Super Famicom",
            FormatEnum.smc => "Super Magicom / Wildcard",
            FormatEnum.ufo => "UFO Super Drive",
            FormatEnum.ufos => "Super UFO Pro SD",
            FormatEnum.Bios => "Bios",
            _ => ""
        };
    }

    /// <summary>
    /// Determines if the header is a Game Doctor format header.
    /// </summary>
    /// <param name="header">The header data as a byte array.</param>
    /// <returns>True if the header is a Game Doctor format; otherwise, false.</returns>
    private bool IsGameDoctorHeader(byte[] header)
    {
        return header.Length >= 16 &&
               System.Text.Encoding.ASCII.GetString(header, 0, 16) == "GAME DOCTOR SF 3";
    }

    /// <summary>
    /// Determines if the header is a Pro Fighter format header by checking specific byte patterns.
    /// </summary>
    /// <param name="header">The header data as a byte array.</param>
    /// <returns>True if the header is a Pro Fighter format; otherwise, false.</returns>
    /// <remarks>
    /// Checks for specific byte patterns that indicate HiROM or LoROM Pro Fighter formats.
    /// </remarks>
    private bool IsProFighterHeaderSpecific(byte[] header)
    {
        if (header.Length < 4) return false;

        byte emu1 = header[0];
        byte emu2 = header[1];
        byte hirom = header[2];

        if (hirom == 0x80) // HiROM
        {
            if ((emu1 == 0x77 || emu1 == 0xF7) && emu2 == 0x83) return true;
            if (emu1 == 0xDD && (emu2 == 0x82 || emu2 == 0x02)) return true;
            if (emu1 == 0xFD && emu2 == 0x82) return true;
        }
        else if (hirom == 0x00) // LoROM
        {
            if ((emu1 == 0x77 || emu1 == 0x47) && emu2 == 0x83) return true;
            if ((emu1 == 0x00 || emu1 == 0x40) && (emu2 == 0x80 || emu2 == 0x00)) return true;
            if (emu1 == 0x11 && emu2 == 0x02) return true;
        }

        return false;
    }

    /// <summary>
    /// Detect super wildcard (swc) or Super Magicom (smc) headers based on specific byte patterns.
    /// </summary>
    /// <param name="header">The header data as a byte array.</param>
    /// <returns>True if the header is a Super Magicom format; otherwise, false.</returns>
    /// <remarks>
    /// SWC header IDs: 0xAA, 0xBB at bytes 8 and 9, type = 0x04
    /// </remarks>
    private bool IsSuperMagicomHeader(byte[] header)
    {
        // SWC header IDs: 0xAA, 0xBB at bytes 0 and 1, type = 0x05
        return header.Length >= 3 && header[8] == 0xAA && header[9] == 0xBB && header[10] == 0x04;
    }

    /// <summary>
    /// Determines the format of a ROM based on its header from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the ROM data.</param>
    /// <returns>A RomFormat object containing information about the detected format.</returns>
    /// <remarks>
    /// This method analyzes the ROM data from a stream to determine its format by checking
    /// for known copier headers and other format-specific characteristics.
    /// </remarks>
    private RomFormat GetHeaderFormat(Stream stream)
    {
        const int HEADER_SIZE = 512;
        stream.Seek(0, SeekOrigin.Begin);

        var format = new RomFormat
        {
            Format = FormatEnum.None,
            RomSizeInBytes = (int)stream.Length,
            HeaderSizeInBytes = 0
        };

        //rom size should be a multiple of 512KB
        if (stream.Length < 0x8000 || (stream.Length % 512) != 0)
        {
            return format; // Invalid size for SNES ROM
        }

        // First, check if we have a copier header by examining the first 512 bytes
        byte[] possibleHeader = new byte[HEADER_SIZE];
        int bytesRead = stream.Read(possibleHeader, 0, HEADER_SIZE);

        // Reset stream position
        stream.Seek(0, SeekOrigin.Begin);

        if (bytesRead < HEADER_SIZE)
        {
            // File too small to have a header, assume headerless
            return DetectHeaderlessFormat(stream, format);
        }

        // Check for specific copier headers first - be more aggressive about detection
        FormatEnum detectedCopierFormat = DetectCopierFormat(possibleHeader);

        var br = new BinaryReader(stream);

        // Check for valid SNES titles at different offsets
        bool hasValidHeaderlessTitle = false;
        bool hasValidHeaderedTitle = false;

        // Check headerless positions (0x7FC0 for LoROM, 0xFFC0 for HiROM)
        if (stream.Length > 0xFFC0 + 21)
        {
            string titleLoRom = Helper.GetString(br, 0x7FC0, 21);
            string titleHiRom = Helper.GetString(br, 0xFFC0, 21);

            if (IsValidSNESTitle(titleLoRom) || IsValidSNESTitle(titleHiRom))
            {
                hasValidHeaderlessTitle = true;
            }
        }

        // Check headered positions (add 512 bytes to offsets)
        if (stream.Length > 0x101C0 + 21)
        {
            string titleLoRomHeadered = Helper.GetString(br, 0x81C0, 21);  // 0x7FC0 + 512
            string titleHiRomHeadered = Helper.GetString(br, 0x101C0, 21); // 0xFFC0 + 512

            if (IsValidSNESTitle(titleLoRomHeadered) || IsValidSNESTitle(titleHiRomHeadered))
            {
                hasValidHeaderedTitle = true;
            }
        }

        if (!hasValidHeaderedTitle && !hasValidHeaderlessTitle)
        {
            return format; // No valid titles found, cannot determine format
        }

        // If we detected a specific copier format, trust it regardless of title validation
        if (detectedCopierFormat != FormatEnum.None)
        {
            format.Format = detectedCopierFormat;
            format.HeaderSizeInBytes = HEADER_SIZE;
            format.RomSizeInBytes -= HEADER_SIZE;
            return format;
        }

        // Determine format based on header detection and title validation
        // Priority: If we detected a specific copier format, prefer headered detection
        if (hasValidHeaderedTitle && !hasValidHeaderlessTitle)
        {
            // Has header - default to SMC since no specific copier was detected
            format.HeaderSizeInBytes = HEADER_SIZE;
            format.Format = FormatEnum.smc;
        }
        else if (hasValidHeaderlessTitle && !hasValidHeaderedTitle)
        {
            // Headerless ROM
            format.Format = FormatEnum.sfc;
            format.HeaderSizeInBytes = 0;
        }
        else if (hasValidHeaderlessTitle && hasValidHeaderedTitle)
        {
            // Ambiguous case - use additional heuristics (prefer headered if size suggests it)
            format = ResolveAmbiguousFormat(stream, possibleHeader, detectedCopierFormat);
        }
        else
        {
            // No valid titles found - try size-based heuristics
            format = DetectBySize(stream, possibleHeader, detectedCopierFormat);
        }

        if (format.Format != FormatEnum.None && format.HeaderSizeInBytes > 0)
        {
            format.RomSizeInBytes -= format.HeaderSizeInBytes;
        }

        return format;
    }

    /// <summary>
    /// Detects the copier format based on the header data.
    /// </summary>
    /// <param name="header">The header data as a byte array.</param>
    /// <returns>The detected copier format, or FormatEnum.None if no known format is detected.</returns>
    /// <remarks>
    /// Checks for various copier formats in order of specificity (most specific first).
    /// </remarks>
    private FormatEnum DetectCopierFormat(byte[] header)
    {
        if (header.Length < 16) return FormatEnum.None;

        // Check in order of specificity (most specific first)

        // UFO Super Drive detection (most specific signatures first)
        if (IsUFOHeaderSpecific(header))
        {
            return FormatEnum.ufo;
        }

        // UFO Super Drive detection (most specific signatures first)
        if (IsUFOSplitSpecific(header))
        {
            return FormatEnum.ufos;
        }

        // Super Wild Card / Super Magicom detection (very specific signature)
        if (IsSuperMagicomHeader(header))
        {
            return FormatEnum.smc; //more used than swc
        }

        // Game Doctor detection (more restrictive)
        if (IsGameDoctorHeader(header))
        {
            return FormatEnum.GameDoctor;
        }

        // Pro Fighter detection (more restrictive)
        if (IsProFighterHeaderSpecific(header))
        {
            return FormatEnum.fig;
        }

        return FormatEnum.None;
    }

    /// <summary>
    /// Determines if the header is a UFO Super Drive format header.
    /// </summary>
    /// <param name="header">The header data as a byte array.</param>
    /// <returns>True if the header is a UFO Super Drive format; otherwise, false.</returns>
    private bool IsUFOHeaderSpecific(byte[] header)
    {
        if (header.Length < 8) return false;

        var text = System.Text.Encoding.ASCII.GetString(header, 8, Math.Min(header.Length, 8));
        if (text == "SUPERUFO")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if the header is a UFO Split format header.
    /// </summary>
    /// <param name="header">The header data as a byte array.</param>
    /// <returns>True if the header is a UFO Split format; otherwise, false.</returns>
    private bool IsUFOSplitSpecific(byte[] header)
    {
        if (header.Length < 8) return false;

        var text = System.Text.Encoding.ASCII.GetString(header, 8, Math.Min(header.Length, 8));
        if (text == "SFCUFOSD")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the format to headerless SFC format.
    /// </summary>
    /// <param name="stream">The stream containing the ROM data.</param>
    /// <param name="format">The RomFormat object to update.</param>
    /// <returns>The updated RomFormat object.</returns>
    private RomFormat DetectHeaderlessFormat(Stream stream, RomFormat format)
    {
        format.Format = FormatEnum.sfc;
        format.HeaderSizeInBytes = 0;
        return format;
    }

    /// <summary>
    /// Resolves ambiguous ROM format cases where both headered and headerless detection methods yield valid results.
    /// </summary>
    /// <param name="stream">The stream containing the ROM data.</param>
    /// <param name="possibleHeader">The first 512 bytes of the ROM that might be a header.</param>
    /// <param name="detectedCopierFormat">Any previously detected copier format.</param>
    /// <returns>A RomFormat object with the resolved format information.</returns>
    /// <remarks>
    /// Uses various heuristics including file size analysis and header pattern detection to determine
    /// whether the ROM has a header and what format it is.
    /// </remarks>
    private RomFormat ResolveAmbiguousFormat(Stream stream, byte[] possibleHeader, FormatEnum detectedCopierFormat)
    {
        var format = new RomFormat
        {
            RomSizeInBytes = (int)stream.Length,
            HeaderSizeInBytes = 0,
            Format = FormatEnum.sfc
        };

        // If we detected a specific copier format, definitely has header
        if (detectedCopierFormat != FormatEnum.None)
        {
            format.HeaderSizeInBytes = 512;
            format.Format = detectedCopierFormat;
            format.RomSizeInBytes -= 512;
            return format;
        }

        // Check if the file size suggests a header
        long sizeWithoutHeader = stream.Length - 512;

        // Common SNES ROM sizes (in KB): 256, 512, 1024, 2048, 4096, etc.
        bool sizeWithoutHeaderIsPowerOf2 = IsPowerOfTwoKB(sizeWithoutHeader);
        bool fullSizeIsPowerOf2 = IsPowerOfTwoKB(stream.Length);

        // If removing 512 bytes gives us a standard ROM size, it likely has a header
        if (sizeWithoutHeaderIsPowerOf2 && !fullSizeIsPowerOf2)
        {
            format.HeaderSizeInBytes = 512;
            format.Format = FormatEnum.smc; // Default to SMC for unknown copier headers
            format.RomSizeInBytes = (int)sizeWithoutHeader;
        }
        else if (fullSizeIsPowerOf2 && !sizeWithoutHeaderIsPowerOf2)
        {
            // Full size is standard, so probably headerless
            format.HeaderSizeInBytes = 0;
            format.Format = FormatEnum.sfc;
        }
        else
        {
            // Size doesn't help - check if header bytes look like a copier header
            // Many copier headers start with non-zero bytes, while ROM data often starts with opcodes
            bool headerLooksLikeCopier = HasCopierHeaderPattern(possibleHeader);

            if (headerLooksLikeCopier)
            {
                format.HeaderSizeInBytes = 512;
                format.Format = FormatEnum.smc;
                format.RomSizeInBytes -= 512;
            }
            // If uncertain, default to headerless (more common in modern dumps)
        }

        return format;
    }

    private bool HasCopierHeaderPattern(byte[] header)
    {
        if (header.Length < 16) return false;

        // Check for common copier header patterns:
        // 1. All zeros (some copiers zero-fill unused header space)
        bool allZeros = header.Take(16).All(b => b == 0);
        if (allZeros) return true;

        // 2. Repeating patterns common in copier headers
        bool hasRepeatingPattern = false;
        for (int i = 0; i < 8; i++)
        {
            if (header[i] == header[i + 8])
            {
                hasRepeatingPattern = true;
                break;
            }
        }

        // 3. High-value bytes that are uncommon in ROM code start
        int highBytes = header.Take(16).Count(b => b > 0x80);
        bool hasHighBytes = highBytes > 8; // More than half are high bytes

        // 4. Text patterns that might indicate copier info
        try
        {
            string headerText = System.Text.Encoding.ASCII.GetString(header.Take(32).ToArray());
            bool hasTextPattern = headerText.Any(c => char.IsLetter(c) && char.IsUpper(c));

            return hasRepeatingPattern || hasHighBytes || hasTextPattern;
        }
        catch
        {
            return hasRepeatingPattern || hasHighBytes;
        }
    }

    private RomFormat DetectBySize(Stream stream, byte[] possibleHeader, FormatEnum detectedCopierFormat)
    {
        var format = new RomFormat
        {
            RomSizeInBytes = (int)stream.Length,
            HeaderSizeInBytes = 0,
            Format = FormatEnum.None
        };

        // If we detected a specific copier format, use it
        if (detectedCopierFormat != FormatEnum.None)
        {
            format.Format = detectedCopierFormat;
            format.HeaderSizeInBytes = 512;
            format.RomSizeInBytes -= 512;
            return format;
        }

        // Check if size indicates a header (common ROM sizes are powers of 2)
        long sizeWithoutHeader = stream.Length - 512;

        if (stream.Length > 512 && IsPowerOfTwoKB(sizeWithoutHeader))
        {
            format.Format = FormatEnum.smc; // Default to SMC for unknown headers
            format.HeaderSizeInBytes = 512;
            format.RomSizeInBytes = (int)sizeWithoutHeader;
        }
        else if (IsPowerOfTwoKB(stream.Length))
        {
            format.Format = FormatEnum.sfc; // Headerless
            format.HeaderSizeInBytes = 0;
        }
        else
        {
            // Assume headerless if we can't determine
            format.Format = FormatEnum.sfc;
            format.HeaderSizeInBytes = 0;
        }

        return format;
    }

    private bool IsPowerOfTwoKB(long size)
    {
        // Check if size is a power of 2 and at least 256KB
        if (size < 256 * 1024) return false;

        long kb = size / 1024;
        return (kb & (kb - 1)) == 0; // Power of 2 check
    }

    private bool IsValidSNESTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;

        // More robust title validation
        int validChars = 0;
        int totalChars = 0;

        foreach (char c in title)
        {
            totalChars++;

            // Valid characters: letters, digits, spaces, and common punctuation
            if (char.IsLetterOrDigit(c) ||
                char.IsWhiteSpace(c) ||
                "!@#$%^&*()_+-=[]{}|;:'\",.<>?/~`".Contains(c))
            {
                validChars++;
            }

            // Control characters (except space) are suspicious
            if (char.IsControl(c) && c != ' ' && c != '\0')
            {
                return false;
            }
        }

        // At least 70% of characters should be valid, and title should have some content
        return validChars >= totalChars * 0.7 && title.Trim().Length > 0;
    }
}
