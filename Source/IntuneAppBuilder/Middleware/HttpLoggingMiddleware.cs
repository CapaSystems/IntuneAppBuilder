using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace IntuneAppBuilder.Middleware
{
    /// <summary>
    /// HTTP message handler to log requests and responses from Microsoft Graph SDK
    /// </summary>
    public class HttpLoggingHandler : DelegatingHandler
    {
        private readonly ILogger logger;

        public HttpLoggingHandler(ILogger logger)
        {
            this.logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Log the outgoing request
            await LogRequestAsync(request);

            var startTime = DateTime.UtcNow;
            
            // Execute the request
            var response = await base.SendAsync(request, cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;

            // Log the response
            await LogResponseAsync(request, response, duration);

            return response;
        }

        private async Task LogRequestAsync(HttpRequestMessage request)
        {
            try
            {
                var url = request.RequestUri?.ToString() ?? "unknown-url";
                var method = request.Method?.Method ?? "unknown-method";
                
                logger.LogInformation($"HTTP Request: {method} {url}");
                
                LogHeaders("Request Headers:", request.Headers);

                // Log request body if present
                if (request.Content != null)
                {
                    await LogContent("Request Body:", request.Content);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error logging HTTP request");
            }
        }

        private async Task LogResponseAsync(HttpRequestMessage request, HttpResponseMessage response, TimeSpan duration)
        {
            try
            {
                var url = request.RequestUri?.ToString() ?? "unknown-url";
                var method = request.Method?.Method ?? "unknown-method";
                
                logger.LogInformation($"HTTP Response: {method} {url} -> {(int)response.StatusCode} {response.StatusCode} ({duration.TotalMilliseconds:F0}ms)");
                
                LogHeaders("Response Headers:", response.Headers);
                
                if (response.Content != null)
                {
                    LogHeaders("Content Headers:", response.Content.Headers);
                    await LogContent("Response Body:", response.Content);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error logging HTTP response");
            }
        }

        private void LogHeaders(string title, System.Net.Http.Headers.HttpHeaders headers)
        {
            if (headers == null) return;

            var headersLog = new StringBuilder();
            foreach (var header in headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    headersLog.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }
            
            if (headersLog.Length > 0)
            {
                logger.LogInformation($"{title}\n{headersLog}");
            }
        }

        private async Task LogContent(string title, HttpContent content)
        {
            try
            {
                var contentString = await content.ReadAsStringAsync();
                
                if (!string.IsNullOrEmpty(contentString))
                {
                    // Check if content is JSON and pretty-print it
                    var processedContent = FormatContentIfJson(contentString, content.Headers?.ContentType?.MediaType);
                    
                    // Limit content size to prevent log overflow
                    if (processedContent.Length > 10000)
                    {
                        processedContent = processedContent.Substring(0, 10000) + "... (truncated)";
                    }
                    logger.LogInformation($"{title} {processedContent}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Could not read content for {title}: {ex.Message}");
            }
        }

        private static string FormatContentIfJson(string content, string contentType)
        {
            // Check if the content type indicates JSON
            if (string.IsNullOrEmpty(contentType) || 
                (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
                 !contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase)))
            {
                return content;
            }

            try
            {
                // Try to parse and pretty-print JSON
                using var document = JsonDocument.Parse(content);
                var formatted = JsonSerializer.Serialize(document, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                // Check if this JSON contains a manifest field and decode it
                var manifestDecoded = TryDecodeManifestFromJson(document);
                if (!string.IsNullOrEmpty(manifestDecoded))
                {
                    formatted += $"\n\n--- Decoded Manifest XML ---\n{manifestDecoded}";
                }

                return formatted;
            }
            catch
            {
                // If JSON parsing fails, return original content
                return content;
            }
        }

        private static string TryDecodeManifestFromJson(JsonDocument document)
        {
            try
            {
                // Check if the root object has a "manifest" property
                if (document.RootElement.TryGetProperty("manifest", out var manifestElement) && 
                    manifestElement.ValueKind == JsonValueKind.String)
                {
                    var manifestBase64 = manifestElement.GetString();
                    if (!string.IsNullOrEmpty(manifestBase64))
                    {
                        // Decode Base64 to XML string
                        var xmlBytes = Convert.FromBase64String(manifestBase64);
                        var xmlString = Encoding.UTF8.GetString(xmlBytes);
                        
                        // Pretty-print the XML
                        return FormatXml(xmlString);
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error decoding manifest: {ex.Message}";
            }

            return null;
        }

        private static string FormatXml(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch
            {
                // If XML parsing fails, return the original string
                return xml;
            }
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            var lowerName = headerName?.ToLowerInvariant();
            return lowerName switch
            {
                "authorization" => true,
                "x-ms-token-aad-access-token" => true,
                "cookie" => true,
                "set-cookie" => true,
                _ => false
            };
        }
    }
}
