namespace N64;

public enum FormatEnum
{
    None,
    /// <summary>
    /// Little Endian
    /// </summary>
    N64,
    /// <summary>
    /// Big Endian (raw)
    /// </summary>
    Z64,
    /// <summary>
    /// Byte Swapped
    /// </summary>
    V64,
    TooBig,
    TooSmall
}