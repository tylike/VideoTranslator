using System;
using System.IO;
using System.Text.Json;
using VideoTranslator.SRT.Models;

namespace VideoTranslator.SRT.Services
{
    public class WhisperJsonParser
    {
        #region 公共方法

        public WhisperJsonRoot Parse(string jsonFilePath)
        {
            if (string.IsNullOrEmpty(jsonFilePath))
            {
                throw new ArgumentNullException(nameof(jsonFilePath));
            }

            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            return ParseJson(jsonContent);
        }

        public WhisperJsonRoot ParseJson(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new ArgumentNullException(nameof(jsonContent));
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                return JsonSerializer.Deserialize<WhisperJsonRoot>(jsonContent, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse JSON content", ex);
            }
        }

        #endregion
    }
}
