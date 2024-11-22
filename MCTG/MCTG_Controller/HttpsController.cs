using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class HttpController
{
    private readonly int _port;

    public HttpController(int port)
    {
        _port = port;
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

                // Process the POST request
                if (request.StartsWith("POST"))
                {
                    string[] parts = request.Split(new[] { "\r\n\r\n" }, StringSplitOptions.None);
                    string body = parts.Length > 1 ? parts[1] : string.Empty;

                    Console.WriteLine("POST Body:");
                    Console.WriteLine(body);

                    // Send an HTTP response
                    string response = "HTTP/1.1 200 OK\r\n" +
                                      "Content-Type: text/plain\r\n" +
                                      "Content-Length: 13\r\n" +
                                      "\r\n" +
                                      "Test Working";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
                else
                {
                    Console.WriteLine("Unsupported request.");
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
}
