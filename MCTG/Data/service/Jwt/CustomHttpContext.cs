using System.Collections.Concurrent;
using System.Text;

namespace MCTG
{
    public class CustomHttpContext
    {
        public CustomHttpRequest Request { get; set; }
        public CustomHttpResponse Response { get; set; }
        public IDictionary<string, object> Items { get; }

        public CustomHttpContext()
        {
            Request = new CustomHttpRequest();
            Response = new CustomHttpResponse();
            Items = new ConcurrentDictionary<string, object>();
        }

        public string GetUsername()
        {
            return Items.TryGetValue("Username", out var username) ? username?.ToString() : null;
        }
    }

    public class CustomHttpRequest
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public IDictionary<string, string> Headers { get; }
        public string Body { get; set; }

        public CustomHttpRequest()
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetHeader(string headerName, out string headerValue)
        {
            return Headers.TryGetValue(headerName, out headerValue);
        }
    }

    public class CustomHttpResponse
    {
        public int StatusCode { get; set; } = 200;
        public string StatusDescription { get; set; } = "OK";
        public IDictionary<string, string> Headers { get; }
        public string Body { get; set; }

        public CustomHttpResponse()
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Headers["Content-Type"] = "application/json";
        }
        

        public void SetUnauthorized()
        {
            StatusCode = 401;
            StatusDescription = "Unauthorized";
            Body = "Authentication required";
        }
    }
}