using KFTelegramBot.Model;

namespace KFTelegramBot.Providers;

public interface IMemoryStateProvider
{
    Dictionary<long, IPipeline?> GetState();
    bool IsContainUserId(long userId);
    IPipeline? GetCurrentPipeline(long userId);
    void SetCurrentPipeline(IPipeline pipeline, long userId);
}