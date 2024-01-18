using System.Timers;
using Timer = System.Timers.Timer;

namespace ImmediateMastodon.Gui.Image;

public class ImageCacheService : IDisposable {
    private ImGuiService imgui;
    private MastodonApi api;
    private Timer cleanupTimer;

    private Dictionary<string, CachedImage> urlCache = new();

    public ImageCacheService(ImGuiService imgui, MastodonApi api) {
        this.imgui = imgui;
        this.api = api;

        this.cleanupTimer = new Timer();
        this.cleanupTimer.Elapsed += this.Cleanup;
        this.cleanupTimer.Interval = 10000;
        this.cleanupTimer.Start();
    }


    public void Dispose() {
        this.cleanupTimer.Dispose();
    }

    public CachedImage GetImage(string url) {
        if (this.urlCache.TryGetValue(url, out var existingImage)) {
            return existingImage;
        }

        var cachedImage = new CachedImage(this.imgui);
        this.urlCache.Add(url, cachedImage);

        Task.Run(async () => {
            var data = await this.api.Client.GetByteArrayAsync(url);
            var texture = this.imgui.LoadImage(data);
            cachedImage.Texture = texture;
        });

        return cachedImage;
    }

    private void Cleanup(object? sender, ElapsedEventArgs args) {
        foreach (var (key, value) in this.urlCache.ToArray()) {
            if (value.LastAccessed < DateTime.Now - TimeSpan.FromSeconds(30)) {
                this.urlCache.Remove(key);
                if (value.Texture is not null) {
                    this.imgui.RemoveTextureHandle(value.Texture);
                    value.Texture.Dispose();
                }
            }
        }
    }
}
