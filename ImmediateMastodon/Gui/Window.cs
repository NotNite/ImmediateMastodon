using ImGuiNET;
using Serilog;

namespace ImmediateMastodon.Gui;

public abstract class Window(string name) {
    public ImGuiWindowFlags Flags { get; set; }
    public bool ShouldRemove { get; set; }
    public bool ShouldDraw { get; set; } = true;

    internal void DrawInternal() {
        try {
            this.PreDraw();
        } catch (Exception e) {
            Log.Error(e, "Failed to run pre draw for window {Name}", name);
        }

        if (this.ShouldDraw) {
            if (ImGui.Begin(name, this.Flags)) {
                try {
                    this.Draw();
                } catch (Exception e) {
                    Log.Error(e, "Failed to draw window {Name}", name);
                }
            }
            ImGui.End();
        }

        try {
            this.PostDraw();
        } catch (Exception e) {
            Log.Error(e, "Failed to run post draw for window {Name}", name);
        }
    }

    protected virtual void PreDraw() { }
    protected abstract void Draw();
    protected virtual void PostDraw() { }
}
