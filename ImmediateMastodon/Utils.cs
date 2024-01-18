using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using ImGuiNET;
using ImmediateMastodon.Gui;
using ImmediateMastodon.Gui.Image;

namespace ImmediateMastodon;

public class Utils {
    private static ReverseMarkdown.Converter MarkdownHtmlConverter = new();
    private static MarkdownRenderer MarkdownRenderer = new();

    public static void OpenUrl(string url) {
        switch (Environment.OSVersion.Platform) {
            case PlatformID.Win32NT:
                Process.Start(new ProcessStartInfo(url) {
                    UseShellExecute = true
                });
                break;

            case PlatformID.Unix:
                Process.Start("xdg-open", url);
                break;

            case PlatformID.MacOSX:
                Process.Start("open", url);
                break;
        }
    }

    public static void Locked(bool locked, Action func) {
        try {
            if (locked) ImGui.BeginDisabled();
            func();
        } finally {
            if (locked) ImGui.EndDisabled();
        }
    }

    public static void LoadingImage(CachedImage img, Vector2 size) {
        if (img.Handle is { } handle) {
            ImGui.Image(handle, size);
        } else {
            ImGui.Dummy(size);
        }
    }

    public static string TextEscape(string str) => str.Replace("%", "%%");

    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public static unsafe void WrapText(
        string csText, float lineWidth, Action? onClick = null, Action? onHover = null
    ) {
        if (csText.Length == 0) {
            return;
        }

        foreach (var part in csText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)) {
            var bytes = Encoding.UTF8.GetBytes(part);
            fixed (byte* rawText = bytes) {
                var text = rawText;
                var textEnd = text + bytes.Length;

                // empty string
                if (text == null) {
                    ImGui.Dummy(Vector2.Zero);
                    continue;
                }

                var widthLeft = ImGui.GetContentRegionAvail().X;
                var endPrevLine =
                    ImGuiNative.ImFont_CalcWordWrapPositionA(ImGui.GetFont().NativePtr, 1, text,
                                                             textEnd, widthLeft);
                if (endPrevLine == null) {
                    continue;
                }

                var firstSpace = FindFirstSpace(text, textEnd);
                var properBreak = firstSpace <= endPrevLine;
                if (properBreak) {
                    WithActions(onClick, onHover, () => ImGuiNative.igTextUnformatted(text, endPrevLine));
                } else {
                    if (lineWidth == 0f) {
                        ImGui.Dummy(Vector2.Zero);
                    } else {
                        // check if the next bit is longer than the entire line width
                        var wrapPos = ImGuiNative.ImFont_CalcWordWrapPositionA(
                            ImGui.GetFont().NativePtr, 1, text, firstSpace, lineWidth);
                        if (wrapPos >= firstSpace) {
                            // only go to next line if it's going to wrap at the space
                            ImGui.Dummy(Vector2.Zero);
                        }
                    }
                }

                widthLeft = ImGui.GetContentRegionAvail().X;
                while (endPrevLine < textEnd) {
                    if (properBreak) {
                        text = endPrevLine;
                    }

                    if (*text == ' ') {
                        ++text;
                    } // skip a space at start of line

                    var newEnd =
                        ImGuiNative.ImFont_CalcWordWrapPositionA(ImGui.GetFont().NativePtr, 1,
                                                                 text, textEnd, widthLeft);
                    if (properBreak && newEnd == endPrevLine) {
                        break;
                    }

                    endPrevLine = newEnd;
                    if (endPrevLine == null) {
                        ImGui.Dummy(Vector2.Zero);
                        ImGui.Dummy(Vector2.Zero);
                        break;
                    }

                    WithActions(onClick, onHover, () => ImGuiNative.igTextUnformatted(text, endPrevLine));

                    if (!properBreak) {
                        properBreak = true;
                        widthLeft = ImGui.GetContentRegionAvail().X;
                    }
                }
            }
        }
    }

    private static unsafe byte* FindFirstSpace(byte* text, byte* textEnd) {
        for (var i = text; i < textEnd; i++) {
            if (char.IsWhiteSpace((char) *i)) {
                return i;
            }
        }

        return textEnd;
    }

    private static void WithActions(Action? onClick, Action? onHover, Action action) {
        action();

        if (onHover != null && ImGui.IsItemHovered()) {
            onHover();
        }

        if (onClick != null && ImGui.IsItemClicked()) {
            onClick();
        }
    }

    public static void Tooltip(string text, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None) {
        if (!ImGui.IsItemHovered(flags)) {
            return;
        }

        var w = ImGui.CalcTextSize("m").X;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(w * 40);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    public static void RenderHtml(string html) {
        var markdown = MarkdownHtmlConverter.Convert(html);
        if (markdown == null) {
            WrapText(html, ImGui.GetContentRegionAvail().X);
        } else {
            RenderMarkdown(markdown);
        }
    }

    public static void RenderMarkdown(string markdown) {
        var document = Markdig.Markdown.Parse(markdown);
        MarkdownRenderer.Render(document);
    }
}
