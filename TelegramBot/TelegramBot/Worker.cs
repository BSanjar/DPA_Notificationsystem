using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Models;



namespace TelegramBot
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private readonly TelegramBotClient _botClient;
		private readonly AppSettings _appSettings;
		// Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
		private static ReceiverOptions _receiverOptions;
		public Worker(ILogger<Worker> logger, IOptions<AppSettings> appSettings)
		{
			_logger = logger;
			_appSettings = appSettings.Value;
			_botClient = new TelegramBotClient(_appSettings.tgToken);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
            //while (!stoppingToken.IsCancellationRequested)
            //{

            Console.OutputEncoding = Encoding.UTF8;
            _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
			{
				AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
				{
					UpdateType.Message, // Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
				},
				// Параметр, отвечающий за обработку сообщений, пришедших за то время, когда ваш бот был оффлайн
				// True - не обрабатывать, False (стоит по умолчанию) - обрабаывать
				ThrowPendingUpdates = true,
			};

			using var cts = new CancellationTokenSource();

			// UpdateHander - обработчик приходящих Update`ов
			// ErrorHandler - обработчик ошибок, связанных с Bot API
			_botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); // Запускаем бота

			var me = await _botClient.GetMeAsync(); // Создаем переменную, в которую помещаем информацию о нашем боте.
			Console.WriteLine($"{me.FirstName} запущен!");

			await Task.Delay(-1); // Устанавливаем бесконечную задержку, чтобы наш бот работал постоянно


			//await Task.Delay(1000, stoppingToken);
		}
        //}

        private Dictionary<long, string> WaitingForEmail = new Dictionary<long, string>();


        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			// Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
			try
			{
				
                // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
                switch (update.Type)
				{
					case UpdateType.Message:
						{
							// эта переменная будет содержать в себе все связанное с сообщениями
							var message = update.Message;
							// From - это от кого пришло сообщение (или любой другой Update)
							var user = message.From;

							// Выводим на экран то, что пишут нашему боту, а также небольшую информацию об отправителе
							Console.WriteLine(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}")));

							// Chat - содержит всю информацию о чате
							var chat = message.Chat;


                            if (WaitingForEmail.TryGetValue(chat.Id, out string state) && state == "waiting_email")
                            {
								if (Regex.IsMatch(message.Text, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase))
								{
                                    // Удаляем состояние ожидания
                                    WaitingForEmail.Remove(chat.Id);
                                    // Сохраняем адрес электронной почты в базу данных и отправляем подтверждение
                                    DB db = new DB(_appSettings);

									var idClient = await db.SearchUserByEmail(message.Text);
                                    if (idClient!=null)
                                    {
                                        if (await db.RegAdmin(chat.Id, idClient))
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
												Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Ваш телеграм аккаунт успешно привязан в систему Реестр"))
												 );
                                            return;
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Произошла ошибка при привязке. Пожалуйста, попробуйте еще раз.")));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Указанный email не найден. Пожалуйста, попробуйте еще раз.")));
                                        return;
                                    }
                                    
								}
								else
								{
                                    await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Указанный email не корректен. Пожалуйста, укажите корректный email.")));
                                }
                                return;
                            }
							else
                            switch (message.Text)
							{
								case "/start":
									{
										var replyKeyboard = new ReplyKeyboardMarkup(
										   new List<KeyboardButton[]>()
										   {
												new KeyboardButton[]
												{
													new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Подписаться на события по реестру")))
												}
										   })
											{
												// автоматическое изменение размера клавиатуры, если не стоит true,
												// тогда клавиатура растягивается чуть ли не до луны,
												// проверить можете сами
												ResizeKeyboard = true,
											};

										await botClient.SendTextMessageAsync(
											chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Добро пожаловать!")),
											replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup

										return;
									}
									case "Подписаться на события по реестру":
									{

										await botClient.SendTextMessageAsync(
											chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Введите регистрационный номер организации (пример: 2-01507)")));										
										//replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup
										return;
									}
                                case "/admin_reg":
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Введите логин в Реестре:")));
                                        // Добавляем состояние ожидания, указывая ID пользователя и текущий этап (ожидание адреса электронной почты)
                                        WaitingForEmail[chat.Id] = "waiting_email";
                                        return;
                                    }




                                default: {

										

                                            if (message.Text.Length == 7 && message.Text[1] == '-')
										{
											DB db = new DB(_appSettings);
											if(await db.SearchInDatabase(message.Text))
											{
												if (await db.SaveToDatabase(chat.Id, message.Text))
												{
													await botClient.SendTextMessageAsync(
													chat.Id,
                                                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Вы успешно подписались на события по реестру: " +message.Text+", теперь при каких либо изменений в реестре, вы получите сообщение")));
													//replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup
													return;
												}
												else
												{
													var replyKeyboard = new ReplyKeyboardMarkup(
												   new List<KeyboardButton[]>()
												   {
														new KeyboardButton[]
														{
															new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Подписаться на события по реестру")))
														}
												   })
													{
														// автоматическое изменение размера клавиатуры, если не стоит true,
														// тогда клавиатура растягивается чуть ли не до луны,
														// проверить можете сами
														ResizeKeyboard = true,
													};

													await botClient.SendTextMessageAsync(
														chat.Id,
                                                        Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Не удалось подписаться, попробуйте еще раз")),
														replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup

													return;
												}										

											}
											else
											{
												var replyKeyboard = new ReplyKeyboardMarkup(
												   new List<KeyboardButton[]>()
												   {
														new KeyboardButton[]
														{
															new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Подписаться на события по реестру")))
														}
												   })
												{
													// автоматическое изменение размера клавиатуры, если не стоит true,
													// тогда клавиатура растягивается чуть ли не до луны,
													// проверить можете сами
													ResizeKeyboard = true,
												};

												await botClient.SendTextMessageAsync(
													chat.Id,
                                                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Не удалось найти организацию с регистрационным номером " +message.Text+" попробуйте еще раз")),
													replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup

												return;
											}


											
										}
										else
										{
											var replyKeyboard = new ReplyKeyboardMarkup(
										   new List<KeyboardButton[]>()
										   {
												new KeyboardButton[]
												{
													new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Подписаться на события по реестру")))
												}
										   })
											{
												// автоматическое изменение размера клавиатуры, если не стоит true,
												// тогда клавиатура растягивается чуть ли не до луны,
												// проверить можете сами
												ResizeKeyboard = true,
											};

											await botClient.SendTextMessageAsync(
												chat.Id,
                                                Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Добро пожаловать!")),
                                                replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup

											return;
										}
									} 
							}

						}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
		{
			// Тут создадим переменную, в которую поместим код ошибки и её сообщение 
			var ErrorMessage = error switch
			{
				ApiRequestException apiRequestException
					=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => error.ToString()
			};

			Console.WriteLine(ErrorMessage);
			return Task.CompletedTask;
		}
	}
}