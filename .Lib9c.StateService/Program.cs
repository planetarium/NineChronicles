using Bencodex;
using Libplanet.Action.State;
using Libplanet.Extensions.RemoteBlockChainStates;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Codec>();

builder.Services.AddSingleton<IBlockChainStates, RemoteBlockChainStates>(_ =>
{
    const string DefaultEndpoint = "http://localhost:31280/graphql/explorer";
    var endpoint = builder.Configuration.GetValue<string>("RemoteBlockChainStatesEndpoint") ?? DefaultEndpoint;
    return new RemoteBlockChainStates(new Uri(endpoint));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
