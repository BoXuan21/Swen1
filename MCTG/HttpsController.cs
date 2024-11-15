using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MCTG
{
    public class HttpsController
    {
        public async Task StartServer()
        {
            HttpListener listener = new HttpListener();

            // Ändere den Port hier
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
                    // Auf Anfrage warten
                    HttpListenerContext context = await listener.GetContextAsync();
                    Console.WriteLine($"Anfrage empfangen: {context.Request.HttpMethod} {context.Request.Url}");

                    if (context.Request.HttpMethod == "POST")
                    {
                        // POST-Daten lesen
                        using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                        {
                            string content = await reader.ReadToEndAsync();
                            Console.WriteLine("Empfangene POST-Daten:");
                            Console.WriteLine(content);

                            // Rückfrage formulieren
                            string responseString = "Daten erfolgreich empfangen. Bitte geben Sie weitere Details an (z.B. ein Name oder eine ID):";
                            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.ContentType = "text/plain";
                            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        // Antwort für nicht-POST-Anfragen
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
