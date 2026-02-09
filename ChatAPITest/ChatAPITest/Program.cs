using System.Text;
using VideoTranslator.Services;

Console.WriteLine("=== ChatService 流式输出测试程序 ===\n");

var baseUrl = "http://127.0.0.1:1235/v1";
var apiKey = "dummy-key";
var model = "all/qwen/qwen3-4b-128k-ud-q8_k_xl.gguf";
var chatService = new ChatService(baseUrl, apiKey, model);

await TestResponsesStreamOutput(chatService);

static async Task TestResponsesStreamOutput(ChatService chatService)
{
    #region 显示流式输出

    Console.WriteLine("显示流式输出 (使用 ResponsesClient)");

    var userInput = "如果一个篮子里有3个苹果，我又放入2个，然后拿走1个，最后篮子里有几个苹果？请详细说明你的思考过程。";

    Console.WriteLine($"发送: {userInput}");
    Console.WriteLine("\n回答: ");
    var finalAnswer = new StringBuilder();
    var inReasoning = false;

    await foreach (var chunk in chatService.SendResponsesStreamAsync(userInput))
    {
        if (chunk.Success)
        {
            switch (chunk.EventType)
            {
                case "reasoning_started":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n[思考过程开始]");
                    inReasoning = true;
                    Console.ResetColor();
                    break;

                case "reasoning_completed":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n[思考过程完成]");
                    inReasoning = false;
                    Console.ResetColor();
                    break;

                case "reasoning_delta":
                case "reasoning_summary_delta":
                    var reasoningText = chunk.ReasoningDelta ?? "";
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(reasoningText);
                    Console.ResetColor();
                    break;

                case "output_text_delta":
                    var text = chunk.ContentDelta ?? "";
                    
                    if (inReasoning)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(text);
                    Console.ResetColor();
                    
                    finalAnswer.Append(text);
                    break;

                case "response_completed":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n\n[响应完成] 原因: {chunk.FinishReason}");
                    Console.ResetColor();
                    break;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ 错误: {chunk.ErrorMessage}");
            Console.ResetColor();
            break;
        }
    }

    Console.WriteLine($"\n\n✓ 测试完成");
    Console.WriteLine($"最终答案长度: {finalAnswer.Length} 字符");

    #endregion

    Console.WriteLine("\n=== 测试完成 ===");
}
