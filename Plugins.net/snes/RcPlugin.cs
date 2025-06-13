using PluginLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace snes
{
    public partial class RcPlugin : IRomcenterPlugin
    {
        private readonly HashSet<string> knownBiosCrcs;

        public PluginResult GetSignature(Stream romStream, string? zipCrc)
        {
            var result = new PluginResult();
            var hash = zipCrc?.ToLowerInvariant() ?? "";

            try
            {
                //analyse rom
                //check rom size is above min
                RomFormat romFormat;

                if (romStream.Length < BiosMinSizeInBytes)
                {
                    romFormat = new RomFormat
                    {
                        Comment = $"Rom is too small ({romStream.Length} bytes), must be {RomMinSizeInBytes} bytes or more",
                        Format = FormatEnum.TooSmall,
                        RomSizeInBytes = (int)romStream.Length
                    };
                }
                //check rom size is above min
                else if (romStream.Length > RomMaxSizeInBytes)
                {
                    romFormat = new RomFormat
                    {
                        Comment = $"Rom is too big ({romStream.Length} bytes), must be {RomMaxSizeInBytes} bytes or less",
                        Format = FormatEnum.TooBig,
                        RomSizeInBytes = (int)romStream.Length
                    };
                }
                else
                {
                    romFormat = GetHeaderFormat(romStream);

                    //calculate hash
                    if (romFormat.InterleavedBlockSize > 0)
                    {
                        //interleaved/swapped rom
                        var ms = new MemoryStream();
                        romStream.Position = 0;
                        romStream.CopyTo(ms);
                        romStream = Helper.DeInterleave(ms, romFormat.InterleavedBlockSize, romFormat.HeaderSizeInBytes, romFormat.Swapped);
                        hash = GetCrc32(romStream, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                    }
                    else
                    {
                        //calculate hash
                        if (romFormat.HeaderSizeInBytes == 0 && romFormat.RomSizeInBytes == romStream.Length && !string.IsNullOrWhiteSpace(zipCrc))
                        {
                            hash = zipCrc!;
                        }
                        else
                        {
                            hash = GetCrc32(romStream, romFormat.HeaderSizeInBytes, romFormat.RomSizeInBytes);
                        }
                    }
                }

                //check bios crc
                if (knownBiosCrcs.Contains(hash))
                {
                    romFormat.Format = FormatEnum.Bios;
                }
                else
                {
                    //if not a bios, check size
                    if (romStream.Length < RomMinSizeInBytes)
                    {
                        romFormat.Comment =
                            $"Rom is too small ({romStream.Length} bytes), must be {RomMinSizeInBytes} bytes or more";
                        romFormat.Format = FormatEnum.TooSmall;
                    }
                }

                //prepare result
                result.Extension = GetFileExtension(romFormat.Format);
                result.Format = GetFormatText(romFormat.Format);
                result.Size = romFormat.RomSizeInBytes;
                result.Comment = romFormat.Comment;
                result.ErrorMessage = romFormat.Error;
                result.Signature = hash;

                return result;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }

            return result;
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
    }
}