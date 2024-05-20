using Microsoft.AspNetCore.Mvc;
using NotifierSystemWebApiProducer.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NotifierSystemWebApiProducer.Controllers
{

	[Route("[controller]")]
	[ApiController]
	public class ApiController : ControllerBase
	{
		private readonly ILogger<ApiController> _logger;


		public ApiController(ILogger<ApiController> logger)
		{
			_logger = logger;
		}


		/// <summary>
		/// АПИ для отправки сообщений в очередь rabbitMQ.
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		[Route("Produce")]
		public async Task<IActionResult> Produce([FromBody] Message message)
		{
			try
			{
				var factory = new ConnectionFactory() { HostName = "192.168.88.211", UserName= "sanjar", Password = "883448" };
				
				using (var connection = factory.CreateConnection())
				{
					using (var chanel = connection.CreateModel())
					{
						chanel.BasicPublish(
							new PublicationAddress("topic", "push.notifications", "telegram"),
							null,
							JsonSerializer.SerializeToUtf8Bytes(message)
							);					
					}
				}
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);	
			}			
		}
	}
}
