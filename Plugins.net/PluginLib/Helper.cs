using System;
using System.IO;
using System.Linq;

namespace PluginLib
{
    public static class Helper
    {
        /// <summary>
        /// Return a string representing bytes.Ex: 1234ABCD
        /// </summary>
        /// <param name="br"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetHexString(BinaryReader br, int offset, int length)
        {
            br.BaseStream.Position = offset;
            byte[] magicBytes = br.ReadBytes(length).ToArray();
            var result = BitConverter.ToString(magicBytes, 0).Replace("-", "");
            return result;
        }

        /// <summary>
        /// Return binary content as string.
        /// </summary>
        /// <param name="br">Specify the encoding (ascii...) when creating BinaryReader</param>
        /// <param name="offset"></param>
        /// <param name="length">Number of char</param>
        /// <returns></returns>
        public static string GetString(BinaryReader br, int offset, int length)
        {
            br.BaseStream.Position = offset;
            var chars = br.ReadChars(length).ToArray();
            return new string(chars);
        }

        public static byte GetByte(BinaryReader br, int offset)
        {
            //advance to offset
            br.BaseStream.Position = offset;
            return br.ReadByte();
        }

        /// <summary>
        /// Take a memory stream, and return a de-interleave memory stream
        /// </summary>
        /// <param name="srcStream">interleaved memory stream</param>
        /// <param name="blockLength">Length of an interleaved block</param>
        /// <param name="headerSize">Size of the header to skip</param>
        /// <param name="swap">true to swap bytes (use high block bytes first)</param>
        /// <returns></returns>
        public static MemoryStream DeInterleave(MemoryStream srcStream, int blockLength, int headerSize, bool swap = false)
        {
            srcStream.Position = 0;

            //create dest stream
            var dstStream = new MemoryStream((int)srcStream.Length);
            byte[] data = srcStream.GetBuffer();
            byte[] dataDst = dstStream.GetBuffer();

            //copy header (if any)
            if (headerSize > 0)
            {
                Array.Copy(data, dataDst, headerSize);
            }

            // Size of file is 512 + nb_blocks * 2 * 8192
            var dataLength = (int)srcStream.Length - headerSize;
            int nbBlocks = dataLength / (2 * blockLength);

            // For each block of 8KB
            var offset = headerSize;
            for (int i = 0; i < nbBlocks; i++)
            {
                // For each byte in block
                for (int j = 0; j < blockLength; j++)
                {
                    if (swap)
                    {
                        dataDst[offset + 2 * j] = data[offset + blockLength + j]; //byte high
                        dataDst[offset + 2 * j + 1] = data[offset + j]; //byte low
                    }
                    else
                    {
                        dataDst[offset + 2 * j] = data[offset + j]; //byte low
                        dataDst[offset + 2 * j + 1] = data[offset + blockLength + j]; //byte high
                    }
                }

                // Next block
                offset += 2 * blockLength;
            }

            return new MemoryStream(dataDst);
        }


        /// <summary>
        /// Take a memory stream, and return a de-interleave, un-swapped and bytes re-ordered memory stream
        /// </summary>
        /// <param name="srcStream">interleaved memory stream</param>
        /// <param name="headerSize">Size of the header to skip</param>
        /// <param name="byteOrder">Bytes order in the stream. Bytes will be re-ordered accordingly.</param>
        /// <returns></returns>
        public static MemoryStream CleanStream(MemoryStream srcStream, int headerSize, ByteOrderEnum byteOrder)
        {
            switch (byteOrder)
            {
                case ByteOrderEnum.ByteSwapped:
                    return UnSwapBytes(srcStream, headerSize);
                case ByteOrderEnum.LittleEndian:
                    return ReverseBytes(srcStream, headerSize);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// ABCD -> BADC
        /// </summary>
        /// <param name="srcStream"></param>
        /// <param name="headerSize"></param>
        /// <returns></returns>
        private static MemoryStream UnSwapBytes(MemoryStream srcStream, int headerSize)
        {
            var ms = new MemoryStream();
            srcStream.Position = 0;
            if (headerSize > 0)
            {
                //copy header
                srcStream.CopyTo(ms, headerSize);
                srcStream.Position = headerSize;
                ms.Position = headerSize;
            }

            //swap
            byte[] data = srcStream.GetBuffer();
            for (int ctr = 0; ctr < data.Length / 2; ctr++)
            {
                var high = data[ctr * 2];
                var low = data[ctr * 2 + 1];
                data[ctr * 2] = low;
                data[ctr * 2 + 1] = high;
            }

            ms.Write(data, 0, data.Length);
            return ms;
        }

        /// <summary>
        /// ABCD -> DCBA
        /// </summary>
        /// <param name="srcStream"></param>
        /// <param name="headerSize"></param>
        /// <returns></returns>
        private static MemoryStream ReverseBytes(MemoryStream srcStream, int headerSize)
        {
            srcStream.Position = 0;
            var br = new BinaryReader(srcStream);
            var bytes = br.ReadBytes((int)br.BaseStream.Length);

            for (int i = 0; i < bytes.Length / 4; i++)
            {
                //swap 4 bytes
                Array.Reverse(bytes, i * 4, 4);
            }
            return new MemoryStream(bytes);
        }

        public static int GetLong(BinaryReader br, int offset)
        {
            br.BaseStream.Position = offset;
            var bytes = br.ReadBytes(4);

            // Swap byte order
            uint b = BitConverter.ToUInt32([bytes[3], bytes[2], bytes[1], bytes[0]], 0);

            return (int)b;
        }

        public static void CreateDummyFile(string fileName, long length)
        {
            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(length);
        }

        public enum ByteOrderEnum
        {
            /// <summary>
            /// ABCD
            /// </summary>
            Regular,
            /// <summary>
            /// BADC
            /// </summary>
            ByteSwapped,
            /// <summary>
            /// DCBA
            /// </summary>
            LittleEndian
        }
    }
}
