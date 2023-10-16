using Bencodex;
using Libplanet.RocksDBStore;
using Libplanet.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Codec>();

builder.Services.AddSingleton<IStateStore, TrieStateStore>(_ =>
{
    var path = builder.Configuration.GetValue<string>("StateStorePath");
    return new TrieStateStore(new RocksDBKeyValueStore(path));
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
