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
        private const string InterfaceVersion = "4.0"; //version of romcenter plugin internal interface

        private const string PlugInName = "Atari 7800 crc calculator"; //full name of plug in
        private const string Author = "Eric Bole-Feysot"; //Author name
        private const string Version = "2.0"; //version of the plug in
        private const string WebPage = "www.romcenter.com"; //home page of plug in
        private const string Email = "eric@romcenter.com"; //Email of plug in support
        private const string Description = "Atari 7800 crc calculator. Skip the Atari 7800 file header to calculate the crc32. Support a78, bin format.";

        private readonly IRomcenterPlugin romcenterPlugin;

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
            const string fileCrc = "22ca4444";
            var result = romcenterPlugin.GetSignature($"{DataPath}[22CA4444] Color Grid.bin", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal(".bin", format.ToLowerInvariant());
            Assert.Equal(4 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("", errorMessage);
            Assert.Equal(fileCrc, result);
        }

        [Fact]
        public void GetSignatureHeaderTest()
        {
            const string fileCrc = "FC051004";
            var result = romcenterPlugin.GetSignature($"{DataPath}[22CA4444] Color Grid.a78", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal(".a78", format.ToLowerInvariant());
            Assert.Equal(4 * 1024, size);
            Assert.Equal("", comment);
            Assert.Equal("", errorMessage);
            Assert.Equal("22ca4444", result);
        }

        /// <summary>
        /// Rom size should be a multiple of 1024
        /// </summary>
        [Fact]
        public void GetSignatureNotARomTest()
        {
            const string fileCrc = "3bcbd64d";
            var result = romcenterPlugin.GetSignature($"{DataPath}not a rom.bin", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal("", format.ToLowerInvariant());
            Assert.Equal(7079, size);
            Assert.Equal("not an atari 7800 rom (invalid size)", comment.ToLowerInvariant());
            Assert.Equal("", errorMessage);
            Assert.Equal(fileCrc, result);
        }

        /// <summary>
        /// File rom size should match rom size in header
        /// </summary>
        [Fact]
        public void GetSignatureIncorrectSizeTest()
        {
            const string fileCrc = "22ca4444";
            var result = romcenterPlugin.GetSignature($"{DataPath}bad size.a78", fileCrc, out var format, out var size, out var comment, out var errorMessage);
            Assert.Equal(".a78", format.ToLowerInvariant());
            Assert.Equal(4 * 1024, size);
            Assert.StartsWith("rom size stored in header", comment.ToLowerInvariant());
            Assert.Equal("", errorMessage);
            Assert.Equal(fileCrc, result);
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
            const string fileCrc = "fc051004";
            var result = romcenterPlugin.GetSignature(tempRom, fileCrc, out var format, out _, out _, out _);
            Assert.Equal(".a78", format.ToLowerInvariant());
            Assert.Equal("22ca4444", result);

            //delete file
            File.Delete(tempRom);
        }

    }
}