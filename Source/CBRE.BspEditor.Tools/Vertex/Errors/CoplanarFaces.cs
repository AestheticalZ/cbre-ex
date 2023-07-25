﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using CBRE.BspEditor.Tools.Vertex.Selection;

namespace CBRE.BspEditor.Tools.Vertex.Errors
{
    [Export(typeof(IVertexErrorCheck))]
    public class CoplanarFaces : IVertexErrorCheck
    {
        private const string Key = "CBRE.BspEditor.Tools.Vertex.Errors.CoplanarFaces";

        private IEnumerable<MutableFace> GetCoplanarFaces(MutableSolid solid)
        {
            List<MutableFace> faces = solid.Faces.ToList();
            return faces.Where(f1 => faces.Where(f2 => f2 != f1).Any(f2 => f2.Plane == f1.Plane));
        }

        public IEnumerable<VertexError> GetErrors(VertexSolid solid)
        {
            foreach (IGrouping<DataStructures.Geometric.Plane, MutableFace> group in GetCoplanarFaces(solid.Copy).GroupBy(x => x.Plane))
            {
                yield return new VertexError(Key, solid).Add(group);
            }
        }
    }
}