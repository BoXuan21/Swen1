using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCTG
{
    public class HttpsController
    {
        private readonly UserController _userController;

        // Dependency injection of UserController
        public HttpsController(UserController userController)
        {
            _userController = userController;
        }

        public async Task StartServer()
        {
            HttpListener listener = new HttpListener();

            // Define the URL for the server
            string url = "http://localhost:5002/";
            listener.Prefixes.Add(url);


            try
            {
                listener.Start();
                Console.WriteLine($"HTTP Server started. Listening on {url}");
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("Failed to start server. Ensure SSL certificates are configured.");
                Console.WriteLine($"Error: {ex.Message}");
                return;
            }

            while (true)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    Console.WriteLine($"Request received: {context.Request.HttpMethod} {context.Request.Url}");

                    if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/sessions")
                    {
                        // Handle user login
                        await HandleLoginRequest(context);
                    }
                    else
                    {
                        // Handle unsupported methods
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        string response = "Only POST requests are allowed on this endpoint.";
                        byte[] buffer = Encoding.UTF8.GetBytes(response);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }

                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling request: {ex.Message}");
                }
            }
        }

        private async Task HandleLoginRequest(HttpListenerContext context)
        {
            using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                try
                {
                    // Read and deserialize the request body
                    string requestBody = await reader.ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

                    if (data != null && data.ContainsKey("Username") && data.ContainsKey("Password"))
                    {
                        string username = data["Username"];
                        string password = data["Password"];

                        // Validate user credentials
                        string token = _userController.ValidateUserCredentials(username, password);

                        if (token != null)
                        {
                            // Respond with the generated token
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            byte[] buffer = Encoding.UTF8.GetBytes(token);
                            context.Response.ContentLength64 = buffer.Length;
                            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            // Invalid credentials
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            string errorResponse = "Invalid username or password.";
                            byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                            context.Response.ContentLength64 = buffer.Length;
                            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        // Missing fields
                        throw new Exception("Missing Username or Password in the request body.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    string errorResponse = $"Error processing request: {ex.Message}";
                    byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
