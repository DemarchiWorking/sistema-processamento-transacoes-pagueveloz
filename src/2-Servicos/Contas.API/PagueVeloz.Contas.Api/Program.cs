using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PagueVeloz.Contas.Aplicacao.Interfaces;
using PagueVeloz.Contas.Infra.Data;
using PagueVeloz.Contas.Infra.Data.Repositories;
using PagueVeloz.Contas.Aplicacao.Comandos;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;


builder.Services.AddOpenApi();

//#######################

services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CriarContaCommand).Assembly);
});

builder.Services.AddDbContext<ContasDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("ContaConnection")));

services.AddScoped<IClienteRepository, ClienteRepository>();
services.AddScoped<IContaRepository, ContaRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();

services.AddMassTransit(busConfig =>
{
    //barramento [RabbitMQ]
    busConfig.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["MessageBroker:Host"], "/", h =>
        {
            h.Username(configuration["MessageBroker:Username"]);
            h.Password(configuration["MessageBroker:Password"]);
        });

        cfg.ConfigureEndpoints(context);
    });

    //outbox
    //fazer o masstransit usar o EF Core como seu Outbox
    busConfig.AddEntityFrameworkOutbox<ContasDbContext>(o =>
    {
        //Outbox publica evento
        o.UseBusOutbox();

        //configura o banco
        o.UsePostgres();

        //define com que frequencia o worker verificara o outbox
        o.QueryDelay = TimeSpan.FromSeconds(10);
    });
});
//#######################
var app = builder.Build();

//configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
