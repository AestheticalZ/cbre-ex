﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Modification;
using CBRE.BspEditor.Modification.Operations.Tree;
using CBRE.BspEditor.Primitives;
using CBRE.BspEditor.Primitives.MapObjects;
using CBRE.BspEditor.Rendering.ResourceManagement;
using CBRE.BspEditor.Rendering.Viewport;
using CBRE.BspEditor.Tools.Properties;
using CBRE.Common.Shell.Components;
using CBRE.Common.Shell.Hotkeys;
using CBRE.Rendering.Cameras;
using CBRE.DataStructures.Geometric;
using CBRE.Rendering.Overlay;
using CBRE.Rendering.Pipelines;
using CBRE.Rendering.Primitives;
using CBRE.Rendering.Resources;
using CBRE.Rendering.Viewports;
using CBRE.Shell.Input;
using Plane = CBRE.DataStructures.Geometric.Plane;

namespace CBRE.BspEditor.Tools.Clip
{
    [Export(typeof(ITool))]
    [OrderHint("N")]
    [DefaultHotkey("Shift+X")]
    public class ClipTool : BaseTool
    {
        public enum ClipState
        {
            None,
            Drawing,
            Drawn,
            MovingPoint1,
            MovingPoint2,
            MovingPoint3
        }

        public enum ClipSide
        {
            Both,
            Front,
            Back
        }

        private Vector3? _clipPlanePoint1;
        private Vector3? _clipPlanePoint2;
        private Vector3? _clipPlanePoint3;
        private Vector3? _drawingPoint;
        private ClipState _prevState;
        private ClipState _state;
        private ClipSide _side;

        public ClipTool()
        {
            Usage = ToolUsage.Both;
            _clipPlanePoint1 = _clipPlanePoint2 = _clipPlanePoint3 = _drawingPoint = null;
            _state = _prevState = ClipState.None;
            _side = ClipSide.Both;
        }

        public override Image GetIcon()
        {
            return Resources.Tool_Clip;
        }

        public override string GetName()
        {
            return "Clip Tool";
        }

        protected override IEnumerable<Subscription> Subscribe()
        {
            yield return Oy.Subscribe<ClipTool>("Tool:Activated", t => CycleClipSide());
            yield return Oy.Subscribe<string>("ClipTool:SetClipSide", v => SetClipSide(v));
        }

        private void SetClipSide(string visiblePoints)
        {
            if (Enum.TryParse(visiblePoints, true, out ClipSide s) && s != _side)
            {
                _side = s;
            }
        }
        
        private void CycleClipSide()
        {
            int side = (int) _side;
            side = (side + 1) % Enum.GetValues(typeof (ClipSide)).Length;
            _side = (ClipSide) side;
        }

        private ClipState GetStateAtPoint(int x, int y, OrthographicCamera camera)
        {
            if (_clipPlanePoint1 == null || _clipPlanePoint2 == null || _clipPlanePoint3 == null) return ClipState.None;

            Vector3 p = camera.Flatten(camera.ScreenToWorld(new Vector3(x, y, 0)));
            Vector3 p1 = camera.Flatten(_clipPlanePoint1.Value);
            Vector3 p2 = camera.Flatten(_clipPlanePoint2.Value);
            Vector3 p3 = camera.Flatten(_clipPlanePoint3.Value);

            float d = 5 / camera.Zoom;

            if (p.X >= p1.X - d && p.X <= p1.X + d && p.Y >= p1.Y - d && p.Y <= p1.Y + d) return ClipState.MovingPoint1;
            if (p.X >= p2.X - d && p.X <= p2.X + d && p.Y >= p2.Y - d && p.Y <= p2.Y + d) return ClipState.MovingPoint2;
            if (p.X >= p3.X - d && p.X <= p3.X + d && p.Y >= p3.Y - d && p.Y <= p3.Y + d) return ClipState.MovingPoint3;

            return ClipState.None;
        }

        protected override void MouseDown(MapDocument document, MapViewport vp, OrthographicCamera camera, ViewportEvent e)
        {
            _prevState = _state;

            Vector3 point = SnapIfNeeded(camera.ScreenToWorld(e.X, e.Y));
            ClipState st = GetStateAtPoint(e.X, e.Y, camera);
            if (_state == ClipState.None || st == ClipState.None)
            {
                _state = ClipState.Drawing;
                _drawingPoint = point;
            }
            else if (_state == ClipState.Drawn)
            {
                _state = st;
            }
        }

        protected override void MouseUp(MapDocument document, MapViewport vp, OrthographicCamera camera, ViewportEvent e)
        {
            _state = _state == ClipState.Drawing ? _prevState : ClipState.Drawn;
        }

        protected override void MouseMove(MapDocument document, MapViewport vp, OrthographicCamera camera, ViewportEvent e)
        {
            MapViewport viewport = vp;

            Vector3 point = SnapIfNeeded(camera.ScreenToWorld(e.X, e.Y));
            ClipState st = GetStateAtPoint(e.X, e.Y, camera);
            if (_state == ClipState.Drawing)
            {
                _state = ClipState.MovingPoint2;
                _clipPlanePoint1 = _drawingPoint;
                _clipPlanePoint2 = point;
                _clipPlanePoint3 = _clipPlanePoint1 + SnapIfNeeded(camera.GetUnusedCoordinate(new Vector3(128, 128, 128)));
            }
            else if (_state == ClipState.MovingPoint1)
            {
                // Move point 1
                Vector3 cp1 = camera.GetUnusedCoordinate(_clipPlanePoint1.Value) + point;
                if (KeyboardState.Ctrl)
                {
                    Vector3? diff = _clipPlanePoint1 - cp1;
                    _clipPlanePoint2 -= diff;
                    _clipPlanePoint3 -= diff;
                }
                _clipPlanePoint1 = cp1;
            }
            else if (_state == ClipState.MovingPoint2)
            {
                // Move point 2
                Vector3 cp2 = camera.GetUnusedCoordinate(_clipPlanePoint2.Value) + point;
                if (KeyboardState.Ctrl)
                {
                    Vector3? diff = _clipPlanePoint2 - cp2;
                    _clipPlanePoint1 -= diff;
                    _clipPlanePoint3 -= diff;
                }
                _clipPlanePoint2 = cp2;
            }
            else if (_state == ClipState.MovingPoint3)
            {
                // Move point 3
                Vector3 cp3 = camera.GetUnusedCoordinate(_clipPlanePoint3.Value) + point;
                if (KeyboardState.Ctrl)
                {
                    Vector3? diff = _clipPlanePoint3 - cp3;
                    _clipPlanePoint1 -= diff;
                    _clipPlanePoint2 -= diff;
                }
                _clipPlanePoint3 = cp3;
            }

            if (st != ClipState.None || _state != ClipState.None && _state != ClipState.Drawn)
            {
                viewport.Control.Cursor = Cursors.Cross;
            }
            else
            {
                viewport.Control.Cursor = Cursors.Default;
            }
        }

        protected override void KeyDown(MapDocument document, MapViewport viewport, OrthographicCamera camera, ViewportEvent e)
        {
            OnKeyDown(document, viewport, e);
            base.KeyDown(document, viewport, camera, e);
        }

        protected override void KeyDown(MapDocument document, MapViewport viewport, PerspectiveCamera camera, ViewportEvent e)
        {
            OnKeyDown(document, viewport, e);
            base.KeyDown(document, viewport, camera, e);
        }

        private void OnKeyDown(MapDocument document, MapViewport viewport, ViewportEvent e)
        {
            if (e.KeyCode == Keys.Enter && _state != ClipState.None)
            {
                if (!_clipPlanePoint1.Value.EquivalentTo(_clipPlanePoint2.Value)
                    && !_clipPlanePoint2.Value.EquivalentTo(_clipPlanePoint3.Value)
                    && !_clipPlanePoint1.Value.EquivalentTo(_clipPlanePoint3.Value)) // Don't clip if the points are too close together
                {
                    PerformClip(document);
                }
            }
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) // Escape cancels, Enter commits and resets
            {
                _clipPlanePoint1 = _clipPlanePoint2 = _clipPlanePoint3 = _drawingPoint = null;
                _state = _prevState = ClipState.None;
            }
        }

        private void PerformClip(MapDocument document)
        {
            List<Solid> objects = document.Selection.OfType<Solid>().ToList();
            if (!objects.Any()) return;

            Plane plane = new Plane(_clipPlanePoint1.Value, _clipPlanePoint2.Value, _clipPlanePoint3.Value);
            Transaction clip = new Transaction();
            bool found = false;
            foreach (Solid solid in objects)
            {
                solid.Split(document.Map.NumberGenerator, plane, out Solid backSolid, out Solid frontSolid);
                found = true;
                
                // Remove the clipped solid
                clip.Add(new Detatch(solid.Hierarchy.Parent.ID, solid));
                
                if (_side != ClipSide.Back && frontSolid != null)
                {
                    // Add front solid
                    clip.Add(new Attach(solid.Hierarchy.Parent.ID, frontSolid));
                }
                
                if (_side != ClipSide.Front && backSolid != null)
                {
                    // Add back solid
                    clip.Add(new Attach(solid.Hierarchy.Parent.ID, backSolid));
                }
            }
            if (found)
            {
                MapDocumentOperation.Perform(document, clip);
            }
        }

        protected override void Render(MapDocument document, BufferBuilder builder, ResourceCollector resourceCollector)
        {
            base.Render(document, builder, resourceCollector);

            if (_state != ClipState.None && _clipPlanePoint1 != null && _clipPlanePoint2 != null && _clipPlanePoint3 != null)
            {
                // Draw the lines
                Vector3 p1 = _clipPlanePoint1.Value;
                Vector3 p2 = _clipPlanePoint2.Value;
                Vector3 p3 = _clipPlanePoint3.Value;

                builder.Append(
                    new []
                    {
                        new VertexStandard { Position = p1, Colour = Vector4.One, Tint = Vector4.One },
                        new VertexStandard { Position = p2, Colour = Vector4.One, Tint = Vector4.One },
                        new VertexStandard { Position = p3, Colour = Vector4.One, Tint = Vector4.One },
                    },
                    new uint [] { 0, 1, 1, 2, 2, 0 },
                    new []
                    {
                        new BufferGroup(PipelineType.Wireframe, CameraType.Both, 0, 6)
                    }
                );

                if (!p1.EquivalentTo(p2)
                    && !p2.EquivalentTo(p3)
                    && !p1.EquivalentTo(p3)
                    && !document.Selection.IsEmpty)
                {
                    Plane plane = new Plane(p1, p2, p3);
                    DataStructures.Geometric.Precision.Plane pp = plane.ToPrecisionPlane();

                    // Draw the clipped solids
                    List<Polygon> faces = new List<Polygon>();
                    foreach (Solid solid in document.Selection.OfType<Solid>().ToList())
                    {
                        DataStructures.Geometric.Precision.Polyhedron s = solid.ToPolyhedron().ToPrecisionPolyhedron();
                        s.Split(pp, out DataStructures.Geometric.Precision.Polyhedron back, out DataStructures.Geometric.Precision.Polyhedron front);

                        if (_side != ClipSide.Front && back != null) faces.AddRange(back.Polygons.Select(x => x.ToStandardPolygon()));
                        if (_side != ClipSide.Back && front != null) faces.AddRange(front.Polygons.Select(x => x.ToStandardPolygon()));
                    }

                    List<VertexStandard> verts = new List<VertexStandard>();
                    List<int> indices = new List<int>();

                    foreach (Polygon polygon in faces)
                    {
                        int c = verts.Count;
                        verts.AddRange(polygon.Vertices.Select(x => new VertexStandard { Position = x, Colour = Vector4.One, Tint = Vector4.One }));
                        for (int i = 0; i < polygon.Vertices.Count; i++)
                        {
                            indices.Add(c + i);
                            indices.Add(c + (i + 1) % polygon.Vertices.Count);
                        }
                    }

                    builder.Append(
                        verts, indices.Select(x => (uint) x),
                        new[] { new BufferGroup(PipelineType.Wireframe, CameraType.Both, 0, (uint) indices.Count) }
                    );

                    // Draw the clipping plane

                    DataStructures.Geometric.Precision.Polygon poly = new DataStructures.Geometric.Precision.Polygon(pp);
                    Box bbox = document.Selection.GetSelectionBoundingBox();
                    Vector3 point = bbox.Center;
                    foreach (Plane boxPlane in bbox.GetBoxPlanes())
                    {
                        Vector3 proj = boxPlane.Project(point);
                        float dist = (point - proj).Length() * 0.1f;
                        DataStructures.Geometric.Precision.Plane pln = new Plane(boxPlane.Normal, proj + boxPlane.Normal * Math.Max(dist, 100)).ToPrecisionPlane();
                        if (poly.Split(pln, out DataStructures.Geometric.Precision.Polygon b, out _)) poly = b;
                    }

                    verts.Clear();
                    indices.Clear();

                    Polygon clipPoly = poly.ToStandardPolygon();
                    Vector4 colour = Color.FromArgb(64, Color.Turquoise).ToVector4();

                    // Add the face in both directions so it renders on both sides
                    List<Vector3>[] polies = new[] { clipPoly.Vertices.ToList(), clipPoly.Vertices.Reverse().ToList() };
                    foreach (List<Vector3> p in polies)
                    {
                        int offs = verts.Count;
                        verts.AddRange(p.Select(x => new VertexStandard
                        {
                            Position = x,
                            Colour = Vector4.One,
                            Tint = colour,
                            Flags = VertexFlags.FlatColour
                        }));

                        for (int i = 2; i < clipPoly.Vertices.Count; i++)
                        {
                            indices.Add(offs);
                            indices.Add(offs + i - 1);
                            indices.Add(offs + i);
                        }
                    }

                    builder.Append(
                        verts, indices.Select(x => (uint)x),
                        new[] { new BufferGroup(PipelineType.TexturedAlpha, CameraType.Perspective, p1, 0, (uint)indices.Count) }
                    );
                }
            }
        }

        protected override void Render(MapDocument document, IViewport viewport, OrthographicCamera camera, Vector3 worldMin, Vector3 worldMax, I2DRenderer im)
        {
            base.Render(document, viewport, camera, worldMin, worldMax, im);

            if (_state != ClipState.None && _clipPlanePoint1 != null && _clipPlanePoint2 != null && _clipPlanePoint3 != null)
            {
                Vector3 p1 = _clipPlanePoint1.Value;
                Vector3 p2 = _clipPlanePoint2.Value;
                Vector3 p3 = _clipPlanePoint3.Value;
                Vector3[] points = new[] {p1, p2, p3};

                foreach (Vector3 p in points)
                {
                    const int size = 4;
                    Vector3 spos = camera.WorldToScreen(p);
                    
                    im.AddRectOutlineOpaque(new Vector2(spos.X - size, spos.Y - size), new Vector2(spos.X + size, spos.Y + size), Color.Black, Color.White);
                }
            }
        }
    }
}
