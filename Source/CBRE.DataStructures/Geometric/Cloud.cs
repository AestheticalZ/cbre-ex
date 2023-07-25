﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric
{
    /// <summary>
    /// A cloud is a wrapper around a collection of points, allowing
    /// various useful operations to be performed on them.
    /// </summary>
    [Serializable]
    public class Cloud : ISerializable
    {
        public List<Vector3> Points { get; }
        public Box BoundingBox { get; }

        public Vector3 MinX { get; }
        public Vector3 MinY { get; }
        public Vector3 MinZ { get; }
        public Vector3 MaxX { get; }
        public Vector3 MaxY { get; }
        public Vector3 MaxZ { get; }

        public Cloud(IEnumerable<Vector3> points)
        {
            List<Vector3> list = points.ToList();

            Points = new List<Vector3>(list);
            BoundingBox = new Box(list);

            MinX = MinY = MinZ = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            MaxX = MaxY = MaxZ = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Vector3 p in list)
            {
                if (p.X < MinX.X) MinX = p;
                if (p.Y < MinY.Y) MinY = p;
                if (p.Z < MinZ.Z) MinZ = p;
                if (p.X > MaxX.X) MaxX = p;
                if (p.Y > MaxY.Y) MaxY = p;
                if (p.Z > MaxZ.Z) MaxZ = p; 
            }
        }

        protected Cloud(SerializationInfo info, StreamingContext context) : this((Vector3[]) info.GetValue("Points", typeof(Vector3[])))
        {

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Points", Points.ToArray());
        }

        /// <summary>
        /// Get a list of the 6 points that define the outermost extents of this cloud.
        /// </summary>
        /// <returns>A list of the 6 (Min|Max)(X|Y|Z) values of this cloud.</returns>
        public IEnumerable<Vector3> GetExtents()
        {
            return new[]
                       {
                           MinX, MinY, MinZ,
                           MaxX, MaxY, MaxZ
                        };
        }
    }
}
