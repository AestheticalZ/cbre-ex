﻿using System.Drawing;
using CBRE.Graphics.Helpers;
using CBRE.Graphics.Renderables;
using OpenTK.Graphics.OpenGL;

namespace CBRE.Editor.Rendering {
    public class WidgetLinesRenderable : IRenderable {
        public void Render(object sender) {
            TextureHelper.Unbind();
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.FromArgb(128, Color.Red));
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(100, 0, 0);
            GL.Color3(Color.FromArgb(128, Color.Lime));
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 100, 0);
            GL.Color3(Color.FromArgb(128, Color.Blue));
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 0, 100);
            GL.End();
        }
    }
}
