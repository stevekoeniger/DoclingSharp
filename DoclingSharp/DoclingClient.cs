using DoclingSharp.ExtensionMethods;
using DoclingSharp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DoclingSharp
{
    /// <summary>
    /// A strongly typed wrapper around the Docling Serve API.
    /// </summary>
    public class DoclingClient : DoclingSharp
    {
        /// <summary>
        /// JSON serializer options.
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> configured with BaseAddress pointing to Docling Serve.</param>
        public DoclingClient(IHttpClientFactory httpClientFactory, IOptions<DoclingOptions> options)
            : base(httpClientFactory, options) { }

        /// <summary>
        /// Converts a document using Docling by uploading it directly.
        /// Falls back to base64 data URL if direct upload is not supported by the server.
        /// </summary>
        /// <param name="file">The uploaded file as <see cref="IFormFile"/>.</param>
        /// <returns>A <see cref="DoclingResult"/> containing extracted text, markdown, JSON, and metadata.</returns>
        public async Task<DoclingResult> ExtractDocumentContentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File cannot be null or empty.", nameof(file));

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0; // reset for reading

            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(ms);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
            form.Add(fileContent, "files", file.FileName);

            // Attempt direct upload
            using var upload = await DoclingHttp.PostAsync("/v1/convert/file", form).ConfigureAwait(false);
            if (upload.IsSuccessStatusCode)
            {
                var json = await upload.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseDoclingResponse(json);
            }

            // Fallback: data URL
            ms.Position = 0;
            var b64 = Convert.ToBase64String(ms.ToArray());
            var dataUrl = $"data:{file.ContentType ?? "application/octet-stream"};base64,{b64}";
            var payload = JsonSerializer.Serialize(new
            {
                sources = new[] { new { kind = "http", url = dataUrl } }
            }, _jsonOptions);

            using var resp = await DoclingHttp.PostAsync("/v1/convert/source", new StringContent(payload, Encoding.UTF8, "application/json")).ConfigureAwait(false);
            var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return ParseDoclingResponse(content);
        }

        /// <summary>
        /// Parse the docling response.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static DoclingResult ParseDoclingResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? md = root.GetPropertyOrNull("md_content")?.GetString() ?? root.FindFirstString("md_content");
            string? txt = root.GetPropertyOrNull("text_content")?.GetString() ?? root.FindFirstString("text_content");
            string? html = root.GetPropertyOrNull("html_content")?.GetString() ?? root.FindFirstString("html_content");
            string? rawJson = root.GetPropertyOrNull("json_content")?.GetRawText();

            var meta = new DocMeta(
                root.FindFirstString("title"),
                root.FindFirstInt("pages") ?? 0,
                root.FindFirstInt("processing_time") ?? 0);

            return new DoclingResult(md, txt, rawJson, html, meta);
        }
    }
}
