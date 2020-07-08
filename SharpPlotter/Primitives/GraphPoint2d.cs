namespace SharpPlotter.Primitives
{
    public readonly struct GraphPoint2d
    {
        public readonly float X;
        public readonly float Y;

        public GraphPoint2d(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public static implicit operator GraphPoint2d((float x, float y) value) => new GraphPoint2d(value.x, value.y); 
    }
}