using DoclingSharp.ViewModels;
using Microsoft.Extensions.Options;

namespace DoclingSharp
{
    /// <summary>
    /// The base docling class.
    /// </summary>
    public abstract class DoclingSharp
    {
        /// <summary>
        /// The maximum characters to chunk a document into.
        /// </summary>
        internal readonly int ChunkMaxCharacters;

        /// <summary>
        /// The number of characters the chunk will overlap.
        /// </summary>
        internal readonly int ChunkCharacterOverlap;

        /// <summary>
        /// The URL for the docling endpoint.
        /// </summary>
        internal readonly HttpClient DoclingHttp;

        /// <summary>
        /// Constructor for the Docling class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for sending the web request.</param>
        /// <param name="options">The <see cref="DoclingOptions"/> that contain the settings.</param>
        protected DoclingSharp(IHttpClientFactory httpClientFactory, IOptions<DoclingOptions> options)
        {
            var opts = options.Value;

            DoclingHttp = httpClientFactory.CreateClient("Docling");
            DoclingHttp.BaseAddress = opts.DoclingAddress;

            ChunkMaxCharacters = opts.ChunkMaxCharacters;
            ChunkCharacterOverlap = opts.ChunkCharacterOverlap;
        }
    }
}
