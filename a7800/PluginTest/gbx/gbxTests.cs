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
        private const string InterfaceVersion = "4.0"; //version of romcenter plugin internal interface

        private const string PlugInName = "Gameboy color crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support
        private const string Description = "Gameboy crc calculator. Skip the Gameboy file header to calculate the crc32. Support .gb, .gbc and .sgb format.";

        private readonly IRomcenterPlugin romcenterPlugin;

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
            const string fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(5009215F) Tennis (World)_gb.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gb", format.ToLowerInvariant());
            Assert.Equal(1 * 32768, size);
            Assert.Equal("", comment);
            Assert.Equal("5009215f", result);
        }

        [Fact]
        public void GetSignatureGbcTest()
        {
            const string fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(1766e558) 3-D Ultra Pinball - Thrillride (USA)_gbc.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gbc", format.ToLowerInvariant());
            Assert.Equal(16 * 32768, size);
            Assert.Equal("", comment);
            Assert.Equal("1766e558", result);
        }

        [Fact]
        public void GetSignatureSgbTest()
        {
            const string fileCrc = "11111111";
            var result = romcenterPlugin.GetSignature($"{DataPath}(69989152) Tetris DX (World)_sgb.rom", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".sgb", format.ToLowerInvariant());
            Assert.Equal(16 * 32768, size);
            Assert.Equal("", comment);
            Assert.Equal("69989152", result);
        }

        [Fact]
        public void GetSignatureGbBiosTest()
        {
            const string fileCrc = "59c8598e";
            var result = romcenterPlugin.GetSignature($"{DataPath}(59C8598E) gb_bios.bin", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(256, size);
            Assert.StartsWith("Too small", comment);
            Assert.Equal("59c8598e", result);
        }

        [Fact]
        public void GetSignatureGbcBiosTest()
        {
            const string fileCrc = "41884e46";
            var result = romcenterPlugin.GetSignature($"{DataPath}(41884e46) gbc_bios.bin", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(2304, size);
            Assert.StartsWith("Too small", comment);
            Assert.Equal("41884e46", result);
        }

        /// <summary>
        /// Rom size should be a multiple of 1024
        /// </summary>
        [Fact]
        public void GetSignatureNotARomTest()
        {
            const string fileCrc = "fa8c60fd";
            var result = romcenterPlugin.GetSignature($"{DataPath}not a rom.gbc", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(38584, size);
            Assert.Equal("not an gameboy rom (missing header)", comment.ToLowerInvariant());
            Assert.Equal(fileCrc, result);
        }

        /// <summary>
        /// File rom size should match rom size in header
        /// </summary>
        [Fact]
        public void GetSignatureIncorrectSizeTest()
        {
            const string fileCrc = "d923d90d";
            var result = romcenterPlugin.GetSignature($"{DataPath}(d923d90d) Bad size.gb", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", errorMessage);
            Assert.Equal(".gb", format.ToLowerInvariant());
            Assert.Equal(32768, size); //header size give 65536
            Assert.StartsWith("rom size stored in header", comment.ToLowerInvariant());
            Assert.Equal("d923d90d", result);
        }

        /// <summary>
        /// Check if rom file is released after operations
        /// </summary>
        [Fact]
        public void FileLockedTest()
        {
            //copy file
            var tempRom = $"{DataPath}rom.bin";
            if (File.Exists(tempRom))
            {
                File.Delete(tempRom);
            }
            File.Copy($"{DataPath}(5009215F) Tennis (World)_gb.rom", tempRom);
            const string fileCrc = "fc051004";
            romcenterPlugin.GetSignature(tempRom, fileCrc, out _, out _, out _, out _);

            //delete file
            File.Delete(tempRom);
        }

        /// <summary>
        /// File rom size should match rom size in header
        /// </summary>
        [Fact]
        public void ExceptionTest()
        {
            const string fileCrc = "d923d90d";
            var result = romcenterPlugin.GetSignature($"{DataPath}filenotfound.gb", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.NotEqual("", errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(0, size); //header size give 65536
            Assert.StartsWith("", comment.ToLowerInvariant());
            Assert.Equal("", result);
        }
    }
}