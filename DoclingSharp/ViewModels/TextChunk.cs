namespace DoclingSharp.ViewModels
{
    /// <summary>
    /// The text chunks.
    /// </summary>
    /// <param name="Text">The text that is in the chunk.</param>
    /// <param name="StartIndex">The start index for the text chunk.</param>
    /// <param name="EndIndex">The end index for the text chunk.</param>
    public record TextChunk(string Text, int StartIndex, int EndIndex);
}
