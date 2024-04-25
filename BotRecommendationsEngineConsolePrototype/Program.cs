using System.Net.Http;
using System.Text;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using HtmlAgilityPack;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using SharedLibrary;
using static OpenAI_API.Chat.ChatMessage;

namespace BotRecommendationsEngineConsolePrototype;

internal class Program
{
    private static string GetPossibleVariantsExample()
    {
        var values = Enum.GetValues(typeof(VioletColor));
        var colors = values
            .OfType<VioletColor>()
            .Select(color => ExtensionMethods.GetEnumDescription(color))
            .ToArray();

        return string.Join(',', colors);
    }

    static async Task Main(string[] args)
    {
        //await NewMethod1();
        await NewMethod();
    }

    private async Task NewMethod1()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://duckduckgo.com/html/?q=%D0%BA%D0%BE%D1%80%D1%88%D1%83%D0%BD%D0%BE%D0%B2%D0%B0%2B%D0%95%D0%9A-%D0%9D%D0%B5%D0%B1%D0%B5%D1%81%D0%BD%D0%BE%D0%B5%2B%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D0%BD%D0%B8%D0%B5"),
        };

        string htmlContent;
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            htmlContent = body;
        }

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

        var api = new OpenAIAPI();

        var chat = api.Chat.CreateConversation();
        chat.Model = Model.ChatGPTTurbo;
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
                              {builder}
                              ---КОНЕЦ---
                              """);
        var violetDescription = await chat.GetResponseFromChatbotAsync();
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

    private static async Task NewMethod()
    {
        var api = new OpenAIAPI();

        var chat = api.Chat.CreateConversation();
        chat.Model = Model.GPT4_Vision;
        chat.AppendSystemMessage(
            $"""
             Ты эксперт по анализую фотографий фиалок.
             Ты можешь с первого взгляда оценить цвет и форму лепестков, а так же дать комментарий о листьях фиалки.
             При анализе цвета цветка ты используешь ТОЛЬКО эти цвета: {GetPossibleVariantsExample()}.
             Когда ты описываешь цвета - ты описываешь ТОЛЬКО цвета самого цветка. Цвет листьев не интересен.
             """);
        chat.AppendUserInput("Проанализируй эту фотографию!",
            ImageInput.FromFile(@"D:\Downloads\Фиалка ЕК-Небесное создание — описание, фото и характеристики сорта.jpg"));
        var imageDescription = await chat.GetResponseFromChatbotAsync();

        var colorsRequest = new ChatRequest
        {
            Model = Model.ChatGPTTurbo,
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
                     В массиве используй ТОЛЬКО эти цвета: {GetPossibleVariantsExample()}.
                     ---ОПИСАНИЕ---
                     {imageDescription}
                     ---КОНЕЦ---
                     Верни JSON объект типа словарь с ключом 'Colors' и массивом ключевых цветов.
                     """)
            }
        };

        var colors = await api.Chat.CreateChatCompletionAsync(colorsRequest);
    }
}