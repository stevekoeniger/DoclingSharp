namespace DoclingSharp.ViewModels
{
    /// <summary>
    /// Represents the output of a Docling conversion request.
    /// </summary>
    /// <param name="Markdown">The markdown found by docling.</param>
    /// <param name="Text">The text found by docling.</param>
    /// <param name="Json">The Json found by docling.</param>
    /// <param name="Html">The html found by docling.</param>
    /// <param name="Metadata">The <see cref="DocMeta"/> from the parsing.</param>
    public record DoclingResult(string? Markdown, string? Text, string? Json, string? Html, DocMeta Metadata);
}
