using CBRE.Common.Mediator;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CBRE.Editor.Menu
{
    public class RecentFilesMenu : IMenuBuilder
    {
        public bool ShowInMenu { get { return true; } }
        public bool ShowInToolStrip { get { return false; } }
        public IEnumerable<ToolStripItem> Build()
        {
            if (MenuManager.RecentFiles.Count == 0) yield break;
            yield return new ToolStripSeparator();
            foreach (CBRE.Settings.Models.RecentFile rf in MenuManager.RecentFiles.OrderBy(x => x.Order))
            {
                string file = rf.Location;
                ToolStripMenuItem mi = new ToolStripMenuItem(Path.GetFileName(file));
                mi.Click += (sender, e) => Mediator.Publish(EditorMediator.LoadFile, file);
                yield return mi;
            }
        }

        public IEnumerable<ToolStripItem> BuildToolStrip()
        {
            throw new System.NotImplementedException();
        }
    }
}