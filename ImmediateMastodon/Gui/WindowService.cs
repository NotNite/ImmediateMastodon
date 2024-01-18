using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImmediateMastodon.Gui;

public class WindowService(
    ImGuiService imgui,
    MastodonApi api,
    IServiceScopeFactory scopeFactory
) : BackgroundService {
    private List<Window> Windows = new();

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => {
        imgui.OnDraw += this.Draw;
        api.OnInitialized += this.CreateWindows;
        api.OnLogin += this.CreateLoggedInWindows;

        stoppingToken.WaitHandle.WaitOne();

        api.OnLogin -= this.CreateLoggedInWindows;
        api.OnInitialized -= this.CreateWindows;
        imgui.OnDraw -= this.Draw;

        foreach (var window in this.Windows) {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (window is IDisposable disposable) disposable.Dispose();
        }
    }, stoppingToken);

    private void CreateWindows() {
        if (api.Api is null) {
            this.Windows.Add(this.CreateWindow(factory => factory.CreateLoginWindow()));
        }
    }

    private void CreateLoggedInWindows() {
        this.Windows.Add(this.CreateWindow(factory => factory.CreateHomeWindow()));
    }

    public void AddWindow(Window window) => this.Windows.Add(window);

    public T CreateWindow<T>(Func<WindowFactory, T> func) {
        using var scope = scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<WindowFactory>();
        return func(factory);
    }

    private void Draw(float deltaTime) {
        foreach (var window in this.Windows) window.DrawInternal();
        this.Windows.RemoveAll(window => window.ShouldRemove);
    }
}
