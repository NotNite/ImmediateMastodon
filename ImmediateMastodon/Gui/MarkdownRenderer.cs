using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ImmediateMastodon.Gui;

internal class MarkdownRenderer : RendererBase {
    private float verticalSpacing;
    private bool lastWasInline;
    private Action? onHover;
    private Action? onClick;
    private bool ignoreInline;
    private bool addedSpacing;

    internal MarkdownRenderer() {
        this.ObjectRenderers.AddRange(new IMarkdownObjectRenderer[] {
            // blocks
            new CodeBlockRenderer(),
            new HeadingBlockRenderer(),
            new ListRenderer(),
            new ParagraphRenderer(),
            new QuoteBlockRenderer(),
            new ThematicBreakRenderer(),

            // inlines
            new AutolinkInlineRenderer(),
            new CodeInlineRenderer(),
            new EmphasisInlineRenderer(),
            new LineBreakInlineRenderer(),
            new LinkInlineRenderer(),
            new LiteralInlineRenderer(),
        });

        this.ObjectWriteBefore += this.BeforeWrite;
        this.ObjectWriteAfter += this.AfterWrite;
    }

    private void AfterWrite(IMarkdownRenderer _, MarkdownObject obj) {
        if (obj is not Block) {
            this.addedSpacing = false;
            return;
        }

        if (this.addedSpacing) {
            return;
        }

        this.addedSpacing = true;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, this.verticalSpacing));
        try {
            ImGui.Spacing();
        } finally {
            ImGui.PopStyleVar();
        }
    }

    private void BeforeWrite(IMarkdownRenderer _, MarkdownObject obj) {
        var isInline = obj is Inline && obj.GetType() != typeof(ContainerInline);
        if (!this.ignoreInline && this.lastWasInline && isInline) {
            ImGui.SameLine();
        }

        this.lastWasInline = isInline;
    }

    public override object Render(MarkdownObject obj) {
        this.verticalSpacing = ImGui.GetStyle().ItemSpacing.Y;
        this.lastWasInline = false;
        this.addedSpacing = false;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        try {
            this.Write(obj);
        } finally {
            ImGui.PopStyleVar();
        }

        return null!;
    }

    private void WriteLeafBlock(LeafBlock leafBlock) {
        var slices = leafBlock.Lines.Lines;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (slices == null) {
            return;
        }

        for (var i = 0; i < slices.Length; i++) {
            ref var slice = ref slices[i].Slice;
            if (slice.Text is null) {
                break;
            }

            this.Write(new LiteralInline(slice));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteLeafInline(LeafBlock leafBlock) {
        Inline? inline = leafBlock.Inline;

        while (inline != null) {
            this.Write(inline);
            inline = inline.NextSibling;
        }
    }

    private class CodeBlockRenderer : MarkdownObjectRenderer<MarkdownRenderer, CodeBlock> {
        protected override void Write(MarkdownRenderer renderer, CodeBlock obj) {
            renderer.ignoreInline = true;
            try {
                renderer.WriteLeafBlock(obj);
            } finally {
                renderer.ignoreInline = false;
            }
        }
    }

    private class ListRenderer : MarkdownObjectRenderer<MarkdownRenderer, ListBlock> {
        protected override void Write(MarkdownRenderer renderer, ListBlock obj) {
            if (obj.IsOrdered) {
                for (var i = 0; i < obj.Count; i++) {
                    var item = (ListItemBlock) obj[i];
                    ImGui.TextUnformatted($"{i + 1}{obj.OrderedDelimiter} ");
                    renderer.lastWasInline = true;
                    renderer.WriteChildren(item);
                }
            } else {
                foreach (var item in obj) {
                    ImGui.TextUnformatted("• ");
                    renderer.lastWasInline = true;
                    renderer.WriteChildren((ListItemBlock) item);
                }
            }
        }
    }

    private class ParagraphRenderer : MarkdownObjectRenderer<MarkdownRenderer, ParagraphBlock> {
        protected override void Write(MarkdownRenderer renderer, ParagraphBlock obj) {
            renderer.WriteLeafInline(obj);
        }
    }

    private class ThematicBreakRenderer : MarkdownObjectRenderer<MarkdownRenderer, ThematicBreakBlock> {
        protected override void Write(MarkdownRenderer renderer, ThematicBreakBlock obj) {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, renderer.verticalSpacing));
            try {
                ImGui.Separator();
            } finally {
                ImGui.PopStyleVar();
            }
        }
    }

    private class QuoteBlockRenderer : MarkdownObjectRenderer<MarkdownRenderer, QuoteBlock> {
        protected override void Write(MarkdownRenderer renderer, QuoteBlock obj) {
            ImGui.Indent();
            try {
                renderer.WriteChildren(obj);
            } finally {
                ImGui.Unindent();
            }
        }
    }

    private class HeadingBlockRenderer : MarkdownObjectRenderer<MarkdownRenderer, HeadingBlock> {
        protected override void Write(MarkdownRenderer renderer, HeadingBlock obj) {
            renderer.WriteLeafInline(obj);
        }
    }

    private class LiteralInlineRenderer : MarkdownObjectRenderer<MarkdownRenderer, LiteralInline> {
        protected override void Write(MarkdownRenderer renderer, LiteralInline obj) {
            Utils.WrapText(
                obj.Content.ToString(),
                ImGui.GetContentRegionAvail().X,
                renderer.onClick,
                renderer.onHover
            );
        }
    }

    private class EmphasisInlineRenderer : MarkdownObjectRenderer<MarkdownRenderer, EmphasisInline> {
        protected override void Write(MarkdownRenderer renderer, EmphasisInline obj) {
            renderer.WriteChildren(obj);
        }
    }

    private class LinkInlineRenderer : MarkdownObjectRenderer<MarkdownRenderer, LinkInline> {
        private void InternalWrite(MarkdownRenderer renderer, LinkInline obj) {
            ImGui.PushStyleColor(ImGuiCol.Text, Constants.LinkColor);
            try {
                renderer.WriteChildren(obj);
            } finally {
                ImGui.PopStyleColor();
            }
        }

        protected override void Write(MarkdownRenderer renderer, LinkInline obj) {
            if (!string.IsNullOrEmpty(obj.Url)) {
                renderer.onClick = () => Process.Start(new ProcessStartInfo(obj.Url) {
                    UseShellExecute = true,
                });
            }

            if (!string.IsNullOrEmpty(obj.Title)) {
                var origColour = ImGui.GetStyle().Colors[(int) ImGuiCol.Text];
                renderer.onHover = () => {
                    ImGui.PushStyleColor(ImGuiCol.Text, origColour);

                    try {
                        Utils.Tooltip(obj.Title);
                    } finally {
                        ImGui.PopStyleColor();
                    }
                };
            }

            try {
                this.InternalWrite(renderer, obj);
            } finally {
                renderer.onClick = null;
                renderer.onHover = null;
            }
        }
    }

    private class AutolinkInlineRenderer : MarkdownObjectRenderer<MarkdownRenderer, AutolinkInline> {
        protected override void Write(MarkdownRenderer renderer, AutolinkInline obj) {
            renderer.onClick = () => Process.Start(new ProcessStartInfo(obj.Url) {
                UseShellExecute = true,
            });

            ImGui.PushStyleColor(ImGuiCol.Text, Constants.LinkColor);
            try {
                Utils.WrapText(
                    obj.Url,
                    ImGui.GetContentRegionAvail().X,
                    renderer.onClick,
                    renderer.onHover
                );
            } finally {
                ImGui.PopStyleColor();
                renderer.onClick = null;
            }
        }
    }

    private class CodeInlineRenderer : MarkdownObjectRenderer<MarkdownRenderer, CodeInline> {
        protected override void Write(MarkdownRenderer renderer, CodeInline obj) {
            Utils.WrapText(
                obj.ContentSpan.ToString(),
                ImGui.GetContentRegionAvail().X,
                renderer.onClick,
                renderer.onHover
            );
        }
    }

    private class LineBreakInlineRenderer : MarkdownObjectRenderer<MarkdownRenderer, LineBreakInline> {
        protected override void Write(MarkdownRenderer renderer, LineBreakInline obj) {
            if (!obj.IsHard) {
                return;
            }

            ImGui.Dummy(Vector2.Zero);
            ImGui.Spacing();
        }
    }
}
