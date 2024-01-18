using System.Numerics;
using Veldrid;

namespace ImmediateMastodon.Gui.Image;

public class CachedImage(ImGuiService imgui) {
    public Texture? Texture;

    public nint? Handle {
        get {
            if (this.Texture is null) return null;
            this.LastAccessed = DateTime.Now;
            return imgui.GetTextureHandle(this.Texture);
        }
    }

    public DateTime LastAccessed = DateTime.Now;
    public Vector2 Size => this.Texture is null ? Vector2.Zero : new Vector2(this.Texture.Width, this.Texture.Height);
}
