using NotifierSystemWorkerConsumer.Models;
using Npgsql;

namespace NotifierSystemWorkerConsumer
{
    public class DB
    {
        private readonly AppSettings _appSettings;
        private readonly string connectionString;
        private readonly ILogger _logger;
        public DB(AppSettings appSettings, ILogger logger)
        {
            _appSettings = appSettings;
            connectionString = _appSettings.Connection;
            _logger = logger;
        }
        public async Task<bool> writeLog(Models.Message message)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = "INSERT INTO public.msg_quee " +
                                    "(id, processid, created_at, sended_at, msg, registry, network, sender_acc, receiver_acc, msg_status) " +
                                    "VALUES(@id, @processid, @created_at, @sended_at, @msg, @registry, @network, @sender_acc, @receiver_acc, @msg_status); ";

                        cmd.Parameters.AddWithValue("@id", message.id);
                        cmd.Parameters.AddWithValue("@processid", message.processid);
                        cmd.Parameters.AddWithValue("@created_at", message.created_at);
                        cmd.Parameters.AddWithValue("@sended_at", message.sended_at);
                        cmd.Parameters.AddWithValue("@msg", message.msg);
                        cmd.Parameters.AddWithValue("@registry", message.registry);
                        cmd.Parameters.AddWithValue("@network", message.network);
                        cmd.Parameters.AddWithValue("@sender_acc", message.sender_acc);
                        cmd.Parameters.AddWithValue("@receiver_acc", message.receiver_acc);
                        cmd.Parameters.AddWithValue("@msg_status", message.msg_status);

                        var r = cmd.ExecuteNonQuery();
                        if (r < 1)
                        {
                            return false;
                        }
                        else { return true; }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in write log to DB ");
                return false;
            }

        }
    }
}
