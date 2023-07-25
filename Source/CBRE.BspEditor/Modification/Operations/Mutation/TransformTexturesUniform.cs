﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CBRE.BspEditor.Documents;
using CBRE.BspEditor.Primitives;
using CBRE.BspEditor.Primitives.MapObjects;

namespace CBRE.BspEditor.Modification.Operations.Mutation
{
    /// <summary>
    /// Uniform transform all ITextured data objects in a collection
    /// </summary>
    public class TransformTexturesUniform : IOperation
    {
        private readonly List<long> _idsToTransform;
        private readonly Matrix4x4 _matrix;
        
        public bool Trivial => false;
        
        public TransformTexturesUniform(Matrix4x4 matrix, params IMapObject[] objectsToTransform)
        {
            _matrix = matrix;
            _idsToTransform = objectsToTransform.Select(x => x.ID).ToList();
        }

        public TransformTexturesUniform(Matrix4x4 matrix, IEnumerable<IMapObject> objectsToTransform)
        {
            _matrix = matrix;
            _idsToTransform = objectsToTransform.Select(x => x.ID).ToList();
        }

        public Task<Change> Perform(MapDocument document)
        {
            Change ch = new Change(document);

            List<IMapObject> objects = _idsToTransform.Select(x => document.Map.Root.FindByID(x)).Where(x => x != null).ToList();

            foreach (IMapObject o in objects)
            {
                foreach (ITextured it in o.Data.OfType<ITextured>())
                {
                    it.Texture.TransformUniform(_matrix);
                    ch.Update(o);
                }
            }

            return Task.FromResult(ch);
        }

        public Task<Change> Reverse(MapDocument document)
        {
            if (!Matrix4x4.Invert(_matrix, out Matrix4x4 inv)) throw new Exception("Unable to reverse this operation.");
            Change ch = new Change(document);

            List<IMapObject> objects = _idsToTransform.Select(x => document.Map.Root.FindByID(x)).Where(x => x != null).ToList();

            foreach (IMapObject o in objects)
            {
                foreach (ITextured it in o.Data.OfType<ITextured>())
                {
                    it.Texture.TransformUniform(inv);
                    ch.Update(o);
                }
            }

            return Task.FromResult(ch);
        }
    }
}
