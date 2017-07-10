using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using CuttingEdge.Conditions;

namespace GZipCompression
{
    /// <summary>
    /// Represent a GZip compressor.
    /// </summary>
    /// <seealso cref="GZipCompression.BaseCompressor" />
    public class Compressor : BaseCompressor
    {
        private static readonly object PrimaryQueueLock = new object();
        private static readonly object SecondaryQueueLock = new object();

        private readonly int _maxThreads;
        private readonly Queue<ChunkObject> _primaryQueue;
        private readonly Queue<ChunkObject> _secondaryQueue;

        private int _primaryQueueChunksComplete;
        private int _secondaryQueueChunksComplete;

        public Compressor()
            : this(Environment.ProcessorCount)
        {
        }

        public Compressor(int maxThreads)
        {
            this._maxThreads = maxThreads;
            this._primaryQueue = new Queue<ChunkObject>(maxThreads);
            this._secondaryQueue = new Queue<ChunkObject>(maxThreads);
        }

        /// <summary>
        /// Occurs when exception is thrown in separate thread.
        /// </summary>
        public event Action<Exception> HandleThreadExceptionEvent;

        /// <summary>
        /// Compresses a single file into a .gz file.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        public override void Compress(string sourcePath, string targetPath)
        {
            #region Check all preconditions

            Condition.Requires(sourcePath, nameof(sourcePath)).IsNotNullOrWhiteSpace();
            Condition.Requires(targetPath, nameof(targetPath)).IsNotNullOrWhiteSpace();
            Condition.WithExceptionOnFailure<FileNotFoundException>().Requires(File.Exists(sourcePath)).IsTrue();

            #endregion Check all preconditions

            if (_maxThreads == 1)
            {
                base.Compress(sourcePath, targetPath);
                return;
            }

            _primaryQueueChunksComplete = default(int);
            _secondaryQueueChunksComplete = default(int);

            var readThread = new Thread(() => ReadProcedure(OperationMode.CompressMode, sourcePath));
            var writeThread = new Thread(() => WriteProcedure(OperationMode.CompressMode, targetPath));
            var compressThread = new Thread(CompressProcedure) { IsBackground = true };

            readThread.Start();
            writeThread.Start();
            compressThread.Start();

            readThread.Join();
            writeThread.Join();
        }

        /// <summary>
        /// Decompresses the specified file.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        public override void Decompress(string sourcePath, string targetPath)
        {
            #region Check all preconditions

            Condition.Requires(sourcePath, nameof(sourcePath)).IsNotNullOrWhiteSpace();
            Condition.Requires(targetPath, nameof(targetPath)).IsNotNullOrWhiteSpace();
            Condition.WithExceptionOnFailure<FileNotFoundException>().Requires(File.Exists(sourcePath)).IsTrue();
            Condition.WithExceptionOnFailure<FileFormatException>().Requires(Path.GetExtension(sourcePath)).IsEqualTo(ApplicationConstants.GzipExtension);

            #endregion Check all preconditions

            if (_maxThreads == 1)
            {
                base.Decompress(sourcePath, targetPath);
                return;
            }

            _primaryQueueChunksComplete = default(int);
            _secondaryQueueChunksComplete = default(int);

            var readThread = new Thread(() => ReadProcedure(OperationMode.DecompressMode, sourcePath));
            var writeThread = new Thread(() => WriteProcedure(OperationMode.DecompressMode, targetPath));
            var decompressThread = new Thread(DecompressProcedure) { IsBackground = true };

            readThread.Start();
            writeThread.Start();
            decompressThread.Start();

            readThread.Join();
            writeThread.Join();
        }

        #region Private Methods

        /// <summary>
        /// Compresses temporal data.
        /// </summary>
        private void CompressProcedure()
        {
            try
            {
                while (true)
                {
                    var chunkObject = this._primaryQueue.DequeueSafe(PrimaryQueueLock);

                    if (chunkObject.Data == null)
                    {
                        break;
                    }

                    while (chunkObject.ChunkNumber != this._primaryQueueChunksComplete)
                    {
                        Thread.Sleep(10);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var compress = new GZipStream(memoryStream, CompressionMode.Compress, true))
                        {
                            compress.Write(chunkObject.Data, 0, chunkObject.Data.Length);
                        }

                        this._secondaryQueue.EnqueueSafe(SecondaryQueueLock, new ChunkObject(chunkObject.ChunkNumber, memoryStream.ToArray()));
                        this._primaryQueueChunksComplete++;
                    }
                }
            }
            catch (Exception exception)
            {
                this.FireHandleThreadExceptionEvent(exception);
            }
            finally
            {
                this._secondaryQueue.EnqueueSafe(SecondaryQueueLock, ChunkObject.NullObject);
            }
        }

        /// <summary>
        /// Decompresses temporal data.
        /// </summary>
        private void DecompressProcedure()
        {
            try
            {
                while (true)
                {
                    var chunkObject = this._primaryQueue.DequeueSafe(PrimaryQueueLock);

                    if (chunkObject.Data == null)
                    {
                        break;
                    }

                    while (chunkObject.ChunkNumber != this._primaryQueueChunksComplete)
                    {
                        Thread.Sleep(10);
                    }

                    using (var memoryStream = new MemoryStream(chunkObject.Data))
                    {
                        var bytesRead = default(int);
                        var buffer = new byte[ApplicationConstants.ChunkSize];

                        using (var decompress = new GZipStream(memoryStream, CompressionMode.Decompress, true))
                        {
                            bytesRead = decompress.Read(buffer, 0, buffer.Length);
                        }

                        Array.Resize(ref buffer, bytesRead);
                        this._secondaryQueue.EnqueueSafe(SecondaryQueueLock, new ChunkObject(chunkObject.ChunkNumber, buffer));
                        this._primaryQueueChunksComplete++;
                    }
                }
            }
            catch (Exception exception)
            {
                this.FireHandleThreadExceptionEvent(exception);
            }
            finally
            {
                this._secondaryQueue.EnqueueSafe(SecondaryQueueLock, ChunkObject.NullObject);
            }
        }

        /// <summary>
        /// Fires the handle thread exception event.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void FireHandleThreadExceptionEvent(Exception exception)
        {
            this.HandleThreadExceptionEvent?.Invoke(exception);
        }

        /// <summary>
        /// A procedure reading to the file.
        /// </summary>
        /// <param name="threadContext">The thread context.</param>
        private void ReadProcedure(OperationMode mode, string path)
        {
            try
            {
                using (var inFile = File.OpenRead(path))
                {
                    var counter = default(int);
                    var dataPortionSize = default(int);

                    while (inFile.Position < inFile.Length)
                    {
                        var buffer = null as byte[];

                        if (mode == OperationMode.CompressMode)
                        {
                            if (inFile.Length - inFile.Position <= ApplicationConstants.ChunkSize)
                            {
                                dataPortionSize = Convert.ToInt32(inFile.Length - inFile.Position);
                            }
                            else
                            {
                                dataPortionSize = ApplicationConstants.ChunkSize;
                            }

                            buffer = new byte[dataPortionSize];
                            inFile.Read(buffer, 0, dataPortionSize);
                        }
                        else if (mode == OperationMode.DecompressMode)
                        {
                            var buffToReadLength = new byte[8];
                            var readLength = inFile.Read(buffToReadLength, 0, 8);
                            var lengthToRead = buffToReadLength.TransformBytesToLength();

                            buffer = new byte[lengthToRead];
                            inFile.Read(buffer, 0, lengthToRead);
                        }

                        while (_primaryQueue.Count == _maxThreads)
                        {
                            Thread.Sleep(10);
                        }

                        this._primaryQueue.EnqueueSafe(PrimaryQueueLock, new ChunkObject(counter++, buffer));
                    }
                }
            }
            catch (Exception exception)
            {
                this.FireHandleThreadExceptionEvent(exception);
            }
            finally
            {
                this._primaryQueue.EnqueueSafe(PrimaryQueueLock, ChunkObject.NullObject);
            }
        }

        /// <summary>
        /// Writes to the file.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="path">The path.</param>
        private void WriteProcedure(OperationMode mode, string path)
        {
            try
            {
                using (var outputFile = File.Create(path))
                {
                    while (true)
                    {
                        var chunkObject = this._secondaryQueue.DequeueSafe(SecondaryQueueLock);

                        if (chunkObject.Data == null)
                        {
                            return;
                        }

                        while (chunkObject.ChunkNumber != this._secondaryQueueChunksComplete)
                        {
                            Thread.Sleep(10);
                        }

                        if (mode == OperationMode.CompressMode)
                        {
                            var lengthToStore = chunkObject.Data.Length.TransformLengthToBytes();

                            outputFile.Write(lengthToStore, 0, lengthToStore.Length);
                            outputFile.Write(chunkObject.Data, 0, chunkObject.Data.Length);
                        }
                        else if (mode == OperationMode.DecompressMode)
                        {
                            outputFile.Write(chunkObject.Data, 0, chunkObject.Data.Length);
                        }

                        this._secondaryQueueChunksComplete++;
                    }
                }
            }
            catch (Exception exception)
            {
                this.FireHandleThreadExceptionEvent(exception);
            }
        }

        #endregion Private Methods
    }
}