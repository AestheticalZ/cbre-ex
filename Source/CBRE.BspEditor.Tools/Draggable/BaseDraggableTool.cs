using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Rendering.Resources;
using CBRE.BspEditor.Rendering.Viewport;
using CBRE.Rendering.Cameras;
using CBRE.Rendering.Overlay;
using CBRE.Rendering.Resources;
using CBRE.Rendering.Viewports;

namespace CBRE.BspEditor.Tools.Draggable
{
    public abstract class BaseDraggableTool : BaseTool
    {
        protected List<IDraggableState> States { get; }

        public IDraggable CurrentDraggable { get; private set; }
        private Vector3? _lastDragPoint;

        protected BaseDraggableTool()
        {
            States = new List<IDraggableState>();
        }

        #region Virtual events
        protected virtual void OnDraggableMouseDown(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 position, IDraggable draggable)
        {

        }
        protected virtual void OnDraggableMouseUp(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 position, IDraggable draggable)
        {

        }
        protected virtual void OnDraggableClicked(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 position, IDraggable draggable)
        {

        }

        protected virtual void OnDraggableDragStarted(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 position, IDraggable draggable)
        {

        }

        protected virtual void OnDraggableDragMoving(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 previousPosition, Vector3 position, IDraggable draggable)
        {

        }

        protected virtual void OnDraggableDragMoved(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 previousPosition, Vector3 position, IDraggable draggable)
        {

        }

        protected virtual void OnDraggableDragEnded(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e, Vector3 position, IDraggable draggable)
        {

        }
        #endregion

        protected override void MouseClick(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (e.Dragging || e.Button != MouseButtons.Left) return;
            if (CurrentDraggable == null) return;
            Vector3 point = camera.ScreenToWorld(e.X, e.Y);
            point = camera.Flatten(point);
            OnDraggableClicked(document, viewport, camera, e, point, CurrentDraggable);
            if (!e.Handled) CurrentDraggable.Click(document, viewport, camera, e, point);
        }

        protected override void MouseDown(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (CurrentDraggable == null) return;
            Vector3 point = camera.ScreenToWorld(e.X, e.Y);
            point = camera.Flatten(point);
            OnDraggableMouseDown(document, viewport, camera, e, point, CurrentDraggable);
            if (!e.Handled) CurrentDraggable.MouseDown(document, viewport, camera, e, point);
        }

        protected override void MouseUp(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (CurrentDraggable == null) return;
            Vector3 point = camera.ScreenToWorld(e.X, e.Y);
            point = camera.Flatten(point);
            OnDraggableMouseUp(document, viewport, camera, e, point, CurrentDraggable);
            if (!e.Handled) CurrentDraggable.MouseUp(document, viewport, camera, e, point);
        }

        protected override void MouseMove(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (e.Dragging || e.Button == MouseButtons.Left) return;
            Vector3 point = camera.ScreenToWorld(e.X, e.Y);
            point = camera.Flatten(point);
            IDraggable drag = null;
            foreach (IDraggableState state in States)
            {
                List<IDraggable> drags = state.GetDraggables().ToList();
                drags.Add(state);
                foreach (IDraggable draggable in drags)
                {
                    if (draggable.CanDrag(document, viewport, camera, e, point))
                    {
                        drag = draggable;
                        break;
                    }
                }
                if (drag != null) break;
            }
            if (drag != CurrentDraggable)
            {
                CurrentDraggable?.Unhighlight(document, viewport);
                CurrentDraggable = drag;
                CurrentDraggable?.Highlight(document, viewport);
            }
        }

        protected override void DragStart(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CurrentDraggable == null) return;
            Vector3 point = camera.Flatten(camera.ScreenToWorld(e.X, e.Y));
            OnDraggableDragStarted(document, viewport, camera, e, point, CurrentDraggable);
            if (!e.Handled) CurrentDraggable.StartDrag(document, viewport, camera, e, point);
            _lastDragPoint = point;
        }

        protected override void DragMove(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CurrentDraggable == null || !_lastDragPoint.HasValue) return;
            Vector3 point = camera.Flatten(camera.ScreenToWorld(e.X, e.Y));
            Vector3 last = _lastDragPoint.Value;
            OnDraggableDragMoving(document, viewport, camera, e, last, point, CurrentDraggable);
            if (!e.Handled) CurrentDraggable.Drag(document, viewport, camera, e, last, point);
            if (!e.Handled) OnDraggableDragMoved(document, viewport, camera, e, last, point, CurrentDraggable);
            _lastDragPoint = point;
        }

        protected override void DragEnd(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CurrentDraggable == null) return;
            Vector3 point = camera.ScreenToWorld(e.X, e.Y);
            point = camera.Flatten(point);
            OnDraggableDragEnded(document, viewport, camera, e, point, CurrentDraggable);
            if (!e.Handled) CurrentDraggable.EndDrag(document, viewport, camera, e, point);
            _lastDragPoint = null;
        }

        private IEnumerable<T> CollectObjects<T>(Func<IDraggable, IEnumerable<T>> collector)
        {
            List<T> list = new List<T>();

            bool foundActive = false;
            foreach (IDraggableState state in States)
            {
                foreach (IDraggable draggable in state.GetDraggables())
                {
                    if (draggable == CurrentDraggable) foundActive = true;
                    else list.AddRange(collector(draggable));
                }
                if (state == CurrentDraggable) foundActive = true;
                else list.AddRange(collector(state));
            }
            if (CurrentDraggable != null && foundActive) list.AddRange(collector(CurrentDraggable));

            return list;
        }

        protected override void Render(MapDocument document, BufferBuilder builder, ResourceCollector resourceCollector)
        {
            foreach (IDraggable obj in CollectObjects(x => new[] {x}))
            {
                obj.Render(document, builder);
            }
            base.Render(document, builder, resourceCollector);
        }

        protected override void Render(MapDocument document, IViewport viewport, OrthographicCamera camera, Vector3 worldMin, Vector3 worldMax, I2DRenderer im)
        {
            foreach (IDraggable obj in CollectObjects(x => new[] { x }).OrderBy(x => camera.GetUnusedValue(x.ZIndex)))
            {
                obj.Render(viewport, camera, worldMin, worldMax, im);
            }
            base.Render(document, viewport, camera, worldMin, worldMax, im);
        }

        protected override void Render(MapDocument document, IViewport viewport, PerspectiveCamera camera, I2DRenderer im)
        {
            foreach (IDraggable obj in CollectObjects(x => new[] { x }).OrderByDescending(x => (x.Origin - camera.Position).LengthSquared()))
            {
                obj.Render(viewport, camera, im);
            }
            base.Render(document, viewport, camera, im);
        }
    }
}