using MassTransit;
using Microsoft.EntityFrameworkCore;
using PagueVeloz.CoreFinanceiro.Aplicacao.Eventos; //consumer
using PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces;
using PagueVeloz.CoreFinanceiro.Infra.Data;
using PagueVeloz.CoreFinanceiro.Infra.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();

services.AddDbContext<CoreFinanceiroDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("FinanceiroConnection")));

builder.Services.AddOpenApi();
services.AddScoped<IContaRepository, ContaRepository>(); //implementacao d infra
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<ITransacaoProcessadaRepository, TransacaoProcessadaRepository>();

services.AddDbContext<CoreFinanceiroDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(PagueVeloz.CoreFinanceiro.Aplicacao.Interfaces.IApplicationMarker).Assembly)); // (Crie uma interface 'IApplicationMarker' na Aplicação)

services.AddMassTransit(busConfig =>
{

    busConfig.AddConsumer<ContaCriadaConsumer>();

    //registra outbox // p o _publishEndpoint.Publish()
    busConfig.AddEntityFrameworkOutbox<CoreFinanceiroDbContext>(o =>
    {
        o.UseBusOutbox();
        o.UsePostgres();
    });


    busConfig.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["MessageBroker:Host"], "/", h =>
        {
            h.Username(configuration["MessageBroker:Username"]);
            h.Password(configuration["MessageBroker:Password"]);
        });

        cfg.ReceiveEndpoint("corefinanceiro-conta-criada", e =>
        {
            //middleware do inbox (idempotentenc)
            e.UseEntityFrameworkOutbox<CoreFinanceiroDbContext>(context);

            //conecta o consumidor a esta fila
            e.ConfigureConsumer<ContaCriadaConsumer>(context);
        });
    });
});

var app = builder.Build();


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


/*
 * 
 * 
services.AddMassTransit(busConfig =>
{
    busConfig.AddEntityFrameworkInbox<CoreFinanceiroDbContext>();

    busConfig.AddConsumer<ContaCriadaConsumer>();

    busConfig.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["MessageBroker:Host"], "/", h =>
        {
            h.Username(configuration["MessageBroker:Username"]);
            h.Password(configuration["MessageBroker:Password"]);
        });

        cfg.ReceiveEndpoint("corefinanceiro-conta-criada", e =>
        {
            e.UseEntityFrameworkOutbox<CoreFinanceiroDbContext>(context);

            e.ConfigureConsumer<ContaCriadaConsumer>(context);
        });
    });

    busConfig.AddEntityFrameworkOutbox<CoreFinanceiroDbContext>(o =>
    {
        o.UseBusOutbox();
        o.UsePostgres();
    });
});

*/