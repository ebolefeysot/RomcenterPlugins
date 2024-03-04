using PluginLib;
using PluginTest.TestBase;

namespace PluginTest.N64
{
    public class N64Tests
    {
        /// <summary>
        /// Base path for test files
        /// </summary>
        private const string DataPath = @"n64\data\";

        private const string DllType = "romcenter signature calculator"; //Identification string, do not change
        private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

        private const string PlugInName = "Nintendo 64 crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support
        private const string Description = "Nintendo 64 crc calculator. Support n64, z64 and v64.";

        private readonly ManagedPlugin romcenterPlugin;

        public N64Tests()
        {
            var pluginPath = "n64.dll";
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

        [Theory]
        [InlineData("(3CE60709)SM64-n64.bin", ".n64", "", 8 * 1024 * 1024, "3ce60709")]
        [InlineData("(3CE60709)SM64-v64.bin", ".v64", "Doctor V64", 8 * 1024 * 1024, "3ce60709")]
        [InlineData("(3CE60709)SM64-z64.bin", ".z64", "Mr. Backup Z64", 8 * 1024 * 1024, "11111111")]
        [InlineData("not a rom.bin", "", "", 4290768, "11111111", "Not a nintendo 64 rom (no header)")]
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
            File.Copy($"{DataPath}(3CE60709)SM64-V64.bin", tempRom);
            const string? fileCrc = "11111111";
            var fs = new FileStream(tempRom, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal(".v64", result.Extension);
            Assert.Equal("3ce60709", result.Signature);

            fs.Close();
            //delete file
            File.Delete(tempRom);
        }

        /// <summary>
        /// If file is too big to be a rom, return zip crc, even if it is null
        /// </summary>
        [Fact]
        public void GetSignatureBigFileTest()
        {
            var romFile = $"{DataPath}bigRom.bin";
            Helper.CreateDummyFile(romFile, 70000000);

            const string? fileCrc = "11111111";
            var fs = new FileStream(romFile, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(70000000, result.Size);
            Assert.StartsWith("Rom is too big", result.Comment);
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
            var romFile = $"{DataPath}not a rom.bin";
            var fs = new FileStream(romFile, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(4290768, result.Size);
            Assert.Equal("Not a nintendo 64 rom (no header)", result.Comment);
            Assert.Equal("aac52050", result.Signature);
        }
    }
}