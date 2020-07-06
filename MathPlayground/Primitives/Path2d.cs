namespace MathPlayground.Primitives
{
    public class Path2d
    {
        private readonly GraphPoint2d[] _points;
        private readonly bool _connectEndToBeginning;

        public Path2d(GraphPoint2d[] points, bool connectEndToBeginning)
        {
            _points = points;
            _connectEndToBeginning = connectEndToBeginning;
        }
    }
}