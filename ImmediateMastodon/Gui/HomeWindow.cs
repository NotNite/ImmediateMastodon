using System.Numerics;
using ImGuiNET;
using ImmediateMastodon.Gui.Image;
using ImmediateMastodon.Gui.Raii;
using Mastonet.Entities;

namespace ImmediateMastodon.Gui;

public class HomeWindow : Window {
    private ImageCacheService cache;
    private MastodonList<Status> statuses = new();

    public HomeWindow(MastodonApi api, ImageCacheService cache) : base("Home") {
        this.cache = cache;
        Task.Run(async () => { this.statuses = await api.Api!.GetHomeTimeline(); });
    }

    protected override void Draw() {
        foreach (var status in this.statuses) {
            if (status.Content == string.Empty) continue;

            using (ImRaii.PushId(status.Id)) {
                var lineHeight = ImGui.GetTextLineHeight();
                var spacing = ImGui.GetStyle().ItemSpacing.Y;
                var size = (lineHeight * 2) + spacing;

                var img = this.cache.GetImage(status.Account.AvatarUrl);
                Utils.LoadingImage(img, new Vector2(size, size));
                ImGui.SameLine();

                var posPrev = ImGui.GetCursorPos();
                ImGui.TextUnformatted(status.Account.DisplayName);
                var posNext = posPrev + new Vector2(0, lineHeight + spacing);
                ImGui.SetCursorPos(posNext);

                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1))) {
                    ImGui.TextUnformatted(status.Account.AccountName);
                }

                ImGui.NewLine();
                ImGui.SameLine();
                Utils.RenderHtml(status.Text ?? status.Content);

                foreach (var media in status.MediaAttachments) {
                    ImGui.NewLine();
                    ImGui.SameLine();
                    var mediaImg = this.cache.GetImage(media.Url);
                    var cra = ImGui.GetContentRegionAvail();
                    var maxWidth = cra.X;
                    var maxHeight = lineHeight * 30;

                    var attachmentSize = mediaImg.Size;
                    var ratio = mediaImg.Size.X / mediaImg.Size.Y;

                    if (attachmentSize.X > ImGui.GetContentRegionAvail().X) {
                        attachmentSize = new Vector2(maxWidth, maxWidth / ratio);
                    }

                    if (attachmentSize.Y > maxHeight) {
                        attachmentSize = new Vector2(maxHeight * ratio, maxHeight);
                    }


                    Utils.LoadingImage(mediaImg, attachmentSize);
                }

                ImGui.Separator();
            }
        }
    }
}
