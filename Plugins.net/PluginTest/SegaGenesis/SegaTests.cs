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
            var fs = new FileStream($"{DataPath}(F9394E97) Sonic_bin.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".gen", result.Extension);
            Assert.Equal("Genesis", result.Format);
            Assert.Equal(512 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
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

        [Fact]
        public void GetSignatureSmdTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(F9394E97) Sonic_smd.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".smd", result.Extension);
            Assert.Equal("Super Magic Drive", result.Format);
            Assert.Equal(512 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("f9394e97", result.Signature);
        }

        [Fact]
        public void GetSignature32xTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(53734E3A) Doom 32x_32x.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".32x", result.Extension);
            Assert.Equal("32X", result.Format);
            Assert.Equal(3 * 1024 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("53734e3a", result.Signature);
        }

        [Fact]
        public void GetSignature32xBinTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(53734E3A) Doom 32x_bin.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".gen", result.Extension);
            Assert.Equal(3 * 1024 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureBiosTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(3F888CF4) BIOS Genesis_bin.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal(2 * 1024, result.Size);
            Assert.StartsWith("Rom is too small", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureBios32xTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(5C12EAE8) BIOS 32X.md", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal(256, result.Size);
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
            Assert.Equal(128 * 1024, result.Size);
            Assert.Equal("", result.Comment);
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