using System;
using System.Collections.Generic;

namespace SharpPlotter.Rendering
{
    public class ItemsToRender
    {
        public IReadOnlyList<RenderedPoint> Points { get; }
        public IReadOnlyList<RenderedSegment> Segments { get; }
        public IReadOnlyList<RenderedFunction> Functions { get; }
        public IReadOnlyList<RenderedArrow> Arrows { get; }
        
        public ItemsToRender(IReadOnlyList<RenderedPoint> points, 
            IReadOnlyList<RenderedSegment> segments, 
            IReadOnlyList<RenderedFunction> functions, 
            IReadOnlyList<RenderedArrow> arrows)
        {
            Points = points ?? Array.Empty<RenderedPoint>();
            Segments = segments ?? Array.Empty<RenderedSegment>();
            Functions = functions ?? Array.Empty<RenderedFunction>();
            Arrows = arrows ?? Array.Empty<RenderedArrow>();
        }
    }
}