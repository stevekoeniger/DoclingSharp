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
        /// Break a string into chunks.
        /// </summary>
        /// <param name="text">The string to break into chunks.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> of <see cref="TextChunk"/>.</returns>
        public IReadOnlyList<TextChunk> ChunkDocument(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<TextChunk>();

            int len = text.Length;
            int estChunks = (len / Math.Max(1, ChunkMaxCharacters - ChunkCharacterOverlap)) + 2;
            var result = new List<TextChunk>(estChunks);

            var chars = text.AsSpan();
            int i = 0;

            while (i < len)
            {
                // Calculate the maximum end position for this chunk
                int maxEnd = Math.Min(i + ChunkMaxCharacters, len);
                int cut = maxEnd;

                // If we're not at the end of the string, try to find a better breaking point
                if (maxEnd < len)
                {
                    // Look for newline within the chunk (backward from maxEnd)
                    cut = FindLastNewline(chars, i, maxEnd);

                    // If no newline found, use maxEnd
                    if (cut == -1)
                        cut = maxEnd;
                }

                // SIMD-optimized trimming
                var (startTrim, endTrim) = TrimWhitespace(chars, i, cut);

                // Add chunk if valid
                if (startTrim <= endTrim)
                {
                    int sliceLen = endTrim - startTrim + 1;
                    result.Add(new TextChunk(text.Substring(startTrim, sliceLen), startTrim, endTrim + 1));
                }

                // Advance with overlap: next chunk starts at (current_end + 1 - overlap)
                i = cut + 1 - ChunkCharacterOverlap;
                if (i <= startTrim) // Prevent infinite loops
                    i = startTrim + 1;

                if (i >= len) break;
            }

            return result;
        }

        /// <summary>
        /// Finds the last newline character within the specified range using SIMD optimization.
        /// Searches backward from maxEnd to start for the rightmost '\n' character.
        /// </summary>
        /// <param name="chars">The character span to search within</param>
        /// <param name="start">The starting position (inclusive) to search from</param>
        /// <param name="maxEnd">The ending position (exclusive) to search to</param>
        /// <returns>The index of the last newline character found, or -1 if none found</returns>
        private static int FindLastNewline(ReadOnlySpan<char> chars, int start, int maxEnd)
        {
            int vecSize = Vector<ushort>.Count;
            var nlVec = new Vector<ushort>('\n');
            int searchStart = start;
            int searchLen = maxEnd - start;

            // SIMD search for newlines
            for (int j = searchLen - vecSize; j >= 0; j -= vecSize)
            {
                if (searchStart + j + vecSize > chars.Length) continue;

                var ushortSpan = MemoryMarshal.Cast<char, ushort>(chars.Slice(searchStart + j, vecSize));
                var slice = new Vector<ushort>(ushortSpan);
                var eq = Vector.Equals(slice, nlVec);

                if (!eq.Equals(Vector<ushort>.Zero))
                {
                    // Found newlines in this vector, find the last one
                    for (int k = vecSize - 1; k >= 0; k--)
                    {
                        if (chars[searchStart + j + k] == '\n')
                        {
                            return searchStart + j + k;
                        }
                    }
                }
            }

            // Fallback: scalar search for remaining chars
            for (int j = (searchLen % vecSize) - 1; j >= 0; j--)
            {
                if (chars[searchStart + j] == '\n')
                    return searchStart + j;
            }

            return -1; // No newline found
        }

        /// <summary>
        /// Trims whitespace from both ends of the specified character range using SIMD optimization.
        /// Uses vectorized operations to quickly skip over whitespace characters at the beginning
        /// and end of the range, falling back to scalar operations for cleanup.
        /// </summary>
        /// <param name="chars">The character span to trim</param>
        /// <param name="start">The starting position (inclusive) of the range to trim</param>
        /// <param name="end">The ending position (exclusive) of the range to trim</param>
        /// <returns>A tuple containing the trimmed start position (inclusive) and end position (exclusive)</returns>
        private static (int start, int end) TrimWhitespace(ReadOnlySpan<char> chars, int start, int end)
        {
            int vecSize = Vector<ushort>.Count;

            // SIMD TrimStart
            int startTrim = start;
            int remaining = end - startTrim;

            while (remaining >= vecSize)
            {
                var ushortSpan = MemoryMarshal.Cast<char, ushort>(chars.Slice(startTrim, vecSize));
                var vec = new Vector<ushort>(ushortSpan);

                if (!AllWhitespace(vec))
                {
                    // Find first non-whitespace in this vector
                    for (int k = 0; k < vecSize; k++)
                    {
                        if (!IsWhiteSpace((char)ushortSpan[k]))
                        {
                            startTrim += k;
                            remaining = 0;
                            break;
                        }
                    }
                    break;
                }

                startTrim += vecSize;
                remaining -= vecSize;
            }

            // Scalar cleanup for TrimStart
            while (startTrim < end && IsWhiteSpace(chars[startTrim]))
                startTrim++;

            // SIMD TrimEnd
            int endTrim = end - 1;
            remaining = endTrim - startTrim + 1;

            while (remaining >= vecSize && endTrim >= startTrim + vecSize - 1)
            {
                var ushortSpan = MemoryMarshal.Cast<char, ushort>(chars.Slice(endTrim - vecSize + 1, vecSize));
                var vec = new Vector<ushort>(ushortSpan);

                if (!AllWhitespace(vec))
                {
                    // Find last non-whitespace in this vector
                    for (int k = vecSize - 1; k >= 0; k--)
                    {
                        if (!IsWhiteSpace((char)ushortSpan[k]))
                        {
                            endTrim = endTrim - (vecSize - 1 - k);
                            remaining = 0;
                            break;
                        }
                    }
                    break;
                }

                endTrim -= vecSize;
                remaining -= vecSize;
            }

            // Scalar cleanup for TrimEnd
            while (endTrim >= startTrim && IsWhiteSpace(chars[endTrim]))
                endTrim--;

            return (startTrim, endTrim);
        }

        /// <summary>
        /// Create the whitespace lookup table.
        /// </summary>
        /// <returns>An array of whitespace lookup bool values.</returns>
        private static bool[] CreateWhitespaceLookup()
        {
            var table = new bool[65536]; // all UTF-16 chars
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