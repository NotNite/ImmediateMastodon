using ImGuiNET;

namespace ImmediateMastodon.Gui;

public class LoginWindow(MastodonApi api) : Window("Login") {
    private string instance = string.Empty;
    private string code = string.Empty;
    private bool waitingForCode = false;
    private bool locked = false;

    protected override void Draw() {
        Utils.Locked(this.locked, () => {
            if (this.waitingForCode) {
                var pressed = ImGui.InputText("Code", ref this.code, 256, ImGuiInputTextFlags.EnterReturnsTrue)
                              || ImGui.Button("Login");

                if (pressed) {
                    Task.Run(async () => {
                        this.locked = true;
                        try {
                            await api.LoginWithCode(this.code);
                            this.ShouldRemove = true;
                        } finally {
                            this.locked = false;
                        }
                    });
                }

                ImGui.SameLine();

                if (ImGui.Button("Back")) this.waitingForCode = false;
            } else {
                ImGui.TextUnformatted("Welcome to ImmediateMastodon!");
                ImGui.TextUnformatted("To begin, enter your instance and login with your web browser.");

                var pressed = ImGui.InputText("Instance", ref this.instance, 256, ImGuiInputTextFlags.EnterReturnsTrue)
                              || ImGui.Button("Get code");

                if (pressed) {
                    Task.Run(async () => {
                        this.locked = true;
                        try {
                            await api.Create(this.instance);
                            api.OpenOAuthUrl();
                            this.waitingForCode = true;
                        } finally {
                            this.locked = false;
                        }
                    });
                }
            }
        });
    }
}
