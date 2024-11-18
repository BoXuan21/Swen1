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

        //inject userController
        public HttpsController(UserController userController)
        {
            _userController = userController;
        }

        public async Task StartServer()
        {
            HttpListener listener = new HttpListener();
            
            string url = "http://localhost:5001/";
            listener.Prefixes.Add(url);

            try
            {
                listener.Start();
                Console.WriteLine($"HTTPS Server gestartet. Listening on {url}");
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("Fehler beim Start des Servers. Stelle sicher, dass das SSL-Zertifikat korrekt konfiguriert ist.");
                Console.WriteLine($"Fehlermeldung: {ex.Message}");
                return;
            }

            while (true)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    Console.WriteLine($"Anfrage empfangen: {context.Request.HttpMethod} {context.Request.Url}");

                    if (context.Request.HttpMethod == "POST")
                    {
                        using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                        {
                            string content = await reader.ReadToEndAsync();
                            Console.WriteLine("Empfangene POST-Daten:");
                            Console.WriteLine(content);

                            try
                            {
                                // Parse JSON to extract userId and endpoint
                                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                                if (data != null && data.ContainsKey("userId") && data.ContainsKey("endpoint"))
                                {
                                    string userId = data["userId"];
                                    string endpoint = data["endpoint"];

                                    // Store in UserController
                                    _userController.AddOrUpdateUserEndpoint(userId, endpoint);

                                    string responseString = "Benutzer erfolgreich hinzugefügt oder aktualisiert.";
                                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                    context.Response.ContentLength64 = buffer.Length;
                                    context.Response.ContentType = "text/plain";
                                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                }
                                else
                                {
                                    throw new Exception("Ungültige Daten. userId und endpoint erforderlich.");
                                }
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                string errorResponse = $"Fehler beim Verarbeiten der Daten: {ex.Message}";
                                byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                                context.Response.ContentLength64 = buffer.Length;
                                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        string errorResponse = "Nur POST-Anfragen sind erlaubt.";
                        byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }

                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler bei der Verarbeitung der Anfrage: {ex.Message}");
                }
            }
        }
    }
}
