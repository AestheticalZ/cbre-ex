using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives.MapObjectData;
using CBRE.BspEditor.Primitives.MapObjects;
using CBRE.BspEditor.Rendering.ChangeHandlers;
using CBRE.BspEditor.Rendering.ResourceManagement;
using CBRE.DataStructures.Geometric;
using CBRE.Rendering.Cameras;
using CBRE.Rendering.Pipelines;
using CBRE.Rendering.Primitives;
using CBRE.Rendering.Resources;
using Plane = CBRE.DataStructures.Geometric.Plane;

namespace CBRE.BspEditor.Rendering.Converters
{
    [Export(typeof(IMapObjectSceneConverter))]
    public class DefaultEntityConverter : IMapObjectSceneConverter
    {
        public MapObjectSceneConverterPriority Priority => MapObjectSceneConverterPriority.DefaultLowest;

        public bool ShouldStopProcessing(MapDocument document, IMapObject obj)
        {
            return false;
        }

        public bool Supports(IMapObject obj)
        {
            return obj is Entity && !obj.Hierarchy.HasChildren;
        }

        public Task Convert(BufferBuilder builder, MapDocument document, IMapObject obj, ResourceCollector resourceCollector)
        {
            return ConvertBox(builder, obj, obj.BoundingBox);
        }

        internal static Task ConvertBox(BufferBuilder builder, IMapObject obj, Box box)
        {
            // It's always a box, these numbers are known
            const uint numVertices = 4 * 6;

            // Pack the indices like this [ solid1 ... solidn ] [ wireframe1 ... wireframe n ]
            const uint numSolidIndices = 36;
            const uint numWireframeIndices = numVertices * 2;

            VertexStandard[] points = new VertexStandard[numVertices];
            uint[] indices = new uint[numSolidIndices + numWireframeIndices];

            Color c = obj.IsSelected ? Color.Red : obj.Data.GetOne<ObjectColor>()?.Color ?? Color.Magenta;
            Vector4 colour = new Vector4(c.R, c.G, c.B, c.A) / 255f;

            VertexFlags flags = obj.IsSelected ? VertexFlags.SelectiveTransformed : VertexFlags.None;

            uint vi = 0u;
            uint si = 0u;
            uint wi = numSolidIndices;
            foreach (Vector3[] face in box.GetBoxFaces())
            {
                uint offs = vi;

                Vector3 normal = new Plane(face[0], face[1], face[2]).Normal;
                foreach (Vector3 v in face)
                {
                    points[vi++] = new VertexStandard
                    {
                        Position = v,
                        Colour = colour,
                        Normal = normal,
                        Texture = Vector2.Zero,
                        Tint = Vector4.One,
                        Flags = flags | VertexFlags.FlatColour
                    };
                }

                // Triangles - [0 1 2]  ... [0 n-1 n]
                for (uint i = 2; i < 4; i++)
                {
                    indices[si++] = offs;
                    indices[si++] = offs + i - 1;
                    indices[si++] = offs + i;
                }

                // Lines - [0 1] ... [n-1 n] [n 0]
                for (uint i = 0; i < 4; i++)
                {
                    indices[wi++] = offs + i;
                    indices[wi++] = offs + (i == 4 - 1 ? 0 : i + 1);
                }
            }

            Vector3 origin = obj.Data.GetOne<Origin>()?.Location ?? box.Center;

            List<BufferGroup> groups = new List<BufferGroup>();

            if (!obj.Data.OfType<IContentsReplaced>().Any(x => x.ContentsReplaced))
            {
                groups.Add(new BufferGroup(PipelineType.TexturedOpaque, CameraType.Perspective, 0, numSolidIndices));
            }
            
            groups.Add(new BufferGroup(PipelineType.Wireframe, obj.IsSelected ? CameraType.Both : CameraType.Orthographic, numSolidIndices, numWireframeIndices));

            builder.Append(points, indices, groups);
            
            // Also push the untransformed wireframe when selected
            if (obj.IsSelected)
            {
                for (int i = 0; i < points.Length; i++) points[i].Flags = VertexFlags.None;
                IEnumerable<uint> untransformedIndices = indices.Skip((int)numSolidIndices);
                builder.Append(points, untransformedIndices, new[]
                {
                    new BufferGroup(PipelineType.Wireframe, CameraType.Both, 0, numWireframeIndices)
                });
            }

            return Task.FromResult(0);
        }
    }
}