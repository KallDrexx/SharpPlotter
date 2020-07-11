namespace SharpPlotter
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

        public static implicit operator Point2d((float x, float y) point) => new Point2d(point.x, point.y); 
    }
}