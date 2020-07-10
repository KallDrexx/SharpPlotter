namespace SharpPlotter.Primitives
{
    public readonly struct Point2d
    {
        public readonly float X;
        public readonly float Y;

        public Point2d(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public static Point2d operator+(Point2d first, Point2d second)
            => new Point2d(first.X + second.X, first.Y + second.Y);
        
        public static Point2d operator-(Point2d first, Point2d second)
            => new Point2d(first.X - second.X, first.Y - second.Y);
    }
}