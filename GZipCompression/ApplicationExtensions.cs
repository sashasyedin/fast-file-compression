using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace GZipCompression
{
    /// <summary>
    /// Application extensions.
    /// </summary>
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Dequeues the safe.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="objectLock">The object lock.</param>
        /// <returns>A result of the operation.</returns>
        public static ChunkObject DequeueSafe(this Queue<ChunkObject> queue, object objectLock)
        {
            while (true)
            {
                lock (objectLock)
                {
                    if (queue.Count > 0)
                    {
                        return queue.Dequeue();
                    }
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Thread-safe enqueue method.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="objectLock">The object lock.</param>
        /// <param name="chunkObject">The chunk object.</param>
        public static void EnqueueSafe(this Queue<ChunkObject> queue, object objectLock, ChunkObject chunkObject)
        {
            lock (objectLock)
            {
                queue.Enqueue(chunkObject);
            }
        }

        /// <summary>
        /// Transforms the length to byte array.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>A result of the operation.</returns>
        public static byte[] TransformLengthToBytes(this int length)
        {
            var lengthToStore = IPAddress.HostToNetworkOrder(length);
            var lengthInBytes = BitConverter.GetBytes(lengthToStore);
            var base64String = Convert.ToBase64String(lengthInBytes);
            return Encoding.ASCII.GetBytes(base64String);
        }

        /// <summary>
        /// Transforms the byte array to length.
        /// </summary>
        /// <param name="intToParse">The int to parse.</param>
        /// <returns>A result of the operation.</returns>
        public static int TransformBytesToLength(this byte[] intToParse)
        {
            var base64String = Encoding.ASCII.GetString(intToParse);
            var lengthInBytes = Convert.FromBase64String(base64String);
            var length = BitConverter.ToInt32(lengthInBytes, 0);
            return IPAddress.NetworkToHostOrder(length);
        }
    }
}