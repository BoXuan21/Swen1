using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MCTG
{
    public class TcpServer
    {
        private readonly TcpListener _listener;
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly JwtMiddleware _jwtMiddleware;

        public TcpServer(int port, IUserRepository userRepository, JwtService jwtService)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _userRepository = userRepository;
            _jwtService = jwtService;
            _jwtMiddleware = new JwtMiddleware(null, jwtService);
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
            using NetworkStream stream = client.GetStream();
    
            try
            {
                // Read request
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
                Console.WriteLine("Received request:");
                Console.WriteLine(request); // Log the raw request

                // Parse the HTTP request
                string[] requestLines = request.Split("\r\n");
                string[] requestLine = requestLines[0].Split(' ');
                string method = requestLine[0];
                string path = requestLine[1];

                Console.WriteLine($"Method: {method}, Path: {path}"); // Log parsed method and path

                // Get request body
                string body = "";
                bool readingBody = false;
                foreach (string line in requestLines)
                {
                    if (readingBody)
                    {
                        body += line;
                    }
                    else if (string.IsNullOrEmpty(line))
                    {
                        readingBody = true;
                    }
                }

                Console.WriteLine($"Body: {body}"); // Log the parsed body

                // Create an HttpContext for the current request
                var context = new DefaultHttpContext();
                context.Request.Method = method;
                context.Request.Path = path;
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

                // Invoke the JWT middleware
                await _jwtMiddleware.Invoke(context);

                // Check if the request is authorized
                if (context.Response.StatusCode == 401)
                {
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Unauthorized");
                    return;
                }

                // Handle different endpoints
                switch (method + " " + path)
                {
                    case "POST /users":
                        await HandleRegistrationAsync(stream, body);
                        break;
                    case "POST /sessions":
                        await HandleLoginAsync(stream, body);
                        break;
                    default:
                        await SendResponseAsync(stream, "HTTP/1.1 404 Not Found", "Unknown endpoint");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}"); // Log any errors
                await SendResponseAsync(stream, "HTTP/1.1 500 Internal Server Error", ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        private async Task HandleRegistrationAsync(NetworkStream stream, string body)
        {
            try
            {
                Console.WriteLine($"Attempting to deserialize: {body}");
        
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Make JSON property matching case-insensitive
                };
        
                var user = JsonSerializer.Deserialize<User>(body, options);
        
                if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                {
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Invalid user data");
                    return;
                }

                Console.WriteLine($"Deserialized user: Username={user.Username}");
        
                _userRepository.Add(user);
                await SendResponseAsync(stream, "HTTP/1.1 201 Created", "User created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", $"Registration failed: {ex.Message}");
            }
        }
        
       
        private async Task HandleLoginAsync(NetworkStream stream, string body)
        {
            try
            {
                var credentials = JsonSerializer.Deserialize<User>(body);
                if (_userRepository.ValidateCredentials(credentials.Username, credentials.Password))
                {
                    string token = _jwtService.GenerateToken(credentials.Username);
                    await SendResponseAsync(stream, "HTTP/1.1 200 OK", token);
                }
                else
                {
                    await SendResponseAsync(stream, "HTTP/1.1 401 Unauthorized", "Invalid credentials");
                }
            }
            catch (Exception)
            {
                await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request", "Login failed");
            }
        }

        private async Task SendResponseAsync(NetworkStream stream, string status, string content)
        {
            string response = $"{status}\r\n" +
                            "Content-Type: application/json\r\n" +
                            $"Content-Length: {content.Length}\r\n" +
                            "\r\n" +
                            content;

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }
}