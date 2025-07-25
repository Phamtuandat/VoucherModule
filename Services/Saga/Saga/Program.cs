using SagaService.Sagas;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRabbitMqWithConsumers(cfg =>
{
    cfg.AddSagaStateMachine<UserRegistrationStateMachine, UserRegistrationState>()
     .InMemoryRepository();
}, builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
