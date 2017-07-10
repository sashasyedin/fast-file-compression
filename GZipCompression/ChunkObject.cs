namespace GZipCompression
{
    /// <summary>
    /// Represents a chunk object.
    /// </summary>
    public class ChunkObject
    {
        /// <summary>
        /// Gets the null object.
        /// </summary>
        /// <value>
        /// The null object.
        /// </value>
        public static ChunkObject NullObject
        {
            get
            {
                return new ChunkObject(-1, null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkObject"/> class.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="data">The data.</param>
        public ChunkObject(int number, byte[] data)
        {
            this.ChunkNumber = number;
            this.Data = data;
        }

        /// <summary>
        /// Gets the chunk number.
        /// </summary>
        /// <value>
        /// The chunk number.
        /// </value>
        public int ChunkNumber { get; private set; }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public byte[] Data { get; private set; }
    }
}