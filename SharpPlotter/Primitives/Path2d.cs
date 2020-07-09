using System.Collections.Generic;
using System.Linq;

namespace SharpPlotter.Primitives
{
    public class Path2d
    {
        public IReadOnlyList<GraphPoint2d> Points { get; }
        public bool ConnectEndToBeginning { get; }

        public Path2d((float x, float y)[] points, bool connectEndToBeginning)
        {
            Points = points.Select(point => new GraphPoint2d(point.x, point.y)).ToArray();
            ConnectEndToBeginning = connectEndToBeginning;
        }
    }
}