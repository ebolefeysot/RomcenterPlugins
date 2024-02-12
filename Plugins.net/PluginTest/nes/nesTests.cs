using PluginBase;
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
            var result = romcenterPlugin.GetSignature($"{DataPath}(BCACBBF4) ms.nes", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".nes", format.ToLowerInvariant());
            Assert.Equal(384 * 1024, size);
            Assert.Equal("iNES", comment);
            Assert.Equal("bcacbbf4", result);
        }

        [Fact]
        public void GetSignatureUnifTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(BCACBBF4) ms.unf", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".unf", format.ToLowerInvariant());
            Assert.Equal(384 * 1024, size);
            Assert.Equal("UNIF", comment);
            Assert.Equal("bcacbbf4", result);
        }

        [Fact]
        public void GetSignatureFfeTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(BCACBBF4) ms.ffe", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".ffe", format.ToLowerInvariant());
            Assert.Equal(384 * 1024, size);
            Assert.Equal("FFE", comment);
            Assert.Equal("bcacbbf4", result);
        }

        [Fact]
        public void GetSignatureNes2Test()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(BCACBBF4) ms nes2.nes", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".nes", format.ToLowerInvariant());
            Assert.Equal(384 * 1024, size);
            Assert.Equal("NES 2.0", comment);
            Assert.Equal("bcacbbf4", result);
        }

        [Fact]
        public void GetSignatureBiosTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(5E607DCF) BIOS fds.bin", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(8 * 1024, size);
            Assert.StartsWith("", comment);
            Assert.Equal(fileCrc, result); //zip crc should be used
        }

        [Fact]
        public void GetSignatureUnknownTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(AA8DC2D8) unknown.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(128 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal(fileCrc, result); //zip crc should be used
        }

        /// <summary>
        /// If file is too big to be a rom, return zip crc, even if it is null
        /// </summary>
        [Fact]
        public void GetSignatureBigFileTest()
        {
            var bigRomFile = $"{DataPath}bigRom.bin";
            Helper.CreateDummyFile(bigRomFile, 2500000);

            const string? fileCrc = "";
            var result = romcenterPlugin.GetSignature(bigRomFile, fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(2500000, size);
            Assert.StartsWith("Too big", comment);
            Assert.Equal(fileCrc, result);
        }

        /// <summary>
        /// ZipCrc not sent (unzipped rom for example). It should be calculated.
        /// </summary>
        [Fact]
        public void EmptyZipCrcTest()
        {
            const string? fileCrc = "";
            var result = romcenterPlugin.GetSignature($"{DataPath}(AA8DC2D8) unknown.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(128 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("aa8dc2d8", result);
        }

        /// <summary>
        /// ZipCrc not sent (unzipped rom for example). It should be calculated.
        /// </summary>
        [Fact]
        public void NullZipCrcTest()
        {
            const string? fileCrc = null;
            var result = romcenterPlugin.GetSignature($"{DataPath}(AA8DC2D8) unknown.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(128 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("aa8dc2d8", result);

        }
    }
}