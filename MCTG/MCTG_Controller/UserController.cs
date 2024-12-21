using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCTG
{
    public class UserController
    {
        private readonly DatabaseController _db;

        public UserController()
        {
            _db = new DatabaseController();
        }

        public async Task<User> Login()
        {
            Console.Clear();
            Console.WriteLine("Welcome to MCTG Login!");

            while (true)
            {
                Console.WriteLine("1. Log in");
                Console.WriteLine("2. Register");
                Console.WriteLine("3. Exit");
                Console.Write("Enter your choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.Write("Enter your username: ");
                        string username = Console.ReadLine();
                        Console.Write("Enter your password: ");
                        string password = Console.ReadLine();

                        var user = await _db.GetUser(username, password);
                        if (user != null)
                        {
                            Console.WriteLine("Login successful! Press any key to continue...");
                            Console.ReadKey();
                            return user;
                        }
                        else
                        {
                            Console.WriteLine("Invalid username or password. Please try again.");
                        }
                        break;

                    case "2":
                        await RegisterUser();
                        break;

                    case "3":
                        return null;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private async Task RegisterUser()
        {
            Console.WriteLine("Register a new account.");
            Console.Write("Enter a username: ");
            string username = Console.ReadLine();
            Console.Write("Enter a password: ");
            string password = Console.ReadLine();

            var newUser = new User
            {
                Username = username,
                Password = password,
                Coin = 20,
                Elo = "Iron"
            };

            bool success = await _db.CreateUser(username, password);
            if (success)
            {
                Console.WriteLine("Registration successful! You can now log in.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Username already exists. Please try a different username.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private async Task<bool> AuthenticateUser(string username, string password)
        {
            var user = await _db.GetUser(username, password);
            return user != null;
        }
    }
}