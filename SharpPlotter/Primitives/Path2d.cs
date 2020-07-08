using System.Collections.Generic;

namespace SharpPlotter.Primitives
{
    public class Path2d
    {
        public IReadOnlyList<GraphPoint2d> Points { get; }
        public bool ConnectEndToBeginning { get; }

        public Path2d(GraphPoint2d[] points, bool connectEndToBeginning)
        {
            Points = points;
            ConnectEndToBeginning = connectEndToBeginning;
        }
    }
}