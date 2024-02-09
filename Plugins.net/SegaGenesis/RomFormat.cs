namespace SegaGenesis;

public class RomFormat
{
    public RomFormat()
    {
        Type = FormatEnum.None;
    }

    public FormatEnum Type { get; set; }
    public string Comment { get; set; } = "";
    public int HeaderSizeInBytes { get; set; }

    /// <summary>
    /// Real size of the rom, without header
    /// </summary>
    public int RomSizeInBytes { get; set; }
    public string Error { get; set; } = "";
    public int HeaderRomSizeInBytes { get; set; }
    public short HeaderVersion { get; set; }
}