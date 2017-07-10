namespace GZipCompression
{
    /// <summary>
    /// Indicates an Operation Mode.
    /// </summary>
    public enum OperationMode
    {
        /// <summary>
        /// No operation mode set.
        /// </summary>
        None = 0,

        /// <summary>
        /// The compress mode.
        /// </summary>
        CompressMode = 1,

        /// <summary>
        /// The decompress mode.
        /// </summary>
        DecompressMode = 2,
    }
}