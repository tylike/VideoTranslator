global using MessageType = NewConsole.MT;
using PublishToBilibili.Services;
using PublishToBilibili.Models;
using PublishToBilibili.Interfaces;
using FlaUI.Core.Definitions;
namespace PublishToBilibili
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                #region Initialize Services
                var processService = new ProcessService();
                IWindowService windowService = new WindowService();
                var publishApi = new BilibiliPublishApi(processService, windowService);
                var scenarioManager = new TestScenarioManager();
                #endregion

                #region Display Available Scenarios
                var scenarios = scenarioManager.GetTestScenarios();
                Console.WriteLine("=== Available Test Scenarios ===");
                for (int i = 0; i < scenarios.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {scenarios[i].Name}");
                    Console.WriteLine($"   {scenarios[i].Description}");
                    Console.WriteLine();
                }
                #endregion

                #region Select Scenario
                System.Console.Write("Enter scenario number (1-10) or 'all' to run all scenarios: ");
                var input = System.Console.ReadLine()?.Trim().ToLower();

                if (input == "all")
                {
                    RunAllScenarios(scenarios, publishApi);
                }
                else if (int.TryParse(input, out int scenarioNumber) && scenarioNumber >= 1 && scenarioNumber <= scenarios.Count)
                {
                    RunSingleScenario(scenarios[scenarioNumber - 1], publishApi);
                }
                else
                {
                    Console.WriteLine("Invalid input. Running default scenario (Scenario 1)...");
                    RunSingleScenario(scenarios[0], publishApi);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        #region Scenario Execution Methods

        static void RunSingleScenario(TestScenario scenario, BilibiliPublishApi publishApi)
        {
            Console.WriteLine($"\n{'='*60}", MessageType.Info);
            Console.WriteLine($"Running: {scenario.Name}", MessageType.Info);
            Console.WriteLine($"Description: {scenario.Description}", MessageType.Info);
            Console.WriteLine($"{'='*60}\n", MessageType.Info);

            var publishInfo = scenario.PublishInfo;

            #region Display Publish Info
            Console.WriteLine("=== Publish Info ===", MessageType.Info);
            Console.WriteLine($"Video Path: {publishInfo.VideoFilePath}", MessageType.Info);
            Console.WriteLine($"Title: {publishInfo.Title}", MessageType.Info);
            Console.WriteLine($"Type: {publishInfo.Type}", MessageType.Info);
            Console.WriteLine($"Tags: {string.Join(", ", publishInfo.Tags)}", MessageType.Info);
            Console.WriteLine($"Description: {publishInfo.Description}", MessageType.Info);
            Console.WriteLine($"Is Repost: {publishInfo.IsRepost}", MessageType.Info);
            Console.WriteLine($"Source Address: {publishInfo.SourceAddress}", MessageType.Info);
            Console.WriteLine($"Enable Original Watermark: {publishInfo.EnableOriginalWatermark}", MessageType.Info);
            Console.WriteLine($"Enable No Repost: {publishInfo.EnableNoRepost}", MessageType.Info);
            Console.WriteLine();
            #endregion

            #region Execute Publish
            var result = publishApi.PublishVideo(publishInfo);

            if (result)
            {
                Console.WriteLine("\n=== Publish Video Successfully ===");
                
                #region Validate Form
                // Console.WriteLine("\n=== Starting Validation ===");
                // var validationResult = ValidateForm(publishInfo);

                // if (validationResult.IsValid)
                // {
                //     Console.WriteLine("\n✓✓✓ All validations passed! ✓✓✓");
                // }
                // else
                // {
                //     Console.WriteLine("\n✗✗✗ Some validations failed! ✗✗✗");
                // }
                #endregion
            }
            else
            {
                Console.WriteLine("\n=== Publish Video Failed ===");
            }
            #endregion
        }

        static void RunAllScenarios(List<TestScenario> scenarios, BilibiliPublishApi publishApi)
        {
            Console.WriteLine($"\n{'='*60}");
            Console.WriteLine("Running All Scenarios");
            Console.WriteLine($"{'='*60}\n");

            var results = new List<ScenarioResult>();

            foreach (var scenario in scenarios)
            {
                var result = RunScenarioWithValidation(scenario, publishApi);
                results.Add(result);
            }

            #region Print Summary
            Console.WriteLine($"\n{'='*60}");
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"{'='*60}");
            
            int passedCount = results.Count(r => r.PublishSuccess);
            int totalCount = results.Count;

            Console.WriteLine($"\nTotal Scenarios: {totalCount}");
            Console.WriteLine($"Passed: {passedCount}");
            Console.WriteLine($"Failed: {totalCount - passedCount}");
            Console.WriteLine($"Success Rate: {(double)passedCount / totalCount * 100:F2}%");

            Console.WriteLine("\n=== Detailed Results ===");
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var status = result.PublishSuccess ? "✓ PASS" : "✗ FAIL";
                Console.WriteLine($"{i + 1}. {result.ScenarioName}: {status}");
            }
            #endregion
        }

        static ScenarioResult RunScenarioWithValidation(TestScenario scenario, BilibiliPublishApi publishApi)
        {
            var result = new ScenarioResult
            {
                ScenarioName = scenario.Name
            };

            Console.WriteLine($"\n{'='*60}");
            Console.WriteLine($"Running: {scenario.Name}");
            Console.WriteLine($"{'='*60}\n");

            var publishInfo = scenario.PublishInfo;

            #region Execute Publish
            var publishResult = publishApi.PublishVideo(publishInfo);
            result.PublishSuccess = publishResult;

            if (publishResult)
            {
                Console.WriteLine("\n=== Publish Video Successfully ===");
                
                #region Validate Form
                // var validationResult = ValidateForm(publishInfo);
                // result.ValidationResult = validationResult;
                // result.ValidationSuccess = validationResult.IsValid;
                #endregion
            }
            else
            {
                Console.WriteLine("\n=== Publish Video Failed ===");
            }
            #endregion

            return result;
        }


        #endregion
    }

    #region Result Models

    public class ScenarioResult
    {
        public string ScenarioName { get; set; } = "";
        public bool PublishSuccess { get; set; }
        // public bool ValidationSuccess { get; set; }
        // public ValidationResult ValidationResult { get; set; } = new ValidationResult();
    }

    #endregion
}
