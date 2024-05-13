using System.Text;
using KFTelegramBot.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLibrary.Providers;
using Telegram.Bot;

namespace KFTelegramBot.Model;

public class CheckNewOrdersBackgroundTask(
    ILogger<CheckNewOrdersBackgroundTask> logger,
    ITelegramBotClient botClient,
    IMemoryStateProvider memoryStateProvider,
    IVioletRepository violetRepository)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var orders = violetRepository.GetAllActiveOrders();

                if (orders.Count > 0)
                {
                    foreach (var order in orders)
                    {
                        if (order == null)
                        {
                            continue;
                        }

                        var violetsInfo = new StringBuilder();
                        foreach (var violet in order.Violets)
                        {
                            violetsInfo.AppendLine($"{violet.Name}");

                            if (violet.SelectedLeafs > 0)
                            {
                                violetsInfo.AppendLine($"Листьев {violet.SelectedLeafs} шт. - {violet.CalculatedLeafsPrice} руб.");
                            }

                            if (violet.SelectedChildren > 0)
                            {
                                violetsInfo.AppendLine(
                                    $"Деток {violet.SelectedChildren} шт. - {violet.CalculatedChildrenPrice} руб.");
                            }

                            if (violet.SelectedWholePlants > 0)
                            {
                                violetsInfo.AppendLine(
                                    $"Растений {violet.SelectedWholePlants} шт. - {violet.CalculatedWholePlantsPrice} руб.");
                            }

                            violetsInfo.AppendLine();
                        }
                        
                        violetsInfo.AppendLine($"Итого: {order.TotalPrice} руб.");

                        var state = memoryStateProvider.GetState()
                            .Select(pair => pair.Key);
#if DEBUG
                        state = state.Take(1);
#endif
                        foreach (var id in state)
                        {
                            await botClient.SendTextMessageAsync(id,
                                $"""
                                 Новый заказ ({order.Date:dddd, dd MMMM yyyy HH:mm:ss})

                                 Имя: {(string.IsNullOrWhiteSpace(order.Name) == false ? order.Name : "-")}
                                 Телефон: {order.PhoneNumber}
                                 Адрес: {order.Address}
                                 Email: {(string.IsNullOrWhiteSpace(order.Email) == false ? order.Email : "-")}

                                 Состав заказа:

                                 {violetsInfo}
                                 """, 
                                cancellationToken: stoppingToken);
                        }

                        violetRepository.SetOrderInactive(order.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error performing periodic work");
            }

            await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
        }
    }
}