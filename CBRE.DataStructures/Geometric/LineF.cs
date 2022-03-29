﻿using CBRE.DataStructures.Transformations;
using System;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Geometric
{
    [Serializable]
    public class LineF : ISerializable
    {
        public CoordinateF Start { get; set; }
        public CoordinateF End { get; set; }

        public readonly static LineF AxisX = new LineF(CoordinateF.Zero, CoordinateF.UnitX);
        public readonly static LineF AxisY = new LineF(CoordinateF.Zero, CoordinateF.UnitY);
        public static readonly LineF AxisZ = new LineF(CoordinateF.Zero, CoordinateF.UnitZ);

        public LineF(CoordinateF start, CoordinateF end)
        {
            Start = start;
            End = end;
        }

        protected LineF(SerializationInfo info, StreamingContext context)
        {
            Start = (CoordinateF)info.GetValue("Start", typeof(CoordinateF));
            End = (CoordinateF)info.GetValue("End", typeof(CoordinateF));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Start", Start);
            info.AddValue("End", End);
        }

        public LineF Reverse()
        {
            return new LineF(End, Start);
        }

        public CoordinateF ClosestPoint(CoordinateF point)
        {
            // http://paulbourke.net/geometry/pointline/

            CoordinateF delta = End - Start;
            float den = delta.LengthSquared();
            if (den == 0) return Start; // Start and End are the same

            CoordinateF numPoint = (point - Start).ComponentMultiply(delta);
            float num = numPoint.X + numPoint.Y + numPoint.Z;
            float u = num / den;

            if (u < 0) return Start; // Point is before the segment start
            if (u > 1) return End;   // Point is after the segment end
            return Start + u * delta;
        }

        /// <summary>
        /// Determines if this line is behind, in front, or spanning a plane.
        /// </summary>
        /// <param name="p">The plane to test against</param>
        /// <returns>A PlaneClassification value.</returns>
        public PlaneClassification ClassifyAgainstPlane(PlaneF p)
        {
            int start = p.OnPlane(Start);
            int end = p.OnPlane(End);

            if (start == 0 && end == 0) return PlaneClassification.OnPlane;
            if (start <= 0 && end <= 0) return PlaneClassification.Back;
            if (start >= 0 && end >= 0) return PlaneClassification.Front;
            return PlaneClassification.Spanning;
        }

        public LineF Transform(IUnitTransformation transform)
        {
            return new LineF(transform.Transform(Start), transform.Transform(End));
        }

        public bool EquivalentTo(LineF other, float delta = 0.0001f)
        {
            return (Start.EquivalentTo(other.Start, delta) && End.EquivalentTo(other.End, delta))
                || (End.EquivalentTo(other.Start, delta) && Start.EquivalentTo(other.End, delta));
        }

        public bool Equals(LineF other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return (Equals(other.Start, Start) && Equals(other.End, End))
                || (Equals(other.End, Start) && Equals(other.Start, End));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(LineF)) return false;
            return Equals((LineF)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Start != null ? Start.GetHashCode() : 0) * 397) ^ (End != null ? End.GetHashCode() : 0);
            }
        }

        public static bool operator ==(LineF left, LineF right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LineF left, LineF right)
        {
            return !Equals(left, right);
        }
    }
}
