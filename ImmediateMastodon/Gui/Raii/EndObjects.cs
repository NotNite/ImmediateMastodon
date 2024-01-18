using System.Numerics;
using ImGuiNET;

namespace ImmediateMastodon.Gui.Raii;

// Most ImGui widgets with IDisposable interface that automatically destroys them
// when created with using variables.
public static partial class ImRaii {
    private static int disabledCount;

    public static IEndObject Child(string strId) {
        return new EndUnconditionally(ImGui.EndChild, ImGui.BeginChild(strId));
    }

    public static IEndObject Child(string strId, Vector2 size) {
        return new EndUnconditionally(ImGui.EndChild, ImGui.BeginChild(strId, size));
    }

    public static IEndObject Child(string strId, Vector2 size, bool border) {
        return new EndUnconditionally(ImGui.EndChild, ImGui.BeginChild(strId, size, border));
    }

    public static IEndObject Child(string strId, Vector2 size, bool border, ImGuiWindowFlags flags) {
        return new EndUnconditionally(ImGui.EndChild, ImGui.BeginChild(strId, size, border, flags));
    }

    public static IEndObject DragDropTarget() {
        return new EndConditionally(ImGui.EndDragDropTarget, ImGui.BeginDragDropTarget());
    }

    public static IEndObject DragDropSource() {
        return new EndConditionally(ImGui.EndDragDropSource, ImGui.BeginDragDropSource());
    }

    public static IEndObject DragDropSource(ImGuiDragDropFlags flags) {
        return new EndConditionally(ImGui.EndDragDropSource, ImGui.BeginDragDropSource(flags));
    }

    public static IEndObject Popup(string id) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopup(id));
    }

    public static IEndObject Popup(string id, ImGuiWindowFlags flags) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopup(id, flags));
    }

    public static IEndObject PopupModal(string id) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupModal(id));
    }

    public static IEndObject PopupModal(string id, ref bool open) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupModal(id, ref open));
    }

    public static IEndObject PopupModal(string id, ref bool open, ImGuiWindowFlags flags) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupModal(id, ref open, flags));
    }

    public static IEndObject ContextPopup(string id) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupContextWindow(id));
    }

    public static IEndObject ContextPopup(string id, ImGuiPopupFlags flags) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupContextWindow(id, flags));
    }

    public static IEndObject ContextPopupItem(string id) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupContextItem(id));
    }

    public static IEndObject ContextPopupItem(string id, ImGuiPopupFlags flags) {
        return new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupContextItem(id, flags));
    }

    public static IEndObject Combo(string label, string previewValue) {
        return new EndConditionally(ImGui.EndCombo, ImGui.BeginCombo(label, previewValue));
    }

    public static IEndObject Combo(string label, string previewValue, ImGuiComboFlags flags) {
        return new EndConditionally(ImGui.EndCombo, ImGui.BeginCombo(label, previewValue, flags));
    }

    public static IEndObject Group() {
        ImGui.BeginGroup();
        return new EndUnconditionally(ImGui.EndGroup, true);
    }

    public static IEndObject Tooltip() {
        ImGui.BeginTooltip();
        return new EndUnconditionally(ImGui.EndTooltip, true);
    }

    public static IEndObject ListBox(string label) {
        return new EndConditionally(ImGui.EndListBox, ImGui.BeginListBox(label));
    }

    public static IEndObject ListBox(string label, Vector2 size) {
        return new EndConditionally(ImGui.EndListBox, ImGui.BeginListBox(label, size));
    }

    public static IEndObject Table(string table, int numColumns) {
        return new EndConditionally(ImGui.EndTable, ImGui.BeginTable(table, numColumns));
    }

    public static IEndObject Table(string table, int numColumns, ImGuiTableFlags flags) {
        return new EndConditionally(ImGui.EndTable, ImGui.BeginTable(table, numColumns, flags));
    }

    public static IEndObject Table(string table, int numColumns, ImGuiTableFlags flags, Vector2 outerSize) {
        return new EndConditionally(ImGui.EndTable, ImGui.BeginTable(table, numColumns, flags, outerSize));
    }

    public static IEndObject Table(
        string table, int numColumns, ImGuiTableFlags flags, Vector2 outerSize, float innerWidth
    ) {
        return new EndConditionally(ImGui.EndTable, ImGui.BeginTable(table, numColumns, flags, outerSize, innerWidth));
    }

    public static IEndObject TabBar(string label) {
        return new EndConditionally(ImGui.EndTabBar, ImGui.BeginTabBar(label));
    }

    public static IEndObject TabBar(string label, ImGuiTabBarFlags flags) {
        return new EndConditionally(ImGui.EndTabBar, ImGui.BeginTabBar(label, flags));
    }

    public static IEndObject TabItem(string label) {
        return new EndConditionally(ImGui.EndTabItem, ImGui.BeginTabItem(label));
    }

    public static unsafe IEndObject TabItem(byte* label, ImGuiTabItemFlags flags) {
        return new EndConditionally(ImGuiNative.igEndTabItem, ImGuiNative.igBeginTabItem(label, null, flags) != 0);
    }

    public static IEndObject TabItem(string label, ref bool open) {
        return new EndConditionally(ImGui.EndTabItem, ImGui.BeginTabItem(label, ref open));
    }

    public static IEndObject TabItem(string label, ref bool open, ImGuiTabItemFlags flags) {
        return new EndConditionally(ImGui.EndTabItem, ImGui.BeginTabItem(label, ref open, flags));
    }

    public static IEndObject TreeNode(string label) {
        return new EndConditionally(ImGui.TreePop, ImGui.TreeNodeEx(label));
    }

    public static IEndObject TreeNode(string label, ImGuiTreeNodeFlags flags) {
        return new EndConditionally(flags.HasFlag(ImGuiTreeNodeFlags.NoTreePushOnOpen) ? Nop : ImGui.TreePop,
                                    ImGui.TreeNodeEx(label, flags));
    }

    public static IEndObject Disabled() {
        ImGui.BeginDisabled();
        ++disabledCount;
        return DisabledEnd();
    }

    public static IEndObject Disabled(bool disabled) {
        if (!disabled)
            return new EndConditionally(Nop, false);

        ImGui.BeginDisabled();
        ++disabledCount;
        return DisabledEnd();
    }

    public static IEndObject Enabled() {
        var oldCount = disabledCount;
        if (oldCount == 0)
            return new EndConditionally(Nop, false);

        void Restore() {
            disabledCount += oldCount;
            while (--oldCount >= 0)
                ImGui.BeginDisabled();
        }

        for (; disabledCount > 0; --disabledCount)
            ImGui.EndDisabled();

        return new EndUnconditionally(Restore, true);
    }

    private static IEndObject DisabledEnd() {
        return new EndUnconditionally(() => {
            --disabledCount;
            ImGui.EndDisabled();
        }, true);
    }

    /* Only in OtterGui for now
    public static IEndObject FramedGroup(string label)
    {
        Widget.BeginFramedGroup(label, Vector2.Zero);
        return new EndUnconditionally(Widget.EndFramedGroup, true);
    }

    public static IEndObject FramedGroup(string label, Vector2 minSize, string description = "")
    {
        Widget.BeginFramedGroup(label, minSize, description);
        return new EndUnconditionally(Widget.EndFramedGroup, true);
    }
    */

    // Used to avoid tree pops when flag for no push is set.
    private static void Nop() { }

    // Exported interface for RAII.
    public interface IEndObject : IDisposable {
        // Empty end object.
        public static readonly IEndObject Empty = new EndConditionally(Nop, false);
        public bool Success { get; }

        public static bool operator true(IEndObject i) {
            return i.Success;
        }

        public static bool operator false(IEndObject i) {
            return !i.Success;
        }

        public static bool operator !(IEndObject i) {
            return !i.Success;
        }

        public static bool operator &(IEndObject i, bool value) {
            return i.Success && value;
        }

        public static bool operator |(IEndObject i, bool value) {
            return i.Success || value;
        }
    }

    // Use end-function regardless of success.
    // Used by Child, Group and Tooltip.
    private struct EndUnconditionally : IEndObject {
        private Action EndAction { get; }

        public bool Success { get; }

        public bool Disposed { get; private set; }

        public EndUnconditionally(Action endAction, bool success) {
            this.EndAction = endAction;
            this.Success = success;
            this.Disposed = false;
        }

        public void Dispose() {
            if (this.Disposed)
                return;

            this.EndAction();
            this.Disposed = true;
        }
    }

    // Use end-function only on success.
    private struct EndConditionally : IEndObject {
        public EndConditionally(Action endAction, bool success) {
            this.EndAction = endAction;
            this.Success = success;
            this.Disposed = false;
        }

        public bool Success { get; }

        public bool Disposed { get; private set; }

        private Action EndAction { get; }

        public void Dispose() {
            if (this.Disposed)
                return;

            if (this.Success)
                this.EndAction();
            this.Disposed = true;
        }
    }
}
