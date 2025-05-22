namespace sms;

public class RomFormat
{
    public RomFormat()
    {
        Format = FormatEnum.None;
    }

    public FormatEnum Format { get; set; }

    /// <summary>
    /// Information about the rom format. This will be displayed in romcenter.
    /// </summary>
    public string Comment { get; set; } = "";

    /// <summary>
    /// Size of the header to skip in bytes. 0 if no header.
    /// </summary>
    public int HeaderSizeInBytes { get; set; }

    /// <summary>
    /// Real size of the rom, without header. Crc is calculated on this size.
    /// </summary>
    public int RomSizeInBytes { get; set; }

    /// <summary>
    /// Error while reading the rom. This will be tagged as 'corrupt' in romcenter.
    /// </summary>
    public string Error { get; set; } = "";

    /// <summary>
    /// Size of the rom as written in the header. It should match <see cref="RomSizeInBytes"/>.
    /// </summary>
    public int HeaderRomSizeInBytes { get; set; }

    /// <summary>
    /// Set the size of blocks if rom is interleaved. Leave 0 if not interleaved.
    /// </summary>
    public int InterleavedBlockSize { get; set; }

    /// <summary>
    /// Set to true if the rom is swapped
    /// </summary>
    public bool Swapped { get; set; }

    /// <summary>
    /// Rom format description calculated from the rom layout. Ex: 'nes 2.0'
    /// </summary>
    public string FormatTxt { get; set; } = "";
}