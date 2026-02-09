using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;

namespace VideoTranslator.Services;

public class ChatService : ServiceBase
{
    #region 字段

    private readonly ResponsesClient _client;

    #endregion

    #region 构造函数

    public ChatService() : base()
    {
        var settings = base.Settings;
        var cfg = this.Config.VideoTranslator.LLM;
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(cfg.ApiUrl)
        };
        _client = new ResponsesClient(cfg.ModelName, new ApiKeyCredential(cfg.ApiKey), options);
    }

    #endregion

    #region 公共方法

    public async IAsyncEnumerable<ResponseStreamChunk> SendResponsesStreamAsync(string userInput)
    {
        progress?.Report("发送流式请求...");

        AsyncCollectionResult<StreamingResponseUpdate>? updates = null;
        Exception? exception = null;

        try
        {
            updates = _client.CreateResponseStreamingAsync(userInput);
        }
        catch (Exception ex)
        {
            exception = ex;
            progress?.Error($"错误: {ex.Message}");
        }

        if (exception != null)
        {
            yield return new ResponseStreamChunk
            {
                Success = false,
                ErrorMessage = exception.Message
            };
            yield break;
        }

        await foreach (var update in updates)
        {
            var chunk = ProcessStreamingUpdate(update);
            if (chunk != null)
            {
                yield return chunk;
            }
        }

        progress?.Report("流式响应完成");
    }

    #endregion

    #region 私有方法

    private ResponseStreamChunk? ProcessStreamingUpdate(StreamingResponseUpdate update)
    {
        if (update is StreamingResponseOutputItemAddedUpdate itemUpdate
            && itemUpdate.Item is ReasoningResponseItem reasoningItem)
        {
            progress?.Report($"[思考过程] ({reasoningItem.Status})");
            return new ResponseStreamChunk
            {
                Success = true,
                EventType = "reasoning_started",
                ReasoningStatus = reasoningItem.Status.ToString()
            };
        }
        else if (update is StreamingResponseOutputItemDoneUpdate itemDone
            && itemDone.Item is ReasoningResponseItem reasoningDone)
        {
            progress?.Report($"[思考完成] ({reasoningDone.Status})");
            return new ResponseStreamChunk
            {
                Success = true,
                EventType = "reasoning_completed",
                ReasoningStatus = reasoningDone.Status.ToString()
            };
        }
        else if (update is StreamingResponseReasoningTextDeltaUpdate reasoningDelta)
        {
            progress?.SetStatusMessage($"{reasoningDelta.Delta}", MessageType.Info,false,false);
            return new ResponseStreamChunk
            {
                Success = true,
                EventType = "reasoning_delta",
                ReasoningDelta = reasoningDelta.Delta
            };
        }
        else if (update is StreamingResponseReasoningSummaryTextDeltaUpdate summaryDelta)
        {
            progress?.Report($"[思考摘要] '{summaryDelta.Delta}'");
            return new ResponseStreamChunk
            {
                Success = true,
                EventType = "reasoning_summary_delta",
                ReasoningDelta = summaryDelta.Delta
            };
        }
        else if (update is StreamingResponseOutputTextDeltaUpdate delta)
        {
            progress?.SetStatusMessage($"{delta.Delta}",MessageType.Info, false,false);
            return new ResponseStreamChunk
            {
                Success = true,
                EventType = "output_text_delta",
                ContentDelta = delta.Delta
            };
        }
        else if (update is StreamingResponseOutputItemDoneUpdate outputDone)
        {
            progress?.Report($"[输出完成]");
            return new ResponseStreamChunk
            {
                Success = true,
                EventType = "response_completed",
                FinishReason = "completed"
            };
        }

        return null;
    }

    #endregion
}

#region Responses API 流式响应模型

public class ResponseStreamChunk
{
    public bool Success { get; set; }
    public string? EventType { get; set; }
    public string? ContentDelta { get; set; }
    public string? ReasoningDelta { get; set; }
    public string? ReasoningStatus { get; set; }
    public string? Role { get; set; }
    public string? ResponseId { get; set; }
    public string? FinishReason { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
