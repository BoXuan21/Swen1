using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MCTG_IF23B049
{
    public class HttpsController
    {
        // Method to start the HTTPS server
        public async Task StartServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("https://localhost:5001/");
            listener.Start();
            Console.WriteLine("HTTPS Server started. Listening on https://localhost:5001/");

            while (true)
            {
                try
                {
                    // Wait for incoming request
                    HttpListenerContext context = await listener.GetContextAsync();
                    Console.WriteLine("Received request.");

                    // Check for POST method
                    if (context.Request.HttpMethod == "POST")
                    {
                        // Read the POST data
                        using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                        {
                            string content = await reader.ReadToEndAsync();
                            Console.WriteLine("Received POST data:");
                            Console.WriteLine(content);

                            // Respond to client
                            string responseString = "Data received successfully.";
                            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                            context.Response.ContentLength64 = buffer.Length;
                            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                        }
                    }
                    else
                    {
                        // Respond with 405 for non-POST requests
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
    }
}
