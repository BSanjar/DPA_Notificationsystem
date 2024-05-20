using System;
using Telegram.Bot;
using Telegram.Bot.Args;

class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        string botToken = "7191247591:AAFpwGV3RCU1Rd_ADklHcy4GKuTXOhwHpiQ";
        var botClient = new TelegramBotClient(botToken);

        botClient.OnMessage += Bot_OnMessage;
        botClient.StartReceiving();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        botClient.StopReceiving();
    }

    private static async void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        var chatId = e.Message.Chat.Id;
        Console.WriteLine($"Chat ID: {chatId}");
    }
}
