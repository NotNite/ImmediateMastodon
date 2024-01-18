using ImmediateMastodon;
using ImmediateMastodon.Gui;
using ImmediateMastodon.Gui.Image;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

if (!Directory.Exists(Config.DataDirectory)) {
    Directory.CreateDirectory(Config.DataDirectory);
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(Config.DataDirectory, "immediate-mastodon.log"))
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

var config = Config.Load();
builder.Services.AddSingleton(config);

void AddSingletonHostedService<T>() where T : class, IHostedService {
    builder.Services.AddSingleton<T>();
    builder.Services.AddHostedService<T>(p => p.GetRequiredService<T>());
}

AddSingletonHostedService<ImGuiService>();
AddSingletonHostedService<WindowService>();
AddSingletonHostedService<MastodonApi>();

builder.Services.AddSingleton<ImageCacheService>();
builder.Services.AddTransient<WindowFactory>();

using var host = builder.Build();

try {
    await host.RunAsync();
} catch (Exception e) {
    Log.Fatal(e, "Host terminated unexpectedly");
} finally {
    Log.CloseAndFlush();
}
