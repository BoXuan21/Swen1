using System;
using System.Collections.Generic;
using System.IO;

namespace MCTG
{
    public class UserController
    {
        private Dictionary<string, string> userCredentials;

        public UserController()
        {
            userCredentials = new Dictionary<string, string>();
            LoadUserCredentials();
        }

        public bool Login()
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

                        if (AuthenticateUser(username, password))
                        {
                            Console.WriteLine("Login successful! Press any key to continue...");
                            Console.ReadKey();
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("Invalid username or password. Please try again.");
                        }
                        break;

                    case "2":
                        RegisterUser();
                        break;

                    case "3":
                        return false;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private void RegisterUser()
        {
            Console.WriteLine("Register a new account.");
            Console.Write("Enter a username: ");
            string username = Console.ReadLine();

            if (userCredentials.ContainsKey(username))
            {
                Console.WriteLine("Username already exists. Please try a different username.");
                return;
            }

            Console.Write("Enter a password: ");
            string password = Console.ReadLine();
            userCredentials[username] = password;

            SaveUserCredentials();
            Console.WriteLine("Registration successful!");
        }

        private bool AuthenticateUser(string username, string password)
        {
            return userCredentials.ContainsKey(username) && userCredentials[username] == password;
        }

        private void LoadUserCredentials()
        {
            string filePath = "userCredentials.txt";

            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                        userCredentials[parts[0]] = parts[1];
                }
            }
        }

        private void SaveUserCredentials()
        {
            string filePath = "userCredentials.txt";

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var credential in userCredentials)
                {
                    writer.WriteLine($"{credential.Key}:{credential.Value}");
                }
            }
        }
    }
}
