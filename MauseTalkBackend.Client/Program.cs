using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MauseTalkBackend.Client;
using MauseTalkBackend.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for API communication
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5129/") // Backend API URL
});

// Register application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<IInviteService, InviteService>();

// Register SignalR service
builder.Services.AddScoped(sp =>
{
    var authService = sp.GetRequiredService<AuthService>();
    return new SignalRService(authService, "http://localhost:5129/hub/chat");
});

await builder.Build().RunAsync();
