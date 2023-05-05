using System;
using System.Threading.Tasks;

namespace TgEventsBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            string telegramApiKey = "6055948934:AAEW4SxMgq0gdtyGps1BZL3wLxY9zJZAHm0" ;
            string timePadApiKey = "ca822d0bacf22ed61c80e1e3363d91e4e8383294";
            string databaseConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=1234";

            var eventBot = new EventBot(telegramApiKey, timePadApiKey, databaseConnectionString);
            await eventBot.InitializeBot(telegramApiKey);
        }
    }
}
