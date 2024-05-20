using MassTransit;
using MassTransit.Transports.Fabric;
using NotifierSystemWebApiProducer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//builder.Services.AddMassTransit(mass =>
//{
//	mass.UsingRabbitMq((context, cfg) =>
//	{
//		cfg.Host("192.168.88.211", "/", h =>
//		{
//			h.Username("sanjar");
//			h.Password("883448");
//		});

//		//cfg.ReceiveEndpoint("Notificator_Queue", endpoint =>
//		//{
//		//	//	endpoint.ConfigureConsumeTopology = false;

//		//	//	endpoint.ExchangeType = ExchangeType.FanOut.ToString();
//		//	//	endpoint.Bind("exchange-name")

//		//});

//		cfg.Send<Message>(s =>
//		{
//			s.UseRoutingKeyFormatter(context => "telegram");
//		});

//		cfg.ClearSerialization();
//		cfg.UseRawJsonSerializer();
//		cfg.ConfigureEndpoints(context);
//	});

//});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
	app.UseSwagger();
	app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
