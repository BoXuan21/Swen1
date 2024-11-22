using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace MCTG
{
    public class HttpController
    {
        private readonly int _port;
        private readonly Dictionary<string, User> _users; // Dictionary to store User objects

        public HttpController(int port)
        {
            _port = port;
            _users = new Dictionary<string, User>(); // Initialize the dictionary with User objects
        }

        public void Start()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            Console.WriteLine($"Starting HTTP server on port {_port}...");
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected!");

                // Handle each client connection in a separate thread
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;

            try
            {
                using (var stream = client.GetStream())
                {
                    // Read the incoming request
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine("Request received:");
                    Console.WriteLine(request);

                    // Parse the HTTP request
                    var lines = request.Split("\r\n");
                    if (lines.Length < 1) throw new Exception("Invalid request format");

                    string[] requestLineParts = lines[0].Split(' ');
                    string method = requestLineParts[0];
                    string endpoint = requestLineParts[1];

                    if (method == "POST" && endpoint == "/sessions")
                    {
                        HandleSessionPost(lines, stream);
                    }
                    else if (method == "POST" && endpoint == "/users")
                    {
                        HandleUserPost(lines, stream);
                    }
                    else
                    {
                        SendResponse(stream, "404 Not Found", "Endpoint not found");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        // Handle user registration (POST /users)
        private void HandleUserPost(string[] requestLines, NetworkStream stream)
        {
            string body = string.Join("\r\n", requestLines).Split("\r\n\r\n")[1];
            Console.WriteLine("Request body:");
            Console.WriteLine(body);

            try
            {
                // Deserialize directly into a User object
                var user = JsonSerializer.Deserialize<User>(body);

                // Validate payload
                if (string.IsNullOrEmpty(user?.Username) || string.IsNullOrEmpty(user?.Password))
                {
                    SendResponse(stream, "400 Bad Request", "Invalid request payload");
                    return;
                }

                // Check if user already exists
                if (_users.ContainsKey(user.Username))
                {
                    SendResponse(stream, "400 Bad Request", "User already exists");
                    return;
                }

                // Register the user
                _users.Add(user.Username, user);
                SendResponse(stream, "201 Created", "User created successfully");
            }
            catch (Exception)
            {
                SendResponse(stream, "400 Bad Request", "Invalid JSON payload");
            }
        }

        // Handle user login (POST /sessions)
        private void HandleSessionPost(string[] requestLines, NetworkStream stream)
        {
            string body = string.Join("\r\n", requestLines).Split("\r\n\r\n")[1];
            Console.WriteLine("Request body:");
            Console.WriteLine(body);

            try
            {
                // Deserialize directly into a User object
                var user = JsonSerializer.Deserialize<User>(body);

                // Validate payload
                if (string.IsNullOrEmpty(user?.Username) || string.IsNullOrEmpty(user?.Password))
                {
                    SendResponse(stream, "400 Bad Request", "Invalid request payload");
                    return;
                }

                // Check if the user exists and credentials are correct
                if (_users.ContainsKey(user.Username) && _users[user.Username].Password == user.Password)
                {
                    // Generate token
                    string token = $"{user.Username}-mtcgToken"; // Example token format

                    // Return the token as the response
                    SendResponse(stream, "200 OK", token);
                }
                else
                {
                    SendResponse(stream, "401 Unauthorized", "Invalid credentials");
                }
            }
            catch (Exception)
            {
                SendResponse(stream, "400 Bad Request", "Invalid JSON payload");
            }
        }

        private void SendResponse(NetworkStream stream, string status, string body)
        {
            string response = $"HTTP/1.1 {status}\r\n" +
                              "Content-Type: text/plain\r\n" +
                              $"Content-Length: {body.Length}\r\n" +
                              "\r\n" +
                              body;

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}
