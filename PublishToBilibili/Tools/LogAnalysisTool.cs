using System;
using PublishToBilibili.Services;

namespace PublishToBilibili.Tools
{
    class LogAnalysisTool
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Bilibili Publish Log Analysis Tool ===\n");

            Console.WriteLine("Available options:");
            Console.WriteLine("1. Analyze existing logs");
            Console.WriteLine("2. Compare scenarios (SelfMade vs Repost)");
            Console.WriteLine("3. Generate full report");
            Console.WriteLine("0. Exit");
            Console.Write("\nSelect option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    AnalyzeLogs();
                    break;
                case "2":
                    CompareScenarios();
                    break;
                case "3":
                    GenerateFullReport();
                    break;
                case "0":
                    Console.WriteLine("Exiting...");
                    break;
                default:
                    Console.WriteLine("Invalid option!");
                    break;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void AnalyzeLogs()
        {
            Console.WriteLine("\n=== Analyzing Existing Logs ===");
            var logAnalyzer = new LogAnalyzer();
            logAnalyzer.AnalyzeAllLogs();
            Console.WriteLine("\nAnalysis complete. Check ControlMappings directory for results.");
        }

        private static void CompareScenarios()
        {
            Console.WriteLine("\n=== Comparing Scenarios ===");
            var logAnalyzer = new LogAnalyzer();
            logAnalyzer.CompareScenarios();
            Console.WriteLine("\nComparison complete. Check ControlMappings directory for results.");
        }

        private static void GenerateFullReport()
        {
            Console.WriteLine("\n=== Generating Full Report ===");
            var logAnalyzer = new LogAnalyzer();
            
            Console.WriteLine("\n1. Analyzing all logs...");
            logAnalyzer.AnalyzeAllLogs();

            Console.WriteLine("\n2. Comparing scenarios...");
            logAnalyzer.CompareScenarios();

            Console.WriteLine("\n3. Report generation complete!");
            Console.WriteLine("Check the ControlMappings directory for detailed reports.");
        }
    }
}
