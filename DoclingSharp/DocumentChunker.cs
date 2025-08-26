using DoclingSharp.ViewModels;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DoclingSharp
{
    /// <summary>
    /// The document chunking class.
    /// </summary>
    public class DocumentChunker : DoclingSharp
    {
        /// <summary>
        /// An array of whitespace values.
        /// </summary>
        private static readonly bool[] _isWhitespace = CreateWhitespaceLookup();

        /// <summary>
        /// Checks to see if a character is whitespace.
        /// </summary>
        /// <param name="c">The character to check for.</param>
        /// <returns>True if the character is whitespace; False if the character is not whitespace.</returns>
        private static bool IsWhiteSpace(char c) => _isWhitespace[c];

        /// <summary>
        /// Constructor for the document chunker.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use for requests to docling.</param>
        /// <param name="options">The <see cref="DoclingOptions"/> that has the settings.</param>
        public DocumentChunker(IHttpClientFactory httpClientFactory, IOptions<DoclingOptions> options)
            : base(httpClientFactory, options) { }

        /// <summary>
        /// Chunk a document
        /// </summary>
        /// <param name="text">The text to chunk.</param>
        /// <returns></returns>
        public IReadOnlyList<TextChunk> ChunkDocument(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<TextChunk>();

            int len = text.Length;
            int estChunks = (len / ChunkMaxCharacters) + 2;
            var result = new List<TextChunk>(estChunks);

            var chars = text.AsSpan();
            int i = 0;

            while (i < len)
            {
                int end = i + ChunkMaxCharacters;
                if (end > len) end = len;

                // --- SIMD newline search (backward) ---
                int cut = end;
                int searchLen = end - i;
                int vecSize = Vector<ushort>.Count;
                var nlVec = new Vector<ushort>('\n');

                for (int j = searchLen - vecSize; j >= 0; j -= vecSize)
                {
                    var ushortSpan = MemoryMarshal.Cast<char, ushort>(chars.Slice(i + j, vecSize));
                    var slice = new Vector<ushort>(ushortSpan);
                    var eq = Vector.Equals(slice, nlVec);

                    if (!eq.Equals(Vector<ushort>.Zero))
                    {
                        // scan backwards in this block to find *last* newline
                        for (int k = vecSize - 1; k >= 0; k--)
                        {
                            if (chars[i + j + k] == '\n')
                            {
                                cut = i + j + k;
                                j = -1; // break outer loop
                                break;
                            }
                        }
                    }
                }

                // --- SIMD TrimStart ---
                int startTrim = i;
                {
                    int remaining = cut - startTrim;
                    while (remaining >= vecSize)
                    {
                        var ushortSpan = MemoryMarshal.Cast<char, ushort>(chars.Slice(startTrim, vecSize));
                        var vec = new Vector<ushort>(ushortSpan);

                        if (!AllWhitespace(vec))
                        {
                            for (int k = 0; k < vecSize; k++)
                            {
                                if (!IsWhiteSpace((char)ushortSpan[k]))
                                {
                                    startTrim += k;
                                    remaining = 0; // done
                                    break;
                                }
                            }
                            break;
                        }

                        startTrim += vecSize;
                        remaining -= vecSize;
                    }

                    while (startTrim < cut && IsWhiteSpace(chars[startTrim]))
                        startTrim++;
                }

                // --- SIMD TrimEnd ---
                int endTrim = cut - 1;
                {
                    int remaining = endTrim - startTrim + 1;
                    while (remaining >= vecSize)
                    {
                        var ushortSpan = MemoryMarshal.Cast<char, ushort>(chars.Slice(endTrim - vecSize + 1, vecSize));
                        var vec = new Vector<ushort>(ushortSpan);

                        if (!AllWhitespace(vec))
                        {
                            for (int k = vecSize - 1; k >= 0; k--)
                            {
                                if (!IsWhiteSpace((char)ushortSpan[k]))
                                {
                                    endTrim = endTrim - (vecSize - 1 - k);
                                    remaining = 0; // done
                                    break;
                                }
                            }
                            break;
                        }

                        endTrim -= vecSize;
                        remaining -= vecSize;
                    }

                    while (endTrim >= startTrim && IsWhiteSpace(chars[endTrim]))
                        endTrim--;
                }

                // --- Add chunk if valid ---
                if (startTrim <= endTrim)
                {
                    int sliceLen = endTrim - startTrim + 1;
                    result.Add(new TextChunk(text.Substring(startTrim, sliceLen), startTrim, cut));
                }

                // advance with overlap
                i = cut + (ChunkMaxCharacters - ChunkCharacterOverlap);
                if (i > len) i = len;
            }

            return result;
        }

        /// <summary>
        /// Create the whitespace lookup table.
        /// </summary>
        /// <returns>An array of whitespace lookup bool values.</returns>
        private static bool[] CreateWhitespaceLookup()
        {
            var table = new bool[65536]; // all possible UTF-16 chars
            for (int c = 0; c < table.Length; c++)
                table[c] = char.IsWhiteSpace((char)c);
            return table;
        }

        /// <summary>
        /// Check if a whole SIMD block is whitespace
        /// </summary>
        /// <param name="vec">The vector for the whitespace.</param>
        /// <returns></returns>
        private static bool AllWhitespace(Vector<ushort> vec)
        {
            for (int k = 0; k < Vector<ushort>.Count; k++)
            {
                if (!IsWhiteSpace((char)vec[k]))
                    return false;
            }
            return true;
        }
    }
}
