namespace Lynx;

public class RomFormat
{
    public FormatEnum Format { get; set; }
    public string Comment { get; set; } = "";

    /// <summary>
    /// Size of the header.
    /// </summary>
    public int HeaderSizeInBytes { get; set; }

    /// <summary>
    /// Real size of the rom, without header
    /// </summary>
    public int RomSizeInBytes { get; set; }
    public string Error { get; set; } = "";
    public short HeaderVersion { get; set; }
    public string FormatTxt { get; set; } = "";
}