using PluginTest.TestBase;

namespace PluginTest.sms
{
    public class SmsTests
    {
        private const string DataPath = @"Sms\data\";
        private readonly ManagedPlugin romcenterPlugin;

        public SmsTests()
        {
            var pluginPath = "sms.dll";
            romcenterPlugin = new ManagedPlugin(pluginPath);
        }

        [Theory]
        [InlineData("(655FB1F4) Bank Panic (Europe)_sms.rom", ".sms", "sms", 32 * 1024, "11111111")]
        [InlineData("(41884e46) unknown.rom", "", "", 2304, "11111111", "Rom is too small")]
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

        [Fact]
        public void AllBiosFiles_ShouldBeDetectedAsSms_WithSmsExtension()
        {
            var biosFolderPath = @"sms\Data\Bios";
            var biosFiles = Directory.GetFiles(biosFolderPath, "*.*", SearchOption.TopDirectoryOnly);

            const string? fileCrc = "";
            foreach (var file in biosFiles)
            {
                using var fs = File.OpenRead(file);
                var result = romcenterPlugin.GetSignature(fs, fileCrc);
                Assert.NotNull(result);
                Assert.Equal("", result.ErrorMessage);
                Assert.Equal(".sms", result.Extension);
                Assert.Equal("sms", result.Format);
                Assert.True(result.Size > 0);
                Assert.True(result.Signature.Length > 0);
            }
        }

        /// <summary>
        /// ZipCrc not sent (unzipped rom for example). It should be calculated.
        /// Rom is 128KB with a crc of 0xAA8DC2D8
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