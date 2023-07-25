using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using LogicAndTrick.Oy;
using CBRE.BspEditor.Documents;
using CBRE.Common.Shell.Documents;
using CBRE.Common.Shell.Hooks;
using CBRE.Rendering.Engine;
using CBRE.Rendering.Overlay;

namespace CBRE.BspEditor.Rendering.Overlay
{
    [Export(typeof(IStartupHook))]
    public class OverlayManager : IStartupHook
    {
        [Import] private Lazy<EngineInterface> _engine;
        [ImportMany] private IOverlayRenderable[] _overlayRenderables;
        [ImportMany] private IMapDocumentOverlayRenderable[] _documentOverlayRenderables;

        /// <inheritdoc />
        public Task OnStartup()
        {
            Oy.Subscribe<IDocument>("Document:Activated", DocumentActivated);
            Oy.Subscribe<IDocument>("Document:Closed", DocumentClosed);

            foreach (IOverlayRenderable or in _overlayRenderables.Union(_documentOverlayRenderables))
            {
                _engine.Value.Add(or);
            }

            return Task.FromResult(0);
        }

        private WeakReference<MapDocument> _activeDocument = new WeakReference<MapDocument>(null);

        // Document events

        private async Task DocumentActivated(IDocument doc)
        {
            MapDocument md = doc as MapDocument;
            _activeDocument = new WeakReference<MapDocument>(md);
            await UpdateDocument(md);
        }

        private async Task DocumentClosed(IDocument doc)
        {
            if (_activeDocument.TryGetTarget(out MapDocument md) && md == doc)
            {
                await UpdateDocument(null);
            }
        }

        private Task UpdateDocument(MapDocument doc)
        {
            foreach (IMapDocumentOverlayRenderable dor in _documentOverlayRenderables)
            {
                dor.SetActiveDocument(doc);
            }

            return Task.CompletedTask;
        }
    }
}