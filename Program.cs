using A2A.Server;
using A2A.Server.AspNetCore;
using Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureService(builder.Configuration);

// A2A Server configuration
builder.Services.AddA2AWellKnownAgent((provider, builder) =>
{
    builder
        .WithName("Aarvi")
        .WithDescription("Your personal assistant")
        .WithVersion("1.0.0.0")
        .WithProvider(provider => provider
            .WithOrganization("Vikas Sharma")
            .WithUrl(new("https://github.com/vikas0sharma")))
        .WithUrl(new("/a2a", UriKind.Relative))
        .SupportsStreaming()
        .WithSkill(skill => skill
            .WithId("youtube-music")
            .WithName("YouTubeMusic")
            .WithDescription("Interacts with YouTube Music."));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapA2AWellKnownAgentEndpoint();
app.MapA2AHttpEndpoint("/a2a");

app.UseHttpsRedirection();

app.UseWebSockets(); // Enable WebSocket support

app.UseAuthorization();

app.MapControllers();
app.UseStaticFiles();
app.Run();
