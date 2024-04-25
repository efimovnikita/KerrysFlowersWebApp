namespace KFTelegramBot.Providers;

public interface IRecommendationsProvider
{
    Task<string> GetDescriptionRecommendation(string prompt);
    Task<string> GetColorsRecommendation(string base64Image);
}