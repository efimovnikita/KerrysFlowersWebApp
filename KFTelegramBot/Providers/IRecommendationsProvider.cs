namespace KFTelegramBot.Providers;

public interface IRecommendationsProvider
{
    Task<string> GetDescriptionRecommendation(string prompt);
}