using System;
using System.Threading.Tasks;

namespace MCTG
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Create a new game instance and run it
            //Game game = new Game();
            //game.StartScreen();

             // Additional functionality for server (optional)
             UserController userController = new UserController();
             HttpsController httpsController = new HttpsController(userController);

             // Start the server
             await httpsController.StartServer();
        }
    }
}