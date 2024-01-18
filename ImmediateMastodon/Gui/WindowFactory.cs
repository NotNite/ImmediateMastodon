using ImmediateMastodon.Gui.Image;

namespace ImmediateMastodon.Gui;

public class WindowFactory(MastodonApi api, ImageCacheService cache) {
    public LoginWindow CreateLoginWindow() => new(api);
    public HomeWindow CreateHomeWindow() => new(api, cache);
}
