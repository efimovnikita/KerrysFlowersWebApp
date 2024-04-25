using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using OpenAI_API.Chat;
using SharedLibrary;
using static OpenAI_API.Chat.ChatMessage;
using static OpenAI_API.Models.Model;

namespace KFTelegramBot.Providers;

public class RecommendationsProvider(string apiKey, HttpClient httpClient, ILogger<RecommendationsProvider> logger)
    : IRecommendationsProvider
{
    public async Task<string> GetColorsRecommendation(string base64Image)
    {
        try
        {
            var api = new OpenAIAPI();

            var chat = api.Chat.CreateConversation();
            chat.Model = GPT4_Vision;

            var possibleVariants = GetStringOfPossibleVariants();

            chat.AppendSystemMessage(
                $"""
                 Ты эксперт по анализую фотографий фиалок.
                 Ты можешь с первого взгляда оценить цвет и форму лепестков, а так же дать комментарий о листьях фиалки.
                 При анализе цвета цветка ты используешь ТОЛЬКО эти цвета: {possibleVariants}.
                 Когда ты описываешь цвета - ты описываешь ТОЛЬКО цвета самого цветка. Цвет листьев не интересен.
                 """);
            chat.AppendUserInput("Проанализируй эту фотографию!",
                ImageInput.FromFile(SaveBase64ImageToTempFile(base64Image)));
            var imageDescription = await chat.GetResponseFromChatbotAsync();

            var colorsRequest = new ChatRequest
            {
                Model = ChatGPTTurbo,
                Temperature = 0.0,
                MaxTokens = 2000,
                ResponseFormat = ChatRequest.ResponseFormats.JsonObject,
                Messages = new[]
                {
                    new ChatMessage(ChatMessageRole.System, """
                                                            Ты помощник, который умеет возвращать массив ключевых цветов цветка фиалки на основании текстового описания.
                                                            Ты возвращаешь результат в форме JSON документа.
                                                            """),
                    new ChatMessage(ChatMessageRole.User,
                        $"""
                         Дай мне массив (не более 3 элементов) ключевых цветов по этому текстовому описанию.
                         В массиве используй ТОЛЬКО эти цвета: {possibleVariants}.
                         ---ОПИСАНИЕ---
                         {imageDescription}
                         ---КОНЕЦ---
                         Верни JSON объект типа словарь с ключом 'Colors' и массивом ключевых цветов.
                         """)
                }
            };

            var colors = await api.Chat.CreateChatCompletionAsync(colorsRequest);
            var response = JsonSerializer.Deserialize<ColorsRecommendationResponse>(colors.Choices.First().Message.TextContent);

            if (response == null)
            {
                return "";
            }

            var unfilteredColors = response.Colors;
            var arrayOfPossibleVariants = GetArrayOfPossibleVariants();
            var filteredColors = unfilteredColors.Where(s => arrayOfPossibleVariants.Contains(s)).ToArray();

            return string.Join(',', filteredColors);
        }
        catch (Exception e)
        {
            logger.LogError("{Error}", e);
            return "";
        }
    }

    private static string GetStringOfPossibleVariants() => string.Join(", ", GetArrayOfPossibleVariants());

    private static string[] GetArrayOfPossibleVariants()
    {
        var values = Enum.GetValues(typeof(VioletColor));
        return values
            .OfType<VioletColor>()
            .Select(color => ExtensionMethods.GetEnumDescription(color))
            .ToArray();
    }

    private class ColorsRecommendationResponse
    {
        public string[] Colors { get; set; }
    }

    private static string SaveBase64ImageToTempFile(string base64Image)
    {
        var imageBytes = Convert.FromBase64String(base64Image);

        var tempDirectory = Path.GetTempPath();

        var fileName = Guid.NewGuid() + ".jpg";

        var filePath = Path.Combine(tempDirectory, fileName);

        File.WriteAllBytes(filePath, imageBytes);

        return filePath;
    }

    public async Task<string> GetDescriptionRecommendation(string prompt)
    {
        try
        {
            logger.LogInformation("Trying to get recommendation result for a '{prompt}'", prompt);
            var engineResponse = await GetSearchEngineResponse(httpClient, prompt);

            var contentFromWebPages = await GetTextContentFromWebPages(httpClient, engineResponse);

            return await GetDescriptionFromTheModel(apiKey, contentFromWebPages);
        }
        catch (Exception e)
        {
            logger.LogError("{Error}", e);
            return "";
        }
    }

    private static async Task<string> GetDescriptionFromTheModel(string apiKey, StringBuilder contentFromWebPages)
    {
        var api = new OpenAIAPI(new APIAuthentication(apiKey));

        var chat = api.Chat.CreateConversation();
        chat.Model = ChatGPTTurbo;
        chat.AppendSystemMessage(
            $"""
             Ты эксперт по составлению описаний для фиалок. Твои описания используются на сайте о фиалках.
             Сначала ты описываешь цветки, потом листья и розетку растения в целом.
             Твоё описание должнр включать в себя только описание внешнего вида растения, без информации о уходе или размножении.
             Обычно твоё описание не более 6 предложений.
             """);
        chat.AppendUserInput($"""
                              Составь описание фиалки на основании этой сырой компиляции:
                              ---НАЧАЛО---
                              {contentFromWebPages}
                              ---КОНЕЦ---
                              """);

        return await chat.GetResponseFromChatbotAsync();
    }

    private static async Task<StringBuilder> GetTextContentFromWebPages(HttpClient client, string htmlContent)
    {
        var document = new HtmlDocument();
        document.LoadHtml(htmlContent);

        var firstThreeLinks = document.DocumentNode.SelectNodes("//a[contains(@class, 'result__url')]")
            .Take(3)
            .ToList();

        var builder = new StringBuilder();
        foreach (var link in firstThreeLinks)
        {
            var url = link.InnerText.Trim();
            try
            {
                var stringAsync = await client.GetStringAsync($"https://{url}");
                builder.Append(ExtractPlainTextFromHtmDoc(stringAsync));
            }
            catch
            {
                // ignored
            }
        }

        return builder;
    }

    private static async Task<string> GetSearchEngineResponse(HttpClient client, string prompt)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri =
                new Uri(
                    $"https://duckduckgo.com/html/?q={prompt}"),
        };

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static string ExtractPlainTextFromHtmDoc(string text)
    {
        HtmlDocument htmlDocument = new();
        htmlDocument.LoadHtml(text);

        HtmlNode? bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
        return bodyNode == null ? text : ExtractTextFromNode(bodyNode);
    }

    private static string ExtractTextFromNode(HtmlNode? node)
    {
        if (node == null)
        {
            return "";
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            return node.InnerText.Trim();
        }

        if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        if (node.HasAttributes && node.Attributes.Contains("style"))
        {
            node.Attributes.Remove("style");
        }

        StringBuilder builder = new();

        foreach (HtmlNode? childNode in node.ChildNodes)
        {
            builder.AppendLine(ExtractTextFromNode(childNode));
        }

        return builder.ToString().Trim();
    }
}