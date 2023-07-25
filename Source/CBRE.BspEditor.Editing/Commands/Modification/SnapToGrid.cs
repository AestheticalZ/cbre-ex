﻿using System.ComponentModel.Composition;
using System.Numerics;
using System.Threading.Tasks;
using CBRE.BspEditor.Commands;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Editing.Properties;
using CBRE.BspEditor.Modification;
using CBRE.BspEditor.Modification.Operations.Mutation;
using CBRE.BspEditor.Primitives.MapData;
using CBRE.Common.Shell.Commands;
using CBRE.Common.Shell.Context;
using CBRE.Common.Shell.Menu;
using CBRE.Common.Translations;

namespace CBRE.BspEditor.Editing.Commands.Modification
{
    [AutoTranslate]
    [Export(typeof(ICommand))]
    [MenuItem("Tools", "", "Snap", "B")]
    [CommandID("BspEditor:Tools:SnapToGrid")]
    [MenuImage(typeof(Resources), nameof(Resources.Menu_SnapSelection))]
    public class SnapToGrid : BaseCommand
    {
        public override string Name { get; set; } = "Snap to grid";
        public override string Details { get; set; } = "Snap selection to grid";

        protected override bool IsInContext(IContext context, MapDocument document)
        {
            return base.IsInContext(context, document) && !document.Selection.IsEmpty;
        }

        protected override async Task Invoke(MapDocument document, CommandParameters parameters)
        {
            DataStructures.Geometric.Box selBox = document.Selection.GetSelectionBoundingBox();
            GridData grid = document.Map.Data.GetOne<GridData>();
            if (grid == null) return;

            Vector3 start = selBox.Start;
            Vector3 snapped = grid.Grid.Snap(start);
            Vector3 trans = snapped - start;
            if (trans == Vector3.Zero) return;

            Matrix4x4 tform = Matrix4x4.CreateTranslation(trans);

            Transaction transaction = new Transaction();
            BspEditor.Modification.Operations.Mutation.Transform transformOperation = new BspEditor.Modification.Operations.Mutation.Transform(tform, document.Selection.GetSelectedParents());
            transaction.Add(transformOperation);

            // Check for texture transform
            TransformationFlags tl = document.Map.Data.GetOne<TransformationFlags>() ?? new TransformationFlags();
            if (tl.TextureLock) transaction.Add(new TransformTexturesUniform(tform, document.Selection));

            await MapDocumentOperation.Perform(document, transaction);
        }
    }
}
