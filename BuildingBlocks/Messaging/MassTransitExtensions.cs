using MassTransit;
using Microsoft.Extensions.DependencyInjection;


namespace BuildingBlocks.Messaging
{
    public static class MassTransitExtensions
    {
        public static IServiceCollection AddRabbitMqWithConsumers(this IServiceCollection services, Action<IBusRegistrationConfigurator> configAction)
        {
            services.AddMassTransit(x =>
            {
                configAction(x);

                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host("rabbitmq", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ConfigureEndpoints(ctx);
                });
            });

            return services;
        }
    }
}
