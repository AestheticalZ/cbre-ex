using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.Editor.Documents;
using CBRE.Settings;
using CBRE.UI;
using System;
using System.Windows.Forms;

namespace CBRE.Editor.UI
{
    public sealed class ViewportContextMenu : ContextMenuStrip
    {
        internal static ViewportContextMenu Instance { get; private set; }

        static ViewportContextMenu()
        {
            Instance = new ViewportContextMenu();
        }

        public void AddNonSelectionItems(Document doc, ViewportBase viewport)
        {
            Items.Clear();
            Add("Paste", HotkeysMediator.OperationsPaste, Clipboard.ClipboardManager.CanPaste());
            Add("Paste Special", HotkeysMediator.OperationsPasteSpecial, Clipboard.ClipboardManager.CanPaste());
            Items.Add(new ToolStripSeparator());
            Add(doc.History.GetUndoString(), HotkeysMediator.HistoryUndo, doc.History.CanUndo());
            Add(doc.History.GetRedoString(), HotkeysMediator.HistoryRedo, doc.History.CanRedo());
        }

        public void AddSelectionItems(Document doc, ViewportBase viewport)
        {
            Items.Clear();
            Add("Cut", HotkeysMediator.OperationsCut);
            Add("Copy", HotkeysMediator.OperationsCopy);
            Add("Delete", HotkeysMediator.OperationsDelete);
            Add("Paste Special", HotkeysMediator.OperationsPasteSpecial, Clipboard.ClipboardManager.CanPaste());
            Items.Add(new ToolStripSeparator());
            Add("Transform...", HotkeysMediator.Transform);
            Items.Add(new ToolStripSeparator());
            Add(doc.History.GetUndoString(), HotkeysMediator.HistoryUndo, doc.History.CanUndo());
            Add(doc.History.GetRedoString(), HotkeysMediator.HistoryRedo, doc.History.CanRedo());
            Items.Add(new ToolStripSeparator());
            Add("Carve", HotkeysMediator.Carve);
            Add("Hollow", HotkeysMediator.MakeHollow);
            Items.Add(new ToolStripSeparator());
            Add("Group", HotkeysMediator.GroupingGroup);
            Add("Ungroup", HotkeysMediator.GroupingUngroup);
            Items.Add(new ToolStripSeparator());
            Add("Tie To Entity", HotkeysMediator.TieToEntity);
            Add("Move To World", HotkeysMediator.TieToWorld);
            Items.Add(new ToolStripSeparator());
            Viewport2D vp = viewport as Viewport2D;
            if (vp != null)
            {
                Coordinate flat = vp.Flatten(new Coordinate(1, 2, 3));
                HotkeysMediator left = flat.X == 1 ? HotkeysMediator.AlignXMin : (flat.X == 2 ? HotkeysMediator.AlignYMin : HotkeysMediator.AlignZMin);
                HotkeysMediator right = flat.X == 1 ? HotkeysMediator.AlignXMax : (flat.X == 2 ? HotkeysMediator.AlignYMax : HotkeysMediator.AlignZMax);
                HotkeysMediator bottom = flat.Y == 1 ? HotkeysMediator.AlignXMin : (flat.Y == 2 ? HotkeysMediator.AlignYMin : HotkeysMediator.AlignZMin);
                HotkeysMediator top = flat.Y == 1 ? HotkeysMediator.AlignXMax : (flat.Y == 2 ? HotkeysMediator.AlignYMax : HotkeysMediator.AlignZMax);
                Items.Add(new ToolStripMenuItem("Align", null,
                                                CreateMenuItem("Top", top),
                                                CreateMenuItem("Left", left),
                                                CreateMenuItem("Right", right),
                                                CreateMenuItem("Bottom", bottom)));
            }
            Add("Properties", HotkeysMediator.ObjectProperties);
        }

        private void Add(string name, Enum onclick, bool enabled = true)
        {
            ToolStripItem mi = CreateMenuItem(name, onclick);
            mi.Enabled = enabled;
            Items.Add(mi);
        }

        private static ToolStripItem CreateMenuItem(string name, Enum onclick)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(name);
            item.Click += (sender, args) => Mediator.Publish(onclick);
            return item;
        }
    }
}