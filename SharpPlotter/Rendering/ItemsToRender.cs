using System.Collections.Generic;

namespace SharpPlotter.Rendering
{
    public class ItemsToRender
    {
        public IReadOnlyList<RenderedPoint> Points { get; }
        public IReadOnlyList<RenderedSegment> Segments { get; }
        public IReadOnlyList<RenderedFunction> Functions { get; }
        
        public ItemsToRender(IReadOnlyList<RenderedPoint> points, 
            IReadOnlyList<RenderedSegment> segments, 
            IReadOnlyList<RenderedFunction> functions)
        {
            Points = points;
            Segments = segments;
            Functions = functions;
        }
    }
}