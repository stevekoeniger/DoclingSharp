namespace DoclingSharp.ViewModels
{
    /// <summary>
    /// Metadata about the converted document.
    /// </summary>
    /// <param name="Title">The title of the document.</param>
    /// <param name="Pages">The total number of pages that were processed.</param>
    /// <param name="ProcessingTime">The processing time in seconds.</param>
    public record DocMeta(string? Title, int Pages, decimal ProcessingTime);
}
