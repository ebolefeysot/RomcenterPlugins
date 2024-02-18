using PluginLib;
using PluginTest.TestBase;

namespace PluginTest.nes
{
    public class NesTests
    {
        private const string DataPath = @"nes\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public NesTests()
        {
            var pluginPath = "nes.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        [Fact]
        public void GetSignatureInesTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(BCACBBF4) ms.nes", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".nes", result.Extension);
            Assert.Equal("iNES", result.Format);
            Assert.Equal(384 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("bcacbbf4", result.Signature);
        }

        [Fact]
        public void GetSignatureUnifTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(BCACBBF4) ms.unf", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".unf", result.Extension);
            Assert.Equal("UNIF", result.Format);
            Assert.Equal(384 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("bcacbbf4", result.Signature);
        }

        [Fact]
        public void GetSignatureFfeTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(BCACBBF4) ms.ffe", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".ffe", result.Extension);
            Assert.Equal("Super Magic Card", result.Format);
            Assert.Equal(384 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("bcacbbf4", result.Signature);
        }

        [Fact]
        public void GetSignatureNes2Test()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(BCACBBF4) ms nes2.nes", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".nes", result.Extension);
            Assert.Equal("NES 2.0", result.Format);
            Assert.Equal(384 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("bcacbbf4", result.Signature);
        }

        [Fact]
        public void GetSignatureBiosTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(5E607DCF) BIOS fds.bin", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(8 * 1024, result.Size);
            Assert.StartsWith("Rom is too small", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureUnknownTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(AA8DC2D8) unknown.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(128 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        /// <summary>
        /// If file is too big to be a rom, return zip crc, even if it is null
        /// </summary>
        [Fact]
        public void GetSignatureBigFileTest()
        {
            var bigRomFile = $"{DataPath}bigRom.bin";
            Helper.CreateDummyFile(bigRomFile, 2500000);

            const string? fileCrc = "11111111";
            var fs = new FileStream(bigRomFile, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(2500000, result.Size);
            Assert.StartsWith("Too big", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
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