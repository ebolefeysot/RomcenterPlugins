namespace Nes;

public class RomFormat
{
    public RomFormat()
    {
        Format = FormatEnum.None;
    }

    public FormatEnum Format { get; set; }
    public string Comment { get; set; } = "";
    public int HeaderSizeInBytes { get; set; }

    /// <summary>
    /// Real size of the rom, without header
    /// </summary>
    public int RomSizeInBytes { get; set; }
    public string Error { get; set; } = "";
    public int HeaderRomSizeInBytes { get; set; }
    public short HeaderVersion { get; set; }
    public string FormatTxt { get; set; } = "";
}