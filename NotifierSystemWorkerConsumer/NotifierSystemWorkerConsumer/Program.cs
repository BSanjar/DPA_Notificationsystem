using NotifierSystemWorkerConsumer;
using NotifierSystemWorkerConsumer.Models;
using RabbitMQ.Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        //подключение
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            Endpoint = new AmqpTcpEndpoint(),
            DispatchConsumersAsync = true,
            HostName = "192.168.88.211",
            UserName = "sanjar",
            Password = "883448",

        });

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
