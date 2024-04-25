using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using static OpenAI_API.Models.Model;

namespace KFTelegramBot.Providers;

public class RecommendationsProvider(string apiKey, HttpClient httpClient, ILogger<RecommendationsProvider> logger)
    : IRecommendationsProvider
{
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
            var stringAsync = await client.GetStringAsync($"https://{url}");
            builder.Append(ExtractPlainTextFromHtmDoc(stringAsync));
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