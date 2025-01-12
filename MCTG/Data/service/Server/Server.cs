using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace MCTG
{
    public partial class TcpServer
    {
        private readonly TcpListener _listener;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly JwtMiddleware _jwtMiddleware;
        private readonly IPackageRepository _packageRepository;
        private readonly ICardRepository _cardRepository;
        private readonly string _connectionString;

        public TcpServer(int port, IUserRepository userRepository, IJwtService jwtService, 
            IPackageRepository packageRepository, ICardRepository cardRepository, string connectionString)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _userRepository = userRepository;
            _jwtService = jwtService;
            _jwtMiddleware = new JwtMiddleware(null, jwtService);
            _packageRepository = packageRepository;
            _cardRepository = cardRepository;
            _connectionString = connectionString;
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private async void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            using NetworkStream networkStream = client.GetStream();
    
            try
            {
                byte[] buffer = new byte[4096];
                using var ms = new MemoryStream();
        
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) return;
        
                string initialRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received request:");
                Console.WriteLine(initialRequest);

                var contentLengthMatch = System.Text.RegularExpressions.Regex.Match(initialRequest, @"Content-Length: (\d+)");
                int contentLength = 0;
                if (contentLengthMatch.Success)
                {
                    contentLength = int.Parse(contentLengthMatch.Groups[1].Value);
                }

                ms.Write(buffer, 0, bytesRead);
                int totalBytesRead = bytesRead;

                int headerEndPosition = initialRequest.IndexOf("\r\n\r\n");
                if (headerEndPosition > 0 && contentLength > 0)
                {
                    int remainingBytes = contentLength - (totalBytesRead - headerEndPosition - 4);
                    while (remainingBytes > 0)
                    {
                        bytesRead = await networkStream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remainingBytes));
                        if (bytesRead == 0) break;
                        ms.Write(buffer, 0, bytesRead);
                        remainingBytes -= bytesRead;
                        totalBytesRead += bytesRead;
                    }
                }

                ms.Position = 0;
                string fullRequest = Encoding.UTF8.GetString(ms.ToArray());
                string[] requestLines = fullRequest.Split("\r\n");
        
                string[] requestLine = requestLines[0].Split(' ');
                if (requestLine.Length < 2)
                {
                    await SendResponseAsync(networkStream, "HTTP/1.1 400 Bad Request", "Invalid request format");
                    return;
                }

                string method = requestLine[0];
                string path = requestLine[1];

                Console.WriteLine($"Method: {method}, Path: {path}");

                var context = new DefaultHttpContext();
                context.Request.Method = method;
                context.Request.Path = path;

                int i;
                for (i = 1; i < requestLines.Length; i++)
                {
                    var line = requestLines[i];
                    if (string.IsNullOrEmpty(line)) break;

                    var headerParts = line.Split(": ", 2);
                    if (headerParts.Length == 2)
                    {
                        context.Request.Headers[headerParts[0]] = headerParts[1];
                    }
                }

                string body = "";
                if (i < requestLines.Length - 1)
                {
                    body = string.Join("\r\n", requestLines.Skip(i + 1));
                }
                Console.WriteLine($"Body: {body}");

                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
                await _jwtMiddleware.Invoke(context);

                using var responseStream = new MemoryStream();

                try
                {
                    switch (method + " " + path.Split('?')[0]) 
                    {
                        case "POST /users":
                            await HandleRegistrationAsync(responseStream, body);
                            break;
                        case "POST /sessions":
                            await HandleLoginAsync(responseStream, body);
                            break;
                        case "POST /transactions/packages":
                            await HandleBuyPackageAsync(responseStream, body, context);
                            break;
                        case "POST /packages":
                            await HandleCreatePackageAsync(responseStream, body, context);
                            break;
                        case "GET /cards":
                            await HandleGetCardsAsync(responseStream, context);
                            break;
                        case "GET /deck":
                            await HandleGetDeckAsync(responseStream, context);
                            break;
                        case "PUT /deck":
                            await HandleConfigureDeckAsync(responseStream, body, context);
                            break;
                        case var profilePath when path.StartsWith("/users/") && method == "GET":
                            await HandleGetProfileAsync(responseStream, path.Split('/')[2], context);
                            break;
                        case var profilePath when path.StartsWith("/users/") && method == "PUT":
                            await HandleUpdateProfileAsync(responseStream, path.Split('/')[2], body, context);
                            break;
                        default:
                            await SendResponseAsync(responseStream, "HTTP/1.1 404 Not Found", "Unknown endpoint");
                            break;
                    }

                    responseStream.Position = 0;
                    await responseStream.CopyToAsync(networkStream);
                    await networkStream.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling request: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    await SendResponseAsync(networkStream, "HTTP/1.1 500 Internal Server Error", 
                        $"Internal server error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                try
                {
                    await SendResponseAsync(networkStream, "HTTP/1.1 500 Internal Server Error", 
                        "A fatal error occurred");
                }
                catch
                {
                    // Ignore any errors in sending error response
                }
            }
            finally
            {
                client.Close();
            }
        }
    }
}