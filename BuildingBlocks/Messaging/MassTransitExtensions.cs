using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace BuildingBlocks.Messaging
{
    public static class MassTransitExtensions
    {
        public static IServiceCollection AddRabbitMqWithConsumers(this IServiceCollection services, Action<IBusRegistrationConfigurator> configAction, IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                configAction(x);

                x.UsingRabbitMq((ctx, cfg) =>
                {
                    Console.WriteLine("Configuring RabbitMQ...");
                    Console.WriteLine($"Host: {configuration["RabbitMq:Host"]}");
                    Console.WriteLine($"Username: {configuration["RabbitMq:Username"]}");
                    Console.WriteLine($"Password: {configuration["RabbitMq:Password"]}");
                    cfg.Host(configuration["RabbitMq:Host"], "/", h =>
                    {
                        h.Username(configuration["RabbitMq:Username"]);
                        h.Password(configuration["RabbitMq:Password"]);
                    });
                    Console.WriteLine("RabbitMQ configured successfully.");
                    cfg.ConfigureEndpoints(ctx);
                });
            });

            return services;
        }
    }
}
