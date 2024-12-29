using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;

namespace MCTG
{
    public class TcpServer
    {
        private readonly TcpListener _listener;
        private readonly IUserRepository _userRepository;

        public TcpServer(int port, IUserRepository userRepository)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _userRepository = userRepository;
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

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            using NetworkStream stream = client.GetStream();
    
            try
            {
                // Read request
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
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

                // Handle different endpoints
                switch (method + " " + path)
                {
                    case "POST /users":
                        HandleRegistration(stream, body);
                        break;
                    case "POST /sessions":
                        HandleLogin(stream, body);
                        break;
                    default:
                        SendResponse(stream, "HTTP/1.1 404 Not Found", "Unknown endpoint");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}"); // Log any errors
                SendResponse(stream, "HTTP/1.1 500 Internal Server Error", ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        private void HandleRegistration(NetworkStream stream, string body)
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
                    SendResponse(stream, "HTTP/1.1 400 Bad Request", "Invalid user data");
                    return;
                }

                Console.WriteLine($"Deserialized user: Username={user.Username}");
        
                _userRepository.Add(user);
                SendResponse(stream, "HTTP/1.1 201 Created", "User created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                SendResponse(stream, "HTTP/1.1 400 Bad Request", $"Registration failed: {ex.Message}");
            }
        }
        
        private void HandleLogin(NetworkStream stream, string body)
        {
            try
            {
                var credentials = JsonSerializer.Deserialize<User>(body);
                if (_userRepository.ValidateCredentials(credentials.Username, credentials.Password))
                {
                    string token = _userRepository.GenerateToken(credentials.Username);
                    SendResponse(stream, "HTTP/1.1 200 OK", token);
                }
                else
                {
                    SendResponse(stream, "HTTP/1.1 401 Unauthorized", "Invalid credentials");
                }
            }
            catch (Exception)
            {
                SendResponse(stream, "HTTP/1.1 400 Bad Request", "Login failed");
            }
        }

        private void SendResponse(NetworkStream stream, string status, string content)
        {
            string response = $"{status}\r\n" +
                            "Content-Type: application/json\r\n" +
                            $"Content-Length: {content.Length}\r\n" +
                            "\r\n" +
                            content;

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}