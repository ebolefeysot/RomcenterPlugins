using PluginTest.TestBase;

namespace PluginTest.Fds
{
    public class FdsTests
    {
        private const string DataPath = @"Fds\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public FdsTests()
        {
            var pluginPath = "fds.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        [Theory]
        [InlineData("[7680ACFD] Goonies fds.bin", ".fds", "Fds", 64 * 1024, "11111111")]
        [InlineData("(AA8DC2D8) unknown.rom", "", "", 128 * 1024, "11111111")]
        [InlineData("[5E607DCF] BIOS fds (Japan).bin", ".bin", "Bios", 8 * 1024, "5e607dcf")]
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
            Assert.Equal(".lyx", result.Extension);
            Assert.Equal("Lyx", result.Format);
            Assert.Equal(128 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("aa8dc2d8", result.Signature);
        }
    }
}