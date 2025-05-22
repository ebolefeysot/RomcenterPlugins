using PluginTest.TestBase;

namespace PluginTest.SegaGenesis
{
    public class GenesisTests
    {
        private const string DataPath = @"SegaGenesis\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public GenesisTests()
        {
            var pluginPath = "genesis.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        /// <summary>
        /// Default crc passed to the plugin is 111111111. It is used for raw files (without header).
        /// </summary>
        [Theory]
        [InlineData("(F9394E97) Sonic_bin.rom", ".gen", "Genesis", 512 * 1024, "11111111")]
        [InlineData("(F9394E97) Sonic_md.rom", ".gen", "Mega Drive", 512 * 1024, "f9394e97")]
        [InlineData("(F9394E97) Sonic_smd.rom", ".smd", "Super Magic Drive", 512 * 1024, "f9394e97")]
        //raw rom : 3072KB
        [InlineData("(53734E3A) Doom 32x_32x.rom", ".32x", "32X", 3 * 1024 * 1024, "11111111")]
        //header 512 bytes : 3073KB
        [InlineData("(53734E3A) Doom 32x_bin.rom", ".bin", "32X", 3 * 1024 * 1024, "53734e3a")]
        [InlineData("(3F888CF4) BIOS Genesis_bin.rom", ".bin", "Bios", 2 * 1024, "3f888cf4", "")]
        [InlineData("(5C12EAE8) 32X_G_BIOS.rom", ".bin", "Bios", 256, "5c12eae8", "")]
        [InlineData("(AA8DC2D8) unknown.rom", "", "", 128 * 1024, "11111111")]
        public void GetSignaturesTest(string fileName, string extension, string format, int size, string crc, string comment = "")
        {
            //force crc calculation for bios
            string? fileCrc = format == "Bios" ? "" : "11111111";
            var fs = new FileStream($"{DataPath}{fileName}", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(extension, result.Extension);
            Assert.Equal(size, result.Size);
            Assert.Equal(format, result.Format);
            Assert.StartsWith(comment, result.Comment);
            Assert.Equal(crc, result.Signature);
        }

        /// <summary>
        /// ZipCrc not sent (unzipped rom for example). It should be calculated.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EmptyZipCrcTest(string? fileCrc)
        {
            var fs = new FileStream($"{DataPath}(AA8DC2D8) unknown.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(128 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("aa8dc2d8", result.Signature);
        }
    }
}