﻿using CBRE.Editor.Tools;
using CBRE.Graphics.Renderables;
using CBRE.UI;

namespace CBRE.Editor.Rendering
{
    class ToolRenderable : IRenderable
    {
        public void Render(object sender)
        {
            if (ToolManager.ActiveTool == null) return;

            if ((ToolManager.ActiveTool.Usage == BaseTool.ToolUsage.Both && sender is ViewportBase)
                || (ToolManager.ActiveTool.Usage == BaseTool.ToolUsage.View2D && sender is Viewport2D)
                || (ToolManager.ActiveTool.Usage == BaseTool.ToolUsage.View3D && sender is Viewport3D))
            {
                ViewportBase vp = sender as ViewportBase;
                ToolManager.ActiveTool.Render(vp);
            }
        }
    }
}
