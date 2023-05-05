using System;
using System.Threading.Tasks;

namespace TgEventsBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            string telegramApiKey = "" ;
            string timePadApiKey = "";
            string databaseConnectionString = "";

            var eventBot = new EventBot(telegramApiKey, timePadApiKey, databaseConnectionString);
            await eventBot.InitializeBot(telegramApiKey);
        }
    }
}
