using System.IO;
using System.IO.Compression;
using CuttingEdge.Conditions;

namespace GZipCompression
{
    /// <summary>
    /// Represents a base compressor.
    /// </summary>
    public abstract class BaseCompressor
    {
        /// <summary>
        /// Compresses a single file into a gz file.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        public virtual void Compress(string sourcePath, string targetPath)
        {
            #region Check all preconditions

            Condition.Requires(sourcePath, nameof(sourcePath)).IsNotNullOrWhiteSpace();
            Condition.Requires(targetPath, nameof(targetPath)).IsNotNullOrWhiteSpace();
            Condition.WithExceptionOnFailure<FileNotFoundException>().Requires(File.Exists(sourcePath)).IsTrue();

            #endregion Check all preconditions

            using (var inFile = File.OpenRead(sourcePath))
            using (var outFile = File.Create(targetPath))
            using (var compress = new GZipStream(outFile, CompressionMode.Compress, false))
            {
                var buffer = new byte[inFile.Length];
                var read = inFile.Read(buffer, 0, buffer.Length);

                while (read > 0)
                {
                    compress.Write(buffer, 0, read);
                    read = inFile.Read(buffer, 0, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Decompresses the specified file.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        public virtual void Decompress(string sourcePath, string targetPath)
        {
            #region Check all preconditions

            Condition.Requires(sourcePath, nameof(sourcePath)).IsNotNullOrWhiteSpace();
            Condition.Requires(targetPath, nameof(targetPath)).IsNotNullOrWhiteSpace();
            Condition.WithExceptionOnFailure<FileNotFoundException>().Requires(File.Exists(sourcePath)).IsTrue();
            Condition.WithExceptionOnFailure<FileFormatException>().Requires(Path.GetExtension(sourcePath)).IsEqualTo(ApplicationConstants.GzipExtension);

            #endregion Check all preconditions

            using (var inStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var outStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zipStream = new GZipStream(inStream, CompressionMode.Decompress, true))
            {
                var buffer = new byte[inStream.Length];

                while (true)
                {
                    var count = zipStream.Read(buffer, 0, buffer.Length);

                    if (count != 0)
                    {
                        outStream.Write(buffer, 0, count);
                    }

                    if (count != buffer.Length)
                    {
                        break;
                    }
                }
            }
        }
    }
}