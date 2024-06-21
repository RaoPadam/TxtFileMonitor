using System;
using System.IO;
using System.Threading;

namespace TxtFileMonitor
{
    class Program
    {
        private static int countdown = 15;
        private static Timer countdownTimer;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to TxtFileMonitor!");
            Console.WriteLine("Instructions:");
            Console.WriteLine("1. Enter the path of the .txt file to monitor when prompted.");
            Console.WriteLine("2. Enter the path for the report log file when prompted. If the file does not exist, it will be created.");
            Console.WriteLine("3. To stop the application, type 'exit' and press Enter.");
            Console.WriteLine();

            string filePath = GetValidFilePath("Please enter the path of the .txt file to monitor:");

            string logFilePath = GetValidLogFilePath("Please enter the path for the report log file:");

            // Read the initial content of the file
            string initialContent = File.ReadAllText(filePath);
            Console.WriteLine("Initial file content:");
            Console.WriteLine(initialContent);

            // Start the file observer
            Observers.FileObserver.StartFileObserver(filePath, logFilePath);

            // Process initial file content
            Observers.FileObserver.ProcessInitialFileContent(initialContent);

            // Set up a timer to report changes every 15 seconds
            Timer reportTimer = new Timer(Observers.FileObserver.ReportChanges, null, 15000, 15000);

            // Set up a countdown timer to update the countdown every second
            countdownTimer = new Timer(UpdateCountdown, null, 0, 1000);

            // Keep the application running until "exit" is entered
            Console.WriteLine("Type 'exit' to stop the application.");
            while (true)
            {
                string command = Console.ReadLine();
                if (!string.IsNullOrEmpty(command) && command.ToLower() == "exit")
                {
                    break;
                }
            }
        }

        private static string GetValidFilePath(string prompt)
        {
            while (true)
            {
                Console.WriteLine(prompt);
                string filePath = Console.ReadLine();
                if (File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".txt")
                {
                    return filePath;
                }
                Console.WriteLine("The specified file does not exist or is not a .txt file. Please try again.");
            }
        }

        private static string GetValidLogFilePath(string prompt)
        {
            Console.WriteLine(prompt);
            string logFilePath = Console.ReadLine();

            if (!File.Exists(logFilePath))
            {
                try
                {
                    File.Create(logFilePath).Dispose();
                    Console.WriteLine("Log file created at specified location.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating log file: {ex.Message}");
                }
            }

            return logFilePath;
        }

        private static void UpdateCountdown(object state)
        {
            if (countdown > 0)
            {
                Console.WriteLine($"Time until next report: {countdown} seconds");
                countdown--;
            }
            else
            {
                // Reset the countdown
                countdown = 15;

                // Check if there were any changes
                if (!Observers.FileObserver.HasChanges())
                {
                    Console.WriteLine("No changes");
                }
            }
        }
    }
}
