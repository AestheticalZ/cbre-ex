using System.Collections.Generic;
using System.Linq;
using CBRE.Common;
using CBRE.DataStructures.Models;
using CBRE.Graphics.Arrays;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CBRE.Editor.Rendering.Arrays {
    public class ModelArray : VBO<Model, MapObjectVertex> {
        private const int Textured = 0;

        public ModelArray(Model model)
            : base(new[] { model }) {
        }

        public void RenderTextured(IGraphicsContext context) {
            foreach (var subset in GetSubsets<ITexture>(Textured)) {
                ((ITexture)subset.Instance).Bind();
                Render(context, PrimitiveType.Triangles, subset);
            }
        }

        protected override void CreateArray(IEnumerable<Model> objects) {
            foreach (var model in objects) {
                PushOffset(model);

                var transforms = model.GetTransforms();

                foreach (var g in model.GetActiveMeshes().GroupBy(x => x.SkinRef)) {
                    StartSubset(Textured);
                    var tex = model.Textures[g.Key];

                    foreach (var mesh in g) {
                        foreach (var vertex in mesh.Vertices) {
                            var transform = transforms[vertex.BoneWeightings.First().Bone.BoneIndex];
                            var c = vertex.Location * transform;
                            var n = vertex.Normal * transform;
                            var index = PushData(new[]
                            {
                                new MapObjectVertex
                                {
                                    Position = new Vector3(c.X, c.Y, c.Z),
                                    Normal = new Vector3(n.X, n.Y, n.Z),
                                    Colour = Color4.White,
                                    Texture = new Vector2(vertex.TextureU, vertex.TextureV),
                                    LightmapUv = new Vector2(-500.0f, -500.0f),
                                    IsSelected = 0
                                }
                            });
                            PushIndex(Textured, index, new[] { (uint)0 });
                        }
                    }
                    PushSubset(Textured, tex.TextureObject);
                }
            }
        }
    }
}