// --------------------------------------------------------------------------------------------------------------------
// <copyright file="crc32.cs" company="Romcenter">
//   Copyright Eric Bole-Feysot
// </copyright>
// <summary>
//   The crc 32.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace a7800;

/// <summary>
/// The crc 32.
/// </summary>
public class Crc32
{
    public string CalculateHash(Stream f)
    {
        var hashAlgorithm = new Crc32HashAlgorithm();

        // get array of 4 8 bits crc values
        byte[] crc = hashAlgorithm.ComputeHash(f);

        // convert to a 32bits hex value
        return Crc32HashAlgorithm.ToHex(crc);
    }
}