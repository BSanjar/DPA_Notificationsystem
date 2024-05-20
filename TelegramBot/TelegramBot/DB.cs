using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Models;

namespace TelegramBot
{
	public class DB
	{
		private static string connectionString = "";
		private readonly AppSettings _appSettings;
		public DB(AppSettings appSettings) {
			_appSettings = appSettings;

			connectionString = _appSettings.Connection;
		}

		public async Task<bool> SearchInDatabase(string regnumber)
		{
			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var cmd = new NpgsqlCommand())
				{
					cmd.Connection = connection;
					cmd.CommandText = "SELECT id FROM public.n_register WHERE regnumber = @regnumber";
					cmd.Parameters.AddWithValue("@regnumber", regnumber);

					// Выполняем запрос и получаем результат
					var result = await cmd.ExecuteScalarAsync();

					// Проверяем, были ли найдены данные
					if (result != null && result != DBNull.Value)
					{
						// Данные найдены
						return true;
					}
					else
					{
						// Данные не найдены
						return false;
					}
				}
			}
		}

		public async Task<bool> SaveToDatabase(long chatId, string regnumber)
		{
			try
			{
				using (var connection = new NpgsqlConnection(connectionString))
				{
				    await connection.OpenAsync();

					using (var cmd = new NpgsqlCommand())
					{
						cmd.Connection = connection;
						cmd.CommandText = "UPDATE public.n_register SET person_telegramid='" + chatId.ToString() + "' WHERE regnumber='" + regnumber + "';";
						 
						var r = await cmd.ExecuteNonQueryAsync();
						if(r<1)
						{
							return false;
						}
						else { return true; }
					}
				}
			}
			catch {return false; }
		}

        public async Task<string> SearchUserByEmail(string email)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = "SELECT id FROM public.users WHERE email = @email";
                    cmd.Parameters.AddWithValue("@email", email);

                    // Выполняем запрос и получаем результат
                    var result = await cmd.ExecuteScalarAsync ();

                    // Проверяем, были ли найдены данные
                    if (result != null && result != DBNull.Value)
                    {
                        // Данные найдены
                        return result.ToString();
                    }
                    else
                    {
                        // Данные не найдены
                        return null;
                    }
                }
            }
        }

        public async Task<bool> RegAdmin(long chatId, string id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync(); // Открытие соединения асинхронно

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        //cmd.CommandText = "UPDATE public.n_register SET person_telegramid='" + chatId.ToString() + "' WHERE regnumber='" + regnumber + "';";


                        cmd.CommandText = "UPDATE public.users SET telegram_id='"+chatId.ToString()+"' WHERE id='"+id+"';";
                      

                        // Установка времени ожидания выполнения запроса
                        //cmd.CommandTimeout = 30; // Пример: установка таймаута на 30 секунд

                        var r = await cmd.ExecuteNonQueryAsync();
                        if (r < 1)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return false;
            }
        }


    }
}
