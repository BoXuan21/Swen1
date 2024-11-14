using System;
using System.Threading.Tasks;

namespace MCTG
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpsController httpsController = new HttpsController();
            await httpsController.StartServer();
        }
    }
}