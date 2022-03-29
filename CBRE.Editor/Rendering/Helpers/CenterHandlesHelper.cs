using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.UI;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CBRE.Editor.Rendering.Helpers
{
    public class CenterHandlesHelper : IHelper
    {
        public Document Document { get; set; }
        public bool Is2DHelper { get { return CBRE.Settings.Select.DrawCenterHandles; } }
        public bool Is3DHelper { get { return false; } }
        public bool IsDocumentHelper { get { return false; } }
        public HelperType HelperType { get { return HelperType.Augment; } }

        public bool IsValidFor(MapObject o)
        {
            return (o is Entity || o is Solid) && !o.HasChildren;
        }

        private double _offset;
        private double _fadeDistance;
        private Coordinate _mousePos;
        public void BeforeRender2D(Viewport2D viewport)
        {
            _offset = 3 / (double)viewport.Zoom;
            _fadeDistance = 200 / (double)viewport.Zoom;
            Point mp = viewport.PointToClient(Control.MousePosition);
            _mousePos = viewport.ScreenToWorld(new Coordinate(mp.X, viewport.Height - mp.Y, 0));
            GL.Enable(EnableCap.LineSmooth);
            GL.Begin(PrimitiveType.Lines);
        }

        public void Render2D(Viewport2D viewport, MapObject o)
        {
            if (CBRE.Settings.Select.CenterHandlesActiveViewportOnly && !viewport.IsFocused) return;
            Coordinate center = viewport.Flatten(o.BoundingBox.Center);
            double a = 192;
            if (CBRE.Settings.Select.CenterHandlesFollowCursor)
            {
                double dist = (double)(center - _mousePos).VectorMagnitude();
                if (dist >= _fadeDistance) return;
                a = 192 * ((_fadeDistance - dist) / _fadeDistance);
            }
            GL.Color4(Color.FromArgb((int)a, o.Colour));
            GL.Vertex2(center.DX - _offset, center.DY - _offset);
            GL.Vertex2(center.DX + _offset, center.DY + _offset);
            GL.Vertex2(center.DX - _offset, center.DY + _offset);
            GL.Vertex2(center.DX + _offset, center.DY - _offset);
        }

        public void AfterRender2D(Viewport2D viewport)
        {
            GL.End();
            GL.Disable(EnableCap.LineSmooth);
        }

        public void BeforeRender3D(Viewport3D viewport)
        {
            throw new NotImplementedException();
        }

        public void Render3D(Viewport3D vp, MapObject o)
        {
            throw new NotImplementedException();
        }

        public void AfterRender3D(Viewport3D viewport)
        {
            throw new NotImplementedException();
        }

        public void RenderDocument(ViewportBase viewport, Document document)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MapObject> Order(ViewportBase viewport, IEnumerable<MapObject> mapObjects)
        {
            return mapObjects;
        }
    }
}