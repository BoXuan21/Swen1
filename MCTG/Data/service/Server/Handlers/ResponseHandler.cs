using System.Text;

namespace MCTG
{
    public partial class TcpServer
    {
        public async Task SendResponseAsync(Stream stream, string status, string content)
        {
            string response = $"{status}\r\n" +
                              "Content-Type: application/json\r\n" +
                              "Access-Control-Allow-Origin: *\r\n" +
                              $"Content-Length: {Encoding.UTF8.GetByteCount(content)}\r\n" +
                              "\r\n" +
                              content;

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.FlushAsync();
        }
    }
}