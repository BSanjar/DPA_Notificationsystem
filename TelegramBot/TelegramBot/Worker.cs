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
		// ��� ������ � ����������� ������ ����. ����� �� ����� ���������, ����� ���� Update �� ����� ��������, Timeout ���� � ��� �����.
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
            _receiverOptions = new ReceiverOptions // ����� �������� �������� ���������� ����
			{
				AllowedUpdates = new[] // ��� ��������� ���� ���������� Update`��, � ��� ��������� ��������� ��� https://core.telegram.org/bots/api#update
				{
					UpdateType.Message, // ��������� (�����, ����/�����, ���������/����� ��������� � �.�.)
				},
				// ��������, ���������� �� ��������� ���������, ��������� �� �� �����, ����� ��� ��� ��� �������
				// True - �� ������������, False (����� �� ���������) - �����������
				ThrowPendingUpdates = true,
			};

			using var cts = new CancellationTokenSource();

			// UpdateHander - ���������� ���������� Update`��
			// ErrorHandler - ���������� ������, ��������� � Bot API
			_botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); // ��������� ����

			var me = await _botClient.GetMeAsync(); // ������� ����������, � ������� �������� ���������� � ����� ����.
			Console.WriteLine($"{me.FirstName} �������!");

			await Task.Delay(-1); // ������������� ����������� ��������, ����� ��� ��� ������� ���������


			//await Task.Delay(1000, stoppingToken);
		}
        //}

        private Dictionary<long, string> WaitingForEmail = new Dictionary<long, string>();


        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			// ����������� ������ ���� try-catch, ����� ��� ��� �� "�����" � ������ �����-���� ������
			try
			{
				
                // ����� �� ������ ����������� switch, ����� ������������ ���������� Update
                switch (update.Type)
				{
					case UpdateType.Message:
						{
							// ��� ���������� ����� ��������� � ���� ��� ��������� � �����������
							var message = update.Message;
							// From - ��� �� ���� ������ ��������� (��� ����� ������ Update)
							var user = message.From;

							// ������� �� ����� ��, ��� ����� ������ ����, � ����� ��������� ���������� �� �����������
							Console.WriteLine(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes($"{user.FirstName} ({user.Id}) ������� ���������: {message.Text}")));

							// Chat - �������� ��� ���������� � ����
							var chat = message.Chat;


                            if (WaitingForEmail.TryGetValue(chat.Id, out string state) && state == "waiting_email")
                            {
								if (Regex.IsMatch(message.Text, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase))
								{
                                    // ������� ��������� ��������
                                    WaitingForEmail.Remove(chat.Id);
                                    // ��������� ����� ����������� ����� � ���� ������ � ���������� �������������
                                    DB db = new DB(_appSettings);

									var idClient = await db.SearchUserByEmail(message.Text);
                                    if (idClient!=null)
                                    {
                                        if (await db.RegAdmin(chat.Id, idClient))
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
												Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("��� �������� ������� ������� �������� � ������� ������"))
												 );
                                            return;
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("��������� ������ ��� ��������. ����������, ���������� ��� ���.")));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("��������� email �� ������. ����������, ���������� ��� ���.")));
                                        return;
                                    }
                                    
								}
								else
								{
                                    await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("��������� email �� ���������. ����������, ������� ���������� email.")));
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
													new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("����������� �� ������� �� �������")))
												}
										   })
											{
												// �������������� ��������� ������� ����������, ���� �� ����� true,
												// ����� ���������� ������������� ���� �� �� �� ����,
												// ��������� ������ ����
												ResizeKeyboard = true,
											};

										await botClient.SendTextMessageAsync(
											chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("����� ����������!")),
											replyMarkup: replyKeyboard); // ����� �������� ���������� � �������� replyMarkup

										return;
									}
									case "����������� �� ������� �� �������":
									{

										await botClient.SendTextMessageAsync(
											chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("������� ��������������� ����� ����������� (������: 2-01507)")));										
										//replyMarkup: replyKeyboard); // ����� �������� ���������� � �������� replyMarkup
										return;
									}
                                case "/admin_reg":
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("������� ����� � �������:")));
                                        // ��������� ��������� ��������, �������� ID ������������ � ������� ���� (�������� ������ ����������� �����)
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
                                                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("�� ������� ����������� �� ������� �� �������: " +message.Text+", ������ ��� ����� ���� ��������� � �������, �� �������� ���������")));
													//replyMarkup: replyKeyboard); // ����� �������� ���������� � �������� replyMarkup
													return;
												}
												else
												{
													var replyKeyboard = new ReplyKeyboardMarkup(
												   new List<KeyboardButton[]>()
												   {
														new KeyboardButton[]
														{
															new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("����������� �� ������� �� �������")))
														}
												   })
													{
														// �������������� ��������� ������� ����������, ���� �� ����� true,
														// ����� ���������� ������������� ���� �� �� �� ����,
														// ��������� ������ ����
														ResizeKeyboard = true,
													};

													await botClient.SendTextMessageAsync(
														chat.Id,
                                                        Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("�� ������� �����������, ���������� ��� ���")),
														replyMarkup: replyKeyboard); // ����� �������� ���������� � �������� replyMarkup

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
															new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("����������� �� ������� �� �������")))
														}
												   })
												{
													// �������������� ��������� ������� ����������, ���� �� ����� true,
													// ����� ���������� ������������� ���� �� �� �� ����,
													// ��������� ������ ����
													ResizeKeyboard = true,
												};

												await botClient.SendTextMessageAsync(
													chat.Id,
                                                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("�� ������� ����� ����������� � ��������������� ������� " +message.Text+" ���������� ��� ���")),
													replyMarkup: replyKeyboard); // ����� �������� ���������� � �������� replyMarkup

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
													new KeyboardButton(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("����������� �� ������� �� �������")))
												}
										   })
											{
												// �������������� ��������� ������� ����������, ���� �� ����� true,
												// ����� ���������� ������������� ���� �� �� �� ����,
												// ��������� ������ ����
												ResizeKeyboard = true,
											};

											await botClient.SendTextMessageAsync(
												chat.Id,
                                                Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("����� ����������!")),
                                                replyMarkup: replyKeyboard); // ����� �������� ���������� � �������� replyMarkup

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
			// ��� �������� ����������, � ������� �������� ��� ������ � � ��������� 
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