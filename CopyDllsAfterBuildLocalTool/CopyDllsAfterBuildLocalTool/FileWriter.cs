using System;
using System.Buffers;
using System.IO;

namespace CopyDllsAfterBuildLocalTool
{
    public class FileWriter
    {
        private const int BufferSliceSize = 1024;

        /// <summary>
        /// Binary Comparer. Binary check files.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool Compare(ReadOnlySpan<byte> source, Stream target)
        {
            ReadOnlySpan<byte> sourceBuffer = source;
            Span<byte> targetBuffer = stackalloc byte[BufferSliceSize];
            while (true)
            {
                var targetRead = target.Read(targetBuffer);

                // reach to end
                if (sourceBuffer.Length == 0)
                    return targetRead == 0;
                // target was empty or shorter then source
                if (targetRead == 0)
                    return false;

                // read source buffer for sliced size.
                var sourceBufferSlice = sourceBuffer.Length < BufferSliceSize ? sourceBuffer : sourceBuffer[..BufferSliceSize];
                // binary equal check for source[0..N] == target[0..N]
                if (!sourceBufferSlice.SequenceEqual(targetBuffer[..targetRead]))
                    return false;

                // next slice...
                sourceBuffer = sourceBuffer[targetRead..];
            }
        }

        /// <summary>
        /// Write binary to path.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <param name="option"></param>
        /// <returns>true when write, false when skipped</returns>
        public static bool Write(ReadOnlySpan<byte> source, string path, WriteCheckOption option = WriteCheckOption.None)
        {
            // if exisiting
            if (File.Exists(path))
            {
                // do nothing for binary equal file.
                if (option == WriteCheckOption.BinaryEquality)
                {
                    using var destination = File.OpenRead(path);
                    if (Compare(source, destination))
                        return false;
                }
            }

            // Write or Overwrite
            var folder = Path.GetDirectoryName(path);
            if (folder == null)
                throw new ArgumentNullException($"Destination path {path} is root path, you must set full filepath.");
            if (Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            using var file = File.Open(path, FileMode.Create, FileAccess.Write);
            file.Write(source);
            return true;
        }
    }

    /// <summary>
    /// Option to control write precheck.
    /// </summary>
    public enum WriteCheckOption
    {
        /// <summary>
        /// No equality check, write to destination everytime.
        /// </summary>
        None = 0,
        /// <summary>
        /// Binary queality check, write to destination when binary mismatch.
        /// </summary>
        BinaryEquality,
    }
}
