using CopyDllsAfterBuildLocalTool;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CopyDllsAfterBuildLocalToolUnitTest
{
    public class FileCheckerTest : IDisposable
    {
        private readonly string _path;

        // setup
        public FileCheckerTest()
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
            Assert.True(FileChecker.Compare(content, fa));

            // binary miss-match item should be false
            var b = Path.Combine(_path, "b");
            File.WriteAllBytes(b, mismatchContent);
            using var fb = File.OpenRead(b);
            Assert.False(FileChecker.Compare(content, fb));
        }

        [Fact]
        public void Exists_BinaryCheck_Test()
        {
            var option = WriteCheckOption.BinaryEquality;

            // Destination not exists should write.
            var random = new Random();
            var content = Enumerable.Range(0, 10).Select(x => (byte)random.Next(0, 254)).ToArray();
            var a = Path.Combine(_path, "a");
            Assert.False(FileChecker.Exists(content, a, option));
            Write(content, a);
            Assert.True(FileChecker.Exists(content, a, option));

            // Binary match file should skipped.
            Write(content, a);
            Assert.True(FileChecker.Exists(content, a, option));

            // Binary mismatch file should write.
            var mismatchContent = content.Append((byte)random.Next(0, 254)).ToArray();
            Assert.False(FileChecker.Exists(mismatchContent, a, option));
            Write(mismatchContent, a);
            Assert.True(FileChecker.Exists(mismatchContent, a, option));
        }

        [Fact]
        public void Exists_NoBinaryCheck_Test()
        {
            var option = WriteCheckOption.None;

            // Destination not exists should write.
            var random = new Random();
            var content = Enumerable.Range(0, 10).Select(x => (byte)random.Next(0, 254)).ToArray();
            var a = Path.Combine(_path, "a");
            Assert.False(FileChecker.Exists(content, a, option));
            Write(content, a);
            Assert.True(FileChecker.Exists(content, a, option));

            // Binary match file should re-write.
            Write(content, a);
            Assert.True(FileChecker.Exists(content, a, option));

            // Binary mismatch file should re-write.
            var mismatchContent = content.Append((byte)random.Next(0, 254)).ToArray();
            Assert.True(FileChecker.Exists(mismatchContent, a, option));
            Write(mismatchContent, a);
            Assert.True(FileChecker.Exists(mismatchContent, a, option));
        }


        /// <summary>
        /// Write binary to path.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <returns>true when write, false when skipped</returns>
        private static void Write(ReadOnlySpan<byte> source, string path)
        {
            // Write or Overwrite
            using var file = File.Open(path, FileMode.Create, FileAccess.Write);
            file.Write(source);
        }
    }
}
