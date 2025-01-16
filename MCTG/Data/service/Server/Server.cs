using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
        private readonly IBattleRepository _battleRepository;
        private readonly ITradeRepository _tradeRepository;
        private readonly IUserStatsRepository _userStatsRepository;
        private readonly string _connectionString;

        public TcpServer(int port, IUserRepository userRepository, ICardRepository cardRepository, 
            IJwtService jwtService, IPackageRepository packageRepository, IBattleRepository battleRepository,
            ITradeRepository tradeRepository, IUserStatsRepository userStatsRepository,
            string connectionString)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _userRepository = userRepository;
            _cardRepository = cardRepository;
            _jwtService = jwtService;
            _jwtMiddleware = new JwtMiddleware(jwtService);
            _packageRepository = packageRepository;
            _battleRepository = battleRepository;
            _tradeRepository = tradeRepository;
            _userStatsRepository = userStatsRepository;
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

        public async void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            using NetworkStream networkStream = client.GetStream();
    
            try
            {
                byte[] buffer = new byte[4096];
                using var ms = new MemoryStream();
        
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    await HttpUtils.SendErrorResponseAsync(networkStream, 400, "Invalid request format");
                    return;
                }

                string initialRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                var contentLengthMatch = System.Text.RegularExpressions.Regex.Match(initialRequest, @"Content-Length: (\d+)");
                int contentLength = 0;
                if (contentLengthMatch.Success)
                {
                    contentLength = int.Parse(contentLengthMatch.Groups[1].Value);
                }

                ms.Write(buffer, 0, bytesRead);

                int headerEndPosition = initialRequest.IndexOf("\r\n\r\n");
                if (headerEndPosition > 0 && contentLength > 0)
                {
                    int remainingBytes = contentLength - (bytesRead - headerEndPosition - 4);
                    while (remainingBytes > 0)
                    {
                        bytesRead = await networkStream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remainingBytes));
                        if (bytesRead == 0) break;
                        ms.Write(buffer, 0, bytesRead);
                        remainingBytes -= bytesRead;
                    }
                }

                ms.Position = 0;
                string fullRequest = Encoding.UTF8.GetString(ms.ToArray());
                string[] requestLines = fullRequest.Split("\r\n");

                if (requestLines.Length < 1)
                {
                    await HttpUtils.SendErrorResponseAsync(networkStream, 400, "Invalid request format");
                    return;
                }

                string[] requestLine = requestLines[0].Split(' ');
                if (requestLine.Length < 2)
                {
                    await HttpUtils.SendErrorResponseAsync(networkStream, 400, "Invalid request format");
                    return;
                }

                var context = new CustomHttpContext();
                context.Request.Method = requestLine[0];
                context.Request.Path = requestLine[1];

                // Find where headers end and body begins
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

                // Parse body
                if (i < requestLines.Length - 1)
                {
                    context.Request.Body = HttpUtils.ParseRequestBody(requestLines, i);
                }

                Console.WriteLine($"Method: {context.Request.Method}, Path: {context.Request.Path}");
                Console.WriteLine($"Body: {context.Request.Body}");

                // Skip auth for registration and login
                bool requiresAuth = !(context.Request.Method == "POST" && 
                    (context.Request.Path == "/users" || context.Request.Path == "/sessions"));

                if (requiresAuth && !_jwtMiddleware.Invoke(context))
                {
                    await HttpUtils.SendResponseAsync(networkStream, context.Response.Body);
                    return;
                }

                await HandleRequestAsync(context);
                await HttpUtils.SendResponseAsync(networkStream, context.Response.Body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex}");
                await HttpUtils.SendErrorResponseAsync(networkStream, 500, "An error occurred processing your request");
            }
            finally
            {
                client.Close();
            }
        }

        private async Task HandleRequestAsync(CustomHttpContext context)
        {
            try
            {
                string path = context.Request.Path.Split('?')[0];
                string method = context.Request.Method;

                switch (method + " " + path)
                {
                    case "POST /users":
                        await HandleRegistrationAsync(context);
                        break;
                    case "POST /sessions":
                        await HandleLoginAsync(context);
                        break;
                    case "POST /packages":
                        await HandleCreatePackageAsync(context);
                        break;
                    case "POST /transactions/packages":
                        await HandleBuyPackageAsync(context);
                        break;
                    case "GET /cards":
                        await HandleGetCardsAsync(context);
                        break;
                    case "GET /deck":
                        await HandleGetDeckAsync(context);
                        break;
                    case "PUT /deck":
                        await HandleConfigureDeckAsync(context);
                        break;
                    case "POST /battles":
                        await HandleBattleAsync(context);
                        break;
                    case "GET /stats":
                        await HandleGetStatsAsync(context);
                        break;
                    case "GET /scoreboard":
                        await HandleGetScoreboardAsync(context);
                        break;
                    case "GET /tradings":
                        await HandleGetTradingsAsync(context);
                        break;
                    case "POST /tradings":
                        await HandleCreateTradeAsync(context);
                        break;
                    default:
                        await HandleSpecialEndpoints(context, method, path);
                        break;
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Internal Server Error";
                context.Response.Body = $"An error occurred: {ex.Message}";
            }
        }

        private async Task HandleSpecialEndpoints(CustomHttpContext context, string method, string path)
        {
            if (path.StartsWith("/tradings/"))
            {
                string tradeId = path.Split('/')[2];
                switch (method)
                {
                    case "POST":
                        await HandleExecuteTradeAsync(context);
                        break;
                    case "DELETE":
                        await HandleDeleteTradeAsync(context);
                        break;
                    default:
                        context.Response.StatusCode = 405;
                        context.Response.StatusDescription = "Method Not Allowed";
                        context.Response.Body = "Method not allowed for trading endpoint";
                        break;
                }
            }
            else if (path.StartsWith("/users/"))
            {
                switch (method)
                {
                    case "GET":
                        await HandleGetProfileAsync(context);
                        break;
                    case "PUT":
                        await HandleUpdateProfileAsync(context);
                        break;
                    default:
                        context.Response.StatusCode = 405;
                        context.Response.StatusDescription = "Method Not Allowed";
                        context.Response.Body = "Method not allowed for user endpoint";
                        break;
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.StatusDescription = "Not Found";
                context.Response.Body = "Endpoint not found";
            }
        }
    }
}