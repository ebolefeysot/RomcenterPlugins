using PluginTest.TestBase;

namespace PluginTest.SegaGenesis
{
    public class SegaTests
    {
        private const string DataPath = @"SegaGenesis\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public SegaTests()
        {
            var pluginPath = "genesis.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        [Fact]
        public void GetSignatureBinTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(F9394E97) Sonic_bin.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gen", format.ToLowerInvariant());
            Assert.Equal(512 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal(fileCrc.ToLowerInvariant(), result); //zip crc should be used
        }

        [Fact]
        public void GetSignatureMdTest()
        {
            const string? fileCrc = "b2CCC6CA";
            var result = romcenterPlugin.GetSignature($"{DataPath}(F9394E97) Sonic_md.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gen", format.ToLowerInvariant());
            Assert.Equal(512 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("f9394e97", result);
        }

        [Fact]
        public void GetSignatureSmdTest()
        {
            const string? fileCrc = "9e514C6e";
            var result = romcenterPlugin.GetSignature($"{DataPath}(F9394E97) Sonic_smd.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".smd", format.ToLowerInvariant());
            Assert.Equal(512 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("f9394e97", result);
        }

        [Fact]
        public void GetSignature32xTest()
        {
            const string? fileCrc = "ab6e378d";
            var result = romcenterPlugin.GetSignature($"{DataPath}(53734E3A) Doom 32x_32x.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".32x", format.ToLowerInvariant());
            Assert.Equal(3 * 1024 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("53734e3a", result);
        }

        [Fact]
        public void GetSignature32xBinTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(53734E3A) Doom 32x_bin.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gen", format.ToLowerInvariant());
            Assert.Equal(3 * 1024 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal(fileCrc, result); //zip crc should be used
        }

        [Fact]
        public void GetSignatureBiosTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(3F888CF4) BIOS Genesis_bin.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gen", format.ToLowerInvariant());
            Assert.Equal(2 * 1024, size);
            Assert.StartsWith("", comment);
            Assert.Equal(fileCrc, result); //zip crc should be used
        }

        [Fact]
        public void GetSignatureBios32xTest()
        {
            const string? fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(5C12EAE8) BIOS 32X.md", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(256, size);
            Assert.StartsWith("Too small for a game", comment);
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