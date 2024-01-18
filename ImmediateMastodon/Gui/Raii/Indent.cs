using ImGuiNET;

namespace ImmediateMastodon.Gui.Raii;

public static partial class ImRaii {
    public static Indent PushIndent(float f, bool scaled = true, bool condition = true) {
        return new Indent().Push(f, scaled, condition);
    }

    public static Indent PushIndent(int i = 1, bool condition = true) {
        return new Indent().Push(i, condition);
    }

    public sealed class Indent : IDisposable {
        public float Indentation { get; private set; }

        public void Dispose() {
            this.Pop(this.Indentation, false);
        }

        public Indent Push(float indent, bool scaled = true, bool condition = true) {
            if (condition) {
                IndentInternal(indent);
                this.Indentation += indent;
            }

            return this;
        }

        public Indent Push(int i = 1, bool condition = true) {
            if (condition) {
                var spacing = i * ImGui.GetStyle().IndentSpacing;
                IndentInternal(spacing);
                this.Indentation += spacing;
            }

            return this;
        }

        public void Pop(float indent, bool scaled = true) {
            IndentInternal(-indent);
            this.Indentation -= indent;
        }

        public void Pop(int i) {
            var spacing = i * ImGui.GetStyle().IndentSpacing;
            IndentInternal(-spacing);
            this.Indentation -= spacing;
        }

        private static void IndentInternal(float indent) {
            if (indent < 0)
                ImGui.Unindent(-indent);
            else if (indent > 0)
                ImGui.Indent(indent);
        }
    }
}
