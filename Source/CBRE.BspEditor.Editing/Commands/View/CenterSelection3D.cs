using System.ComponentModel.Composition;
using System.Threading.Tasks;
using LogicAndTrick.Oy;
using CBRE.BspEditor.Commands;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Editing.Properties;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Menu;
using CBRE.Common.Translations;

namespace CBRE.BspEditor.Editing.Commands.View
{
    [AutoTranslate]
    [Export(typeof(ICommand))]
    [CommandID("BspEditor:View:CenterSelection3D")]
    [MenuItem("View", "", "Selection", "D")]
    [MenuImage(typeof(Resources), nameof(Resources.Menu_CenterSelection3D))]
    public class CenterSelection3D : BaseCommand
    {
        public override string Name { get; set; } = "Center 3D views on selection";
        public override string Details { get; set; } = "Move the cameras of 3D views to focus on the selected objects.";

        protected override async Task Invoke(MapDocument document, CommandParameters parameters)
        {
            if (document.Selection.IsEmpty) return;

            DataStructures.Geometric.Box box = document.Selection.GetSelectionBoundingBox();

            await Oy.Publish("MapDocument:Viewport:Focus3D", box);
        }
    }
}