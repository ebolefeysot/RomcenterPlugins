namespace A7800;

public class RomFormat
{
    public FormatEnum Type { get; set; }
    public string Comment { get; set; } = "";
    public int HeaderSizeInBytes { get; set; }
    public int RomSizeInBytes { get; set; }
    public string Error { get; set; } = "";
    public int HeaderRomSizeInBytes { get; set; }
    public short HeaderVersion { get; set; }
}