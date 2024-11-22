using System;
using System.Threading.Tasks;

namespace MCTG
{
    class Program
    {
        public static void Main(string[] args)
        {
            //Create a new game instance and run it
            Game game = new Game();
            game.StartScreen();

            //int port = 10001;
            
            //var httpController = new HttpController(port);
            //httpController.Start();
        }
    }
}