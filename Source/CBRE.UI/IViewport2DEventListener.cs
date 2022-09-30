﻿using CBRE.DataStructures.Geometric;

namespace CBRE.UI
{
    public interface IViewport2DEventListener : IViewportEventListener
    {
        void ZoomChanged(decimal oldZoom, decimal newZoom);
        void PositionChanged(Coordinate oldPosition, Coordinate newPosition);
    }
}
