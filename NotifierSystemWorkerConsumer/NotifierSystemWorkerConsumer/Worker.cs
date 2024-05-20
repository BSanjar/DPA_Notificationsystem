using Microsoft.Extensions.Options;
using NotifierSystemWorkerConsumer.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace NotifierSystemWorkerConsumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private readonly AppSettings _appSettings;

        public Worker(ILogger<Worker> logger, IConnectionFactory connectionFactory, IOptions<AppSettings> appSettings)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {

                using (var connection = _connectionFactory.CreateConnection())
                {
                    try
                    {
                        using (var channel = connection.CreateModel())
                        {
                            var consumer = new AsyncEventingBasicConsumer(channel);
                            consumer.Received += async (model, es) =>
                            {
                                try
                                {

                                    DB dB = new DB(_appSettings, _logger);

                                    var body = es.Body.ToArray();
                                    var message = JsonSerializer.Deserialize<Message>(body);
                                    var messageString = JsonSerializer.Serialize(message);

                                    //отправка в телеграм бот
                                    if (message.network == "telegram")
                                    {
                                        TelegramBot tgBot = new TelegramBot(_appSettings, _logger);

                                        if (await tgBot.SendMessageAsync(message))
                                        {
                                            message.sended_at = DateTime.Now.ToString();
                                            message.msg_status = "0";
                                            //записываю лог в БД
                                            dB.writeLog(message);
                                            _logger.LogInformation("new msg to telegram: \n" + messageString);
                                           
                                        }
                                        else
                                        {
                                            _logger.LogError("Message was not sended to " + message.receiver_acc);
                                        }
                                        channel.BasicAck(es.DeliveryTag, false);
                                    }
                                    else //отправка на почту
                                    {
                                        EmaiConnector ec = new EmaiConnector(_appSettings, _logger);

                                        if (await ec.sendEmail(message.receiver_acc, message.msgSub, message.msg))
                                        {
                                            message.sended_at = DateTime.Now.ToString();
                                            message.msg_status = "0";
                                            dB.writeLog(message);
                                            _logger.LogInformation("new msg to email: \n" + messageString);
                                           
                                        }
                                        else
                                        {
                                            _logger.LogError("Message was not sended to "+ message.receiver_acc);
                                        }
                                        channel.BasicAck(es.DeliveryTag, false);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "An error occurred while processing a message.");
                                    // Не подтверждаем сообщение, чтобы оно было возвращено в очередь для повторной обработки
                                    channel.BasicNack(es.DeliveryTag, false, true);
                                }

                            };
                            channel.BasicConsume(queue: "telegram-queues", autoAck: false, consumer: consumer);

                            // Ожидаем, пока не будет запрошена отмена или произойдет ошибка
                            await Task.Delay(Timeout.Infinite, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while processing a message.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while consuming messages from the queue.");
            }
            finally
            {
                _logger.LogInformation("Task.Delay completed."); // Логируем завершение ожидания
            }
        }
    }
}
