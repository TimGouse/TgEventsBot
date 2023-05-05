using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class EventBot
{
    private static TelegramBotClient _bot;
    private readonly TimePadApi _timePadApi;
    private readonly DatabaseManager _databaseManager;

    public async Task InitializeBot(string telegramApiKey)
    {
        await _databaseManager.InitializeDatabaseAsync();
        _bot = new TelegramBotClient(telegramApiKey);

        var me = await _bot.GetMeAsync();
        Console.WriteLine($"Bot: {me.FirstName} is now running...");

        await ReceiveMessagesAsync();
        
    }

    private async Task ReceiveMessagesAsync()
    {
        var offset = 0;

        while (true)
        {
            var updates = await _bot.GetUpdatesAsync(offset);

            foreach (var update in updates)
            {
                if (update.Message?.Text != null)
                {
                    Console.WriteLine($"Message received: {update.Message.Text}");

                    switch (update.Message.Text.Split(' ')[0].ToLower())
                    {
                        case "/start":
                            //await SendAvailableCommandsAsync(update.Message.Chat.Id);
                            await SendMenu(update.Message.Chat.Id);
                            break;
                        case "/search":
                            await OnSearchCommand(update.Message);
                            break;
                        case "/addtolist":
                            await OnAddToListCommand(update.Message);
                            break;
                        case "/removefromlist":
                            await OnRemoveFromListCommand(update.Message);
                            break;
                        case "/recommendations":
                            await OnRecommendationsCommand(update.Message);
                            break;
                        case "/getwishlist":
                            await GetWishlist(update.Message);
                            break;
                        case "/getvisitedlist":
                            await GetVisitedlist(update.Message);
                            break;
                        default:
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Неизвестная комманда: {update.Message.Text}");
                            break;
                    }
                }
                else if (update.CallbackQuery != null)
                {
                    // Обработка обратных вызовов InlineKeyboard
                    var callbackQuery = update.CallbackQuery;

                    if (callbackQuery.Data == "menu")
                    {
                        await SendAvailableCommandsAsync(callbackQuery.Message.Chat.Id);
                    }

                    // Отправьте ответ на обратный вызов, чтобы убрать "часы" на кнопке
                    await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                }

                offset = update.Id + 1;
            }

            await Task.Delay(1000);
        }
    }
    private async Task SendMenu(long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
        new []
        {
            InlineKeyboardButton.WithCallbackData("Меню", "menu")
        }
    });

        await _bot.SendTextMessageAsync(chatId, "Выберите меню:", replyMarkup: keyboard);
    }
    private async Task SendAvailableCommandsAsync(ChatId chatId)
    {
        var commandsList = new StringBuilder();
        commandsList.AppendLine("Вот доступные команды:");
        commandsList.AppendLine("/search - Поиск ближайших по времени мероприятий в Москве.");
        commandsList.AppendLine("/addtolist {название_списка} {id_мероприятия} - Добавить мероприятие в список 'visited' или 'wishlist'.");
        commandsList.AppendLine("/removefromlist {название_списка} {id_мероприятия} - Удалить мероприятие из списка 'visited' или 'wishlist'.");
        commandsList.AppendLine("/getwishlist - Получить список желаемых мероприятий.");
        commandsList.AppendLine("/getvisitedlist - Получить список посещенных мероприятий.");
        commandsList.AppendLine("/recommendations - Получить мероприятия по категории ИТ и интернет");
        commandsList.AppendLine("/start - Показать меню.");

        await _bot.SendTextMessageAsync(chatId, commandsList.ToString());
    }

    private async Task OnSearchCommand(Message message)
    {
        var messageParts = message.Text.Split(' ');

        string searchText = messageParts.Length > 1 ? messageParts[1] : null;

        var events = await _timePadApi.SearchEventsAsync(searchText);

        if (events.Count == 0)
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Мероприятия не найдены.");
            return;
        }

        StringBuilder eventsText = new StringBuilder();
        foreach (var eventItem in events)
        {
            eventsText.AppendLine($"ID мероприятия: {eventItem.Id}");
            eventsText.AppendLine($"Название: {eventItem.Name}");
            //eventsText.AppendLine($"Описание: {eventItem.Description}");
            //eventsText.AppendLine($"Дата и время: {eventItem.StartsAt}");
            //eventsText.AppendLine($"Местоположение: {eventItem.Location?.City ?? "не указано"}");
            eventsText.AppendLine("Местоположение: Москва");
            eventsText.AppendLine($"URL: {eventItem.Url}");
            eventsText.AppendLine("-----------------------------");
        }

        await _bot.SendTextMessageAsync(message.Chat.Id, eventsText.ToString());
    }

    private async Task OnAddToListCommand(Message message)
    {
        // Разбиение сообщения на части и парсинг параметров
        var messageParts = message.Text.Split(' ');

        // Проверка количества аргументов
        if (messageParts.Length < 3)
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Укажите имя списка (visited/wishlist) и event ID.");
            return;
        }

        // Получение названия списка и идентификатора события
        string listName = messageParts[1];
        if (!int.TryParse(messageParts[2], out int eventId))
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Неверный event ID.");
            return;
        }
        // Добавление пользователя в базу данных, если его там еще нет
        await _databaseManager.AddUserAsync(message.Chat.Id);

        // Добавление события в соответствующий список
        if (listName.Equals("visited", StringComparison.OrdinalIgnoreCase))
        {
            await _databaseManager.AddVisitedEventAsync(message.Chat.Id, eventId);
            await _bot.SendTextMessageAsync(message.Chat.Id, "Событие добавлено в список (visited).");
            await _databaseManager.GetVisitedEventsAsync(message.Chat.Id);
        }
        else if (listName.Equals("wishlist", StringComparison.OrdinalIgnoreCase))
        {
            await _databaseManager.AddWishlistEventAsync(message.Chat.Id, eventId);
            await _bot.SendTextMessageAsync(message.Chat.Id, "Событие добавлено в список (wishlist).");
            
        }
        else
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Неверное имя списка. Пожалуйста используйте 'visited'  'wishlist'.");
        }
    }
    private async Task OnRemoveFromListCommand(Message message)
    {
        // Разбиение сообщения на части и парсинг параметров
        var messageParts = message.Text.Split(' ');

        // Проверка количества аргументов
        if (messageParts.Length < 3)
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Укажите имя списка (visited/wishlist) и event ID.");
            return;
        }

        // Получение названия списка и идентификатора события
        string listName = messageParts[1];
        if (!int.TryParse(messageParts[2], out int eventId))
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Неверный event ID.");
            return;
        }

        // Удаление события из соответствующего списка
        if (listName.Equals("visited", StringComparison.OrdinalIgnoreCase))
        {
            await _databaseManager.RemoveVisitedEventAsync(message.Chat.Id, eventId);
            await _bot.SendTextMessageAsync(message.Chat.Id, "Событие удалено из списка (visited).");
        }
        else if (listName.Equals("wishlist", StringComparison.OrdinalIgnoreCase))
        {
            await _databaseManager.RemoveWishlistEventAsync(message.Chat.Id, eventId);
            await _bot.SendTextMessageAsync(message.Chat.Id, "Событие удалено из списка (wishlist).");
        }
        else
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Неверное имя списка. Пожалуйста используйте 'visited' или 'wishlist'.");
        }
    }
    private async Task GetWishlist(Message message)
    {
        var wishlistEventIds = await _databaseManager.GetWishlistEventsAsync(message.Chat.Id);
        var events = new List<Event>();

        foreach (var eventId in wishlistEventIds)
        {
            var eventDetails = await _timePadApi.GetEventAsync(eventId);
            if (eventDetails != null)
            {
                events.Add(eventDetails);
            }
        }

        if (events.Count > 0)
        {
            var messageBuilder = new StringBuilder("Список желаний:\n\n");

            foreach (var eventDetails in events)
            {
                messageBuilder.AppendLine($"ID события: {eventDetails.Id}");
                messageBuilder.AppendLine($"Название: {eventDetails.Name.Replace("&quot;", "").Replace("&amp;", "").Replace("quot;", "")}");
                messageBuilder.AppendLine($"URL: {eventDetails.Url}");
                messageBuilder.AppendLine();
            }

            await _bot.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
        }
        else
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Ваш список желаний пуст.");
        }


    }

    private async Task GetVisitedlist(Message message)
    {
        var visitedEventIds = await _databaseManager.GetVisitedEventsAsync(message.Chat.Id);
        var events = new List<Event>();

        foreach (var eventId in visitedEventIds)
        {
            var eventDetails = await _timePadApi.GetEventAsync(eventId);
            if (eventDetails != null)
            {
                events.Add(eventDetails);
            }
        }

        if (events.Count > 0)
        {
            var messageBuilder = new StringBuilder("Посещенные события:\n\n");

            foreach (var eventDetails in events)
            {
                messageBuilder.AppendLine($"ID события: {eventDetails.Id}");
                messageBuilder.AppendLine($"Название: {eventDetails.Name}");
                messageBuilder.AppendLine($"URL: {eventDetails.Url}");
                messageBuilder.AppendLine();
            }

            await _bot.SendTextMessageAsync(message.Chat.Id, messageBuilder.ToString());
        }
        else
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Список посещенных событий пуст.");
        }
    }

    private async Task OnRecommendationsCommand(Message message)
    {

        // Поиск событий в категории "ИТ и интернет", используя последнее событие из объединенного списка
        var similarEvents = await _timePadApi.SearchEventsAsync(category_ids: 452);

        // Отправка рекомендаций пользователю
        if (similarEvents.Count > 0)
        {
            var recommendationsMessage = "Несколько рекомендаций на основе категории ИТ и интернет:\n\n";

            foreach (var similarEvent in similarEvents)
            {
                recommendationsMessage += $"ID события: {similarEvent.Id}\n";
                recommendationsMessage += $"Название: {similarEvent.Name}\n";
                recommendationsMessage += $"URL: {similarEvent.Url}\n\n";
            }

            await _bot.SendTextMessageAsync(message.Chat.Id, recommendationsMessage);
        }
        else
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "No similar events found.");
        }
    }


    public EventBot(string telegramApiKey, string timePadApiKey, string databaseConnectionString)
    {
        _bot = new TelegramBotClient(telegramApiKey);
        _timePadApi = new TimePadApi(timePadApiKey);
        _databaseManager = new DatabaseManager(databaseConnectionString);
    }
}
