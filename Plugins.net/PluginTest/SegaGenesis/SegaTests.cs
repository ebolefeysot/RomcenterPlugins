using PluginTest.TestBase;

namespace PluginTest.SegaGenesis
{
    public class LynxTests
    {
        private const string DataPath = @"SegaGenesis\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public LynxTests()
        {
            var pluginPath = "genesis.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        [Theory]
        [InlineData("(F9394E97) Sonic_bin.rom", ".gen", "", 512 * 1024, "11111111")]
        [InlineData("(F9394E97) Sonic_md.rom", ".gen", "Mega Drive", 512 * 1024, "f9394e97")]
        [InlineData("(F9394E97) Sonic_smd.rom", ".smd", "Super Magic Drive", 512 * 1024, "f9394e97")]
        [InlineData("(53734E3A) Doom 32x_32x.rom", ".32x", "32X", 3 * 1024 * 1024, "53734e3a")]
        [InlineData("(53734E3A) Doom 32x_bin.rom", ".gen", "32X", 3 * 1024 * 1024, "11111111")]
        [InlineData("(3F888CF4) BIOS Genesis_bin.rom", "", "", 2 * 1024, "11111111", "Rom is too small")]
        [InlineData("(5C12EAE8) BIOS 32X.md", "", "", 2 * 1024, "11111111", "Rom is too small")]
        [InlineData("(AA8DC2D8) unknown.rom", "", "", 128 * 1024, "11111111")]
        public void GetSignaturesTest(string fileName, string extension, string format, int size, string crc, string comment = "")
        {
            const string? fileCrc = "11111111";
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

        [Fact]
        public void GetSignatureMdTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(F9394E97) Sonic_md.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".gen", result.Extension);
            Assert.Equal("Mega Drive", result.Format);
            Assert.Equal(512 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("f9394e97", result.Signature);
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