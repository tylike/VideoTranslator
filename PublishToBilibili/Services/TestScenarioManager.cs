using PublishToBilibili.Models;

namespace PublishToBilibili.Services
{
    public class TestScenarioManager
    {
        public List<TestScenario> GetTestScenarios()
        {
            return new List<TestScenario>
            {
                 #region Scenario 1: 基础自制视频
                 new TestScenario
                 {
                     Name = "场景1: 基础自制视频",
                     Description = "测试基本的自制视频发布功能，包含标题、类型、标签、简介",
                     PublishInfo = new PublishInfo
                     {
                         VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                         Title = "测试视频标题 - 自制",
                         Type = "自制",
                         Tags = new List<string> { "测试标签1", "测试标签2", "测试标签3" },
                         Description = "这是一个测试视频的简介内容，用于验证基础自制视频发布功能。",
                         IsRepost = false,
                         SourceAddress = "",
                         EnableOriginalWatermark = false,
                         EnableNoRepost = false
                     }
                 },
                 #endregion

                #region Scenario 2: 转载视频
                new TestScenario
                {
                    Name = "场景2: 转载视频",
                    Description = "测试转载视频发布功能，包含来源地址",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "转载视频测试",
                        Type = "转载",
                        Tags = new List<string> { "转载", "测试" },
                        Description = "这是一个转载视频的测试内容。",
                        IsRepost = true,
                        SourceAddress = "https://www.example.com/source",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = false
                    }
                },
                #endregion

                #region Scenario 3: 带原创水印的自制视频
                new TestScenario
                {
                    Name = "场景3: 带原创水印的自制视频",
                    Description = "测试启用原创水印功能",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "原创水印测试视频",
                        Type = "自制",
                        Tags = new List<string> { "原创", "水印" },
                        Description = "测试原创水印功能的视频。",
                        IsRepost = false,
                        SourceAddress = "",
                        EnableOriginalWatermark = true,
                        EnableNoRepost = false
                    }
                },
                #endregion

                #region Scenario 4: 带禁止转载的转载视频
                new TestScenario
                {
                    Name = "场景4: 带禁止转载的转载视频",
                    Description = "测试转载视频并启用禁止转载功能",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "禁止转载测试视频",
                        Type = "转载",
                        Tags = new List<string> { "转载", "禁止转载" },
                        Description = "测试禁止转载功能的视频。",
                        IsRepost = true,
                        SourceAddress = "https://www.example.com/source2",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = true
                    }
                },
                #endregion

                #region Scenario 5: 完整功能测试
                new TestScenario
                {
                    Name = "场景5: 完整功能测试",
                    Description = "测试所有功能：转载视频 + 原创水印 + 禁止转载",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "完整功能测试视频",
                        Type = "转载",
                        Tags = new List<string> { "完整测试", "原创水印", "禁止转载" },
                        Description = "这是一个完整功能测试视频，包含所有选项。",
                        IsRepost = true,
                        SourceAddress = "https://www.example.com/full-test",
                        EnableOriginalWatermark = true,
                        EnableNoRepost = true
                    }
                },
                #endregion

                #region Scenario 6: 长标题和简介
                new TestScenario
                {
                    Name = "场景6: 长标题和简介",
                    Description = "测试长标题和长简介的处理能力",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "这是一个非常长的视频标题，用于测试系统对长标题的处理能力，确保能够正确显示和保存",
                        Type = "自制",
                        Tags = new List<string> { "长标题", "长简介" },
                        Description = "这是一个非常长的视频简介内容，用于测试系统对长简介的处理能力。这个简介包含了很多文字，用来验证系统是否能够正确处理和显示长文本内容。同时也可以测试UI在长文本情况下的表现和布局是否正常。",
                        IsRepost = false,
                        SourceAddress = "",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = false
                    }
                },
                #endregion

                #region Scenario 7: 多标签测试
                new TestScenario
                {
                    Name = "场景7: 多标签测试",
                    Description = "测试多个标签的填写和验证",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "多标签测试视频",
                        Type = "自制",
                        Tags = new List<string> { "标签1", "标签2", "标签3", "标签4", "标签5", "标签6" },
                        Description = "测试多个标签的填写和验证功能。",
                        IsRepost = false,
                        SourceAddress = "",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = false
                    }
                },
                #endregion

                #region Scenario 8: 特殊字符测试
                new TestScenario
                {
                    Name = "场景8: 特殊字符测试",
                    Description = "测试标题和简介中包含特殊字符的情况",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "特殊字符测试 @#$%^&*()_+-=[]{}|;':\",./<>?",
                        Type = "自制",
                        Tags = new List<string> { "特殊字符", "测试" },
                        Description = "测试特殊字符 @#$%^&*()_+-=[]{}|;':\",./<>? 在标题和简介中的显示和处理。",
                        IsRepost = false,
                        SourceAddress = "",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = false
                    }
                },
                #endregion

                #region Scenario 9: 空值测试
                new TestScenario
                {
                    Name = "场景9: 空值测试",
                    Description = "测试空标题、空标签、空简介的情况",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "",
                        Type = "自制",
                        Tags = new List<string>(),
                        Description = "",
                        IsRepost = false,
                        SourceAddress = "",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = false
                    }
                },
                #endregion

                #region Scenario 10: 中文内容测试
                new TestScenario
                {
                    Name = "场景10: 中文内容测试",
                    Description = "测试纯中文内容的处理",
                    PublishInfo = new PublishInfo
                    {
                        VideoFilePath = @"D:\VideoTranslator\videoProjects\1\2B-OancSM80.mp4",
                        Title = "中文标题测试视频",
                        Type = "自制",
                        Tags = new List<string> { "中文", "测试", "视频" },
                        Description = "这是一个纯中文的测试视频简介，用于验证系统对中文内容的处理能力。包含各种中文字符和标点符号。",
                        IsRepost = false,
                        SourceAddress = "",
                        EnableOriginalWatermark = false,
                        EnableNoRepost = false
                    }
                }
                #endregion
            };
        }

        public TestScenario GetScenarioByName(string scenarioName)
        {
            return GetTestScenarios().FirstOrDefault(s => s.Name == scenarioName) ?? new TestScenario();
        }

        public TestScenario GetScenarioById(int scenarioId)
        {
            var scenarios = GetTestScenarios();
            if (scenarioId >= 0 && scenarioId < scenarios.Count)
            {
                return scenarios[scenarioId];
            }
            return new TestScenario();
        }
    }

    public class TestScenario
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public PublishInfo PublishInfo { get; set; } = new PublishInfo();
    }
}
