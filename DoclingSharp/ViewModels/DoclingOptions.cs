namespace DoclingSharp.ViewModels
{
    /// <summary>
    /// The options for the Docling class.
    /// </summary>
    public class DoclingOptions
    {
        /// <summary>
        /// The address for docling.
        /// </summary>
        public Uri DoclingAddress { get; set; } = default!;

        /// <summary>
        /// The maximum characters to chunk a document into.
        /// </summary>
        public int ChunkMaxCharacters { get; set; } = 1024;

        /// <summary>
        /// The number of characters the chunk will overlap.
        /// </summary>
        public int ChunkCharacterOverlap { get; set; } = 128;
    }
}
