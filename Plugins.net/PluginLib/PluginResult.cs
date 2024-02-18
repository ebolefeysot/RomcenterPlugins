namespace PluginLib
{
    public class PluginResult
    {
        /// <summary>
        /// Extension to use with this rom. Ex: '.nes'
        /// </summary>
        public string Extension { get; set; } = "";

        /// <summary>
        /// Rom format calculated from the rom layout. Ex: 'nes 2.0'
        /// </summary>
        public string Format { get; set; } = "";

        /// <summary>
        /// Size of the pure rom in bytes. Extra header is skipped. Bytes are de-swapped and de-interleaved.
        /// This size must be the same for all format.
        /// </summary>
        public long Size { get; set; }

        public string Comment { get; set; } = "";

        /// <summary>
        /// Corruption error message. Empty if no error.
        /// </summary>
        public string ErrorMessage { get; set; } = "";

        /// <summary>
        /// Rom signature. This can be a CRC, or any other unique value. It should match the datafile signatures.
        /// Ex: '1234ABCD'
        /// </summary>
        public string Signature { get; set; } = "";
    }
}