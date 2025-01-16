using System.Text;
using System.Net.Sockets;

namespace MCTG
{
    public static class HttpUtils
    {
        public static async Task SendResponseAsync(NetworkStream stream, string content, string contentType = "application/json")
        {
            var response = new StringBuilder();
            response.AppendLine("HTTP/1.1 200 OK");
            response.AppendLine($"Content-Type: {contentType}");
            response.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(content)}");
            response.AppendLine();
            response.Append(content);

            byte[] responseBytes = Encoding.UTF8.GetBytes(response.ToString());
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.FlushAsync();
        }

        public static async Task SendErrorResponseAsync(NetworkStream stream, int statusCode, string message)
        {
            var statusText = statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unknown Error"
            };

            var response = new StringBuilder();
            response.AppendLine($"HTTP/1.1 {statusCode} {statusText}");
            response.AppendLine("Content-Type: application/json");
            response.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(message)}");
            response.AppendLine();
            response.Append(message);

            byte[] responseBytes = Encoding.UTF8.GetBytes(response.ToString());
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.FlushAsync();
        }

        public static string ParseRequestBody(string[] requestLines, int headerEndIndex)
        {
            if (headerEndIndex >= requestLines.Length - 1)
            {
                return string.Empty;
            }

            return string.Join("\r\n", requestLines.Skip(headerEndIndex + 1));
        }
        
    }
}