using CopyDllsAfterBuildLocalTool;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CopyDllsAfterBuildLocalToolUnitTest
{
    public class FileWriterTest : IDisposable
    {
        private readonly string _path;

        // setup
        public FileWriterTest()
        {
            _path = Path.Combine(Path.GetTempPath(), "FileWriterTest", Guid.NewGuid().ToString());

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);
        }

        // teardown
        public void Dispose()
        {
            if (Directory.Exists(_path))
                Directory.Delete(_path, true);
        }

        [Fact]
        public void BinaryCompareTest()
        {
            var random = new Random();
            var content = Enumerable.Range(0, 10).Select(x => (byte)random.Next(0, 254)).ToArray();
            var mismatchContent = content.Append((byte)random.Next(0, 254)).ToArray();

            // binary match item should be true
            var a = Path.Combine(_path, "a");
            File.WriteAllBytes(a, content);
            using var fa = File.OpenRead(a);
            Assert.True(FileWriter.Compare(content, fa));

            // binary miss-match item should be false
            var b = Path.Combine(_path, "b");
            File.WriteAllBytes(b, mismatchContent);
            using var fb = File.OpenRead(b);
            Assert.False(FileWriter.Compare(content, fb));
        }

        [Fact]
        public void Write_WriteCheckOption_BinaryEquality_Test()
        {
            var option = WriteCheckOption.BinaryEquality;

            // Destination not exists should write.
            var random = new Random();
            var content = Enumerable.Range(0, 10).Select(x => (byte)random.Next(0, 254)).ToArray();
            var a = Path.Combine(_path, "a");
            Assert.True(FileWriter.Write(content, a, option));

            // Binary match file should skipped.
            Assert.False(FileWriter.Write(content, a, option));

            // Binary mismatch file should write.
            var mismatchContent = content.Append((byte)random.Next(0, 254)).ToArray();
            Assert.True(FileWriter.Write(mismatchContent, a, option));
        }

        [Fact]
        public void Write_WriteCheckOption_None_Test()
        {
            var option = WriteCheckOption.None;

            // Destination not exists should write.
            var random = new Random();
            var content = Enumerable.Range(0, 10).Select(x => (byte)random.Next(0, 254)).ToArray();
            var a = Path.Combine(_path, "a");
            Assert.True(FileWriter.Write(content, a, option));

            // Binary match file should write.
            Assert.True(FileWriter.Write(content, a, option));

            // Binary mismatch file should write.
            var mismatchContent = content.Append((byte)random.Next(0, 254)).ToArray();
            Assert.True(FileWriter.Write(mismatchContent, a, option));
        }
    }
}
