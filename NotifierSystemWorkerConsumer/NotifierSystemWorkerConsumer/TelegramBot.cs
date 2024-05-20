using NotifierSystemWorkerConsumer.Models;
using Telegram.Bot;

namespace NotifierSystemWorkerConsumer
{

    public class TelegramBot
    {
        private readonly TelegramBotClient _botClient;
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;

        public TelegramBot(AppSettings appSettings, ILogger logger)
        {
            _appSettings = appSettings;
            _botClient = new TelegramBotClient(_appSettings.tgToken);
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync(Message message)
        {
            try
            {
                //отправка в телеграм АПИ
                var result = await _botClient.SendTextMessageAsync(message.receiver_acc, message.msg);
                return true;
            }
            catch (Exception ex)
            {
                //логирование в консоль
                _logger.LogError(ex, "Error in telegram bot");
                return false;
            }
        }
    }
}
