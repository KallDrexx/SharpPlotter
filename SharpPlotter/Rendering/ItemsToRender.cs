using System.Collections.Generic;

namespace SharpPlotter.Rendering
{
    public class ItemsToRender
    {
        public IReadOnlyList<RenderedPoint> Points { get; }
        public IReadOnlyList<RenderedSegment> Segments { get; }
        
        public ItemsToRender(IReadOnlyList<RenderedPoint> points, IReadOnlyList<RenderedSegment> segments)
        {
            Points = points;
            Segments = segments;
        }
    }
}