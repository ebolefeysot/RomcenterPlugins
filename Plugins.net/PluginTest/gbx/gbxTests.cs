using PluginTest.TestBase;

namespace PluginTest.gbx
{
    public class GbxTests
    {
        /// <summary>
        /// Base path for test files
        /// </summary>
        private const string DataPath = @"gbx\data\";

        private const string DllType = "romcenter signature calculator"; //Identification string, do not change
        private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

        private const string PlugInName = "Gameboy color crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support
        private const string Description = "Gameboy crc calculator. Skip the Gameboy file header to calculate the crc32. Support .gb, .gbc and .sgb format.";

        private readonly ManagedPlugin romcenterPlugin;

        public GbxTests()
        {
            var pluginPath = "gbx.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        /// <summary>
        /// Test plugin info methods
        /// </summary>
        [Fact]
        public void GetInfoTest()
        {
            Assert.Equal(DllType, romcenterPlugin.GetDllType());
            Assert.Equal(PlugInName, romcenterPlugin.GetPlugInName());
            Assert.Equal(Version, romcenterPlugin.GetVersion());
            Assert.Equal(Author, romcenterPlugin.GetAuthor());
            Assert.Equal(Description, romcenterPlugin.GetDescription());
            Assert.Equal(InterfaceVersion, romcenterPlugin.GetDllInterfaceVersion());
            Assert.Equal(Email, romcenterPlugin.GetEmail());
            Assert.Equal(WebPage, romcenterPlugin.GetWebPage());
        }

        [Fact]
        public void GetSignatureGbTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(5009215F) Tennis (World)_gb.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".gb", result.Extension);
            Assert.Equal("Gameboy", result.Format);
            Assert.Equal(1 * 32768, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureGbcTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(1766e558) 3-D Ultra Pinball - Thrillride (USA)_gbc.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".gbc", result.Extension);
            Assert.Equal("Gameboy color", result.Format);
            Assert.Equal(16 * 32768, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureSgbTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(69989152) Tetris DX (World)_sgb.rom", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".sgb", result.Extension);
            Assert.Equal("Super Gameboy", result.Format);
            Assert.Equal(16 * 32768, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureGbBiosTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(59C8598E) gb_bios.bin", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(256, result.Size);
            Assert.StartsWith("Rom is too small", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureGbcBiosTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(41884e46) gbc_bios.bin", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(2304, result.Size);
            Assert.StartsWith("Rom is too small", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        /// <summary>
        /// Rom size should be a multiple of 1024
        /// </summary>
        [Fact]
        public void GetSignatureNotARomTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}not a rom.gbc", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(38584, result.Size);
            Assert.StartsWith("Not a gameboy rom", result.Comment);
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
            var romFile = $"{DataPath}not a rom.gbc";
            var fs = new FileStream(romFile, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(38584, result.Size);
            Assert.Equal("Not a gameboy rom (missing header)", result.Comment);
            Assert.Equal("fa8c60fd", result.Signature);
        }

        /// <summary>
        /// File rom size should match rom size in header
        /// </summary>
        [Fact]
        public void GetSignatureIncorrectSizeTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}(d923d90d) Bad size.gb", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".gb", result.Extension);
            Assert.Equal(32768, result.Size);
            Assert.StartsWith("Rom size stored in header", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        ///// <summary>
        ///// Check if rom file is released after operations
        ///// </summary>
        //[Fact]
        //public void FileLockedTest()
        //{
        //    //copy file
        //    var tempRom = $"{DataPath}rom.bin";
        //    if (File.Exists(tempRom))
        //    {
        //        File.Delete(tempRom);
        //    }
        //    File.Copy($"{DataPath}(5009215F) Tennis (World)_gb.rom", tempRom);
        //    const string? fileCrc = "fc051004";
        //    romcenterPlugin.GetSignature(tempRom, fileCrc, out _, out _, out _, out _);

        //    //delete file
        //    File.Delete(tempRom);
        //}

        //[Fact]
        //public void ExceptionTest()
        //{
        //    const string? fileCrc = "d923d90d";
        //    var result = romcenterPlugin.GetSignature($"{DataPath}filenotfound.gb", fileCrc, out var format, out var size, out var comment, out var errorMessage);
        //    Assert.NotEqual("", errorMessage);
        //    Assert.Equal("", format.ToLowerInvariant());
        //    Assert.Equal(0, size); //header size give 65536
        //    Assert.StartsWith("", comment.ToLowerInvariant());
        //    Assert.Equal("", result);
        //}
    }
}