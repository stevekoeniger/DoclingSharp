# DoclingSharp

DoclingSharp is a .NET 8 client library for interacting with [Docling](https://github.com/your-docling-repo) (version >1.0.1). It provides two core features:

- **DoclingClient** – Extracts content from documents uploaded as `IFormFile` objects via the Docling API.  
- **DocumentChunker** – for splitting document content into manageable chunks suitable for embeddings or other processing.

---

## Installation

Install via NuGet:

```bash
dotnet add package DoclingSharp
```

## Usage
In your `Program.cs` or `Startup.cs`:

```csharp
builder.Services.AddDocling(c =>
{
    c.DoclingAddress = new Uri("http://localhost:5001");
    c.ChunkMaxCharacters = 2048;
    c.ChunkCharacterOverlap = 128;
});
```
This sets up both `DoclingClient` and `DocumentChunker` with your preferred configuration.

### Extract and chunk documents
In a controller, service, or background task:
```csharp
// Extract content from an uploaded IFormFile
var result = await _doclingClient.ExtractDocumentContentAsync(file);

// Split the content into chunks for embedding or further processing
var documentChunks = _documentChunker.ChunkDocument(result.Markdown);

// Pass the chunks to your embedding engine
```
### Features
- ExtractDocumentContentAsync: Works with IFormFile inputs for PDFs, DOCX, and other supported formats. Returns structured DoclingResult including Markdown, plain text, JSON, and HTML.
- DocumentChunker: Splits documents into overlapping chunks to ensure no content is lost between segments, making it ideal for embedding workflows.
- Fully integrated with IHttpClientFactory and IOptions<T> for clean dependency injection.
- Configurable chunk sizes and overlap.

### Example
```csharp
var options = new DoclingOptions
{
    DoclingAddress = new Uri("http://localhost:5001"),
    ChunkMaxCharacters = 2048,
    ChunkCharacterOverlap = 128
};

var httpClientFactory = ...; // get via DI or manually
var doclingClient = new DoclingClient(httpClientFactory, Options.Create(options));
var documentChunker = new DocumentChunker(options);

var file = File.OpenRead("example.pdf");
var result = await doclingClient.ExtractDocumentContentAsync(file);
var chunks = documentChunker.ChunkDocument(result.Markdown);
```

### Requirements
- .NET 8+
- Docling API >1.0.1
- Microsoft.Extensions.Http and Microsoft.Extensions.Options
