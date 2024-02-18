
using PluginLib;
using PluginTest.TestBase;

namespace PluginTest.A7800
{
    public class A7800Tests
    {
        /// <summary>
        /// Base path for test files
        /// </summary>
        private const string DataPath = @"a7800\data\";

        private const string DllType = "romcenter signature calculator"; //Identification string, do not change
        private const string InterfaceVersion = "4.2"; //version of romcenter plugin internal interface

        private const string PlugInName = "Atari 7800 crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support
        private const string Description = "Atari 7800 crc calculator. Skip the Atari 7800 file header to calculate the crc32. Support a78, bin format.";

        private readonly ManagedPlugin romcenterPlugin;

        public A7800Tests()
        {
            var pluginPath = "A7800.dll";
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
        public void GetSignatureRawTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}[22CA4444] Color Grid.bin", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(".bin", result.Extension);
            Assert.Equal("RAW", result.Format);
            Assert.Equal(4 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal(fileCrc, result.Signature);
        }

        [Fact]
        public void GetSignatureHeaderTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}[22CA4444] Color Grid.a78", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal(".a78", result.Extension);
            Assert.Equal("A78", result.Format);
            Assert.Equal(4 * 1024, result.Size);
            Assert.Equal("", result.Comment);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("22ca4444", result.Signature);
        }

        /// <summary>
        /// Rom size should be a multiple of 1024
        /// </summary>
        [Fact]
        public void GetSignatureNotARomTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}not a rom.bin", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.Extension);
            Assert.Equal(7079, result.Size);
            Assert.Equal("Not an atari 7800 rom (invalid size)", result.Comment);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal(fileCrc, result.Signature);
        }

        /// <summary>
        /// ZipCrc not sent (unzipped rom for example). It should be calculated.
        /// </summary>
        [Fact]
        public void NullZipCrcTest()
        {
            const string? fileCrc = null;
            var fs = new FileStream($"{DataPath}not a rom.bin", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.Extension);
            Assert.Equal(7079, result.Size);
            Assert.Equal("Not an atari 7800 rom (invalid size)", result.Comment);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("3bcbd64d", result.Signature);
        }

        /// <summary>
        /// File rom size should match rom size in header
        /// </summary>
        [Fact]
        public void GetSignatureIncorrectSizeTest()
        {
            const string? fileCrc = "11111111";
            var fs = new FileStream($"{DataPath}bad size.a78", FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal(".a78", result.Extension);
            Assert.Equal(4 * 1024, result.Size);
            Assert.StartsWith("Rom size stored in header", result.Comment);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("22ca4444", result.Signature);
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
            File.Copy($"{DataPath}[22CA4444] Color Grid.a78", tempRom);
            const string? fileCrc = "11111111";
            var fs = new FileStream(tempRom, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal(".a78", result.Extension);
            Assert.Equal("22ca4444", result.Signature);

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
            Helper.CreateDummyFile(romFile, 2500000);

            const string? fileCrc = "11111111";
            var fs = new FileStream(romFile, FileMode.Open, FileAccess.Read);
            var result = romcenterPlugin.GetSignature(fs, fileCrc);
            Assert.NotNull(result);
            Assert.Equal("", result.ErrorMessage);
            Assert.Equal("", result.Extension);
            Assert.Equal("", result.Format);
            Assert.Equal(2500000, result.Size);
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
            Assert.Equal(7079, result.Size);
            Assert.Equal("Not an atari 7800 rom (invalid size)", result.Comment);
            Assert.Equal("3bcbd64d", result.Signature);
        }
    }
}