using System;

namespace SharpPlotter
{
    public readonly struct Point2d : IEquatable<Point2d>
    {
        public readonly float X;
        public readonly float Y;

        public Point2d(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Point2d((float x, float y) point) => new Point2d(point.x, point.y);

        public static bool operator ==(Point2d first, Point2d second)
        {
            return Math.Abs(first.X - second.X) < 0.0001f &&
                   Math.Abs(first.Y - second.Y) < 0.000f;
        }

        public static bool operator !=(Point2d first, Point2d second)
        {
            return !(first == second);
        }

        public bool Equals(Point2d other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is Point2d other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}