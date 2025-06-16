using PluginTest.TestBase;

namespace PluginTest.Snes
{
    public class SnesTests
    {
        private const string DataPath = @"Snes\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public SnesTests()
        {
            var pluginPath = "snes.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        /// <summary>
        /// Default crc passed to the plugin is 111111111. It is used only for raw files (without header).
        /// </summary>
        [Theory]
        [InlineData("(CD973979) Gradius III (USA) ufos.rom", ".ufo", "Super UFO Pro SD", 512 * 1024, "cd973979")]
        [InlineData("(CD973979) Gradius III (USA) fig.rom", ".fig", "Pro Fighter", 512 * 1024, "cd973979")]
        //sfc is the clean raw format. Zip crc can be used.
        [InlineData("(CD973979) Gradius III (USA) sfc.rom", ".sfc", "Super Famicom", 512 * 1024, "11111111")]
        [InlineData("(CD973979) Gradius III (USA) swc.rom", ".smc", "Super Magicom / Wildcard", 512 * 1024, "cd973979")]
        [InlineData("(CD973979) Gradius III (USA) smc.rom", ".smc", "Super Magicom / Wildcard", 512 * 1024, "cd973979")]
        [InlineData("(CD973979) Gradius III (USA) gd.rom", ".gd3", "Game Doctor", 512 * 1024, "cd973979")]
        [InlineData("(CD973979) Gradius III (USA) ufo.rom", ".ufo", "UFO Super Drive", 512 * 1024, "cd973979")]
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