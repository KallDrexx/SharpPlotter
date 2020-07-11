using System;
using System.Collections.Generic;
using SkiaSharp;

namespace SharpPlotter.Rendering
{
    public class Camera
    {
        /// <summary>
        /// How many pixels from the edge grid lines are allowed to extend to.  Mostly gives adequate room for labels
        /// </summary>
        private const int GridLineMargin = 20;
        
        /// <summary>
        /// How many grid lines should appear in each axis (so the display isn't cluttered when zooming out)
        /// </summary>
        private const int MaxGridLineCount = 10;
        
        /// <summary>
        /// How many pixels away from the 0 value axis a grid line should be clear from non-0 axis lines.  This cuts
        /// down on noise when a non-0 line might get drawn too close to the zero axis, and we always want the zero
        /// axis to be shown.
        /// </summary>
        private const int ZeroAxisClearance = 20;

        private readonly SKSurface _surface;
        private readonly int _width, _height, _usableWidth, _usableHeight;
        private readonly int _basePixelsPerXUnit, _basePixelsPerYUnit;
        private Point2d _origin;
        private float _zoomFactor;

        /// <summary>
        /// The X/Y coordinates on the graph the camera is centered on
        /// </summary>
        public Point2d Origin
        {
            get => _origin;
            set
            {
                _origin = value;
                CameraHasMoved = true;
            }
        }

        /// <summary>
        /// How zoomed in or out the camera should be. 1 designates that it is not zoomed in or out at all, values
        /// greater than 1 are zoomed in while numbers less than 1 are zoomed out.
        /// </summary>
        public float ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                // Don't allow negative or zero zoom values, as that that doesn't make sense
                // and messes up the calculations.
                if (value > 0)
                {
                    _zoomFactor = value;
                    CameraHasMoved = true;
                }
            }
        }
        
        /// <summary>
        /// If true than that means the camera is either in a new position or the zoom factor has changed.  This helps
        /// know if the view should be re-rendered or not.
        /// </summary>
        public bool CameraHasMoved { get; private set; }

        public Camera(int width, int height)
        {
            _width = width;
            _usableWidth = width - GridLineMargin * 2;
            _height = height;
            _usableHeight = height - GridLineMargin * 2;

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(info);

            _basePixelsPerXUnit = 90;
            _basePixelsPerYUnit = 90;
            
            Origin = new Point2d(0, 0);
            ZoomFactor = 1f;
        }

        /// <summary>
        /// Moves the camera along the graph by pixel amounts instead of graph amounts
        /// </summary>
        public void MoveByPixelAmount(int x, int y)
        {
            var horizontalUnits = x / (_basePixelsPerXUnit * ZoomFactor);
            var verticalUnits = y / (_basePixelsPerYUnit * ZoomFactor);
            
            Origin = new Point2d(Origin.X + horizontalUnits, Origin.Y + verticalUnits);
        }

        public SKImage Render(IReadOnlyList<RenderedPoint> points, IReadOnlyList<RenderedSegment> segments)
        {
            points ??= Array.Empty<RenderedPoint>();
            segments ??= Array.Empty<RenderedSegment>();
            
            _surface.Canvas.Clear(SKColors.Black);
            RenderGridLines();
            RenderSegments(segments);
            RenderPoints(points);

            CameraHasMoved = false;
            
            return _surface.Snapshot();
        }

        private void RenderGridLines()
        {
            var labelPaint = new SKPaint{Color = SKColors.White, TextAlign = SKTextAlign.Center};
            var importantLinePaint = new SKPaint{Color = SKColors.White, StrokeWidth = 2};
            var standardLinePaint = new SKPaint{
                Color = SKColors.Gray, 
                StrokeWidth = 1, 
                PathEffect = SKPathEffect.CreateDash(new[] {5f, 5f}, 5f),
            };

            DrawXAxisGraphLines(standardLinePaint, labelPaint);
            DrawXAxisLineAt(0, importantLinePaint, labelPaint, true);
            DrawYAxisGraphLines(standardLinePaint, labelPaint);
            DrawYAxisLineAt(0, importantLinePaint, labelPaint, true);
        }

        private void RenderPoints(IEnumerable<RenderedPoint> points)
        {
            foreach (var point in points)
            {
                var x = GetPixelXForGraphValue((int) point.Point.X);
                var y = GetPixelYForGraphValue((int) point.Point.Y);
                var color = new SKColor(point.Color.R, point.Color.G, point.Color.B);
                
                _surface.Canvas.DrawCircle(x, y, 5, new SKPaint{Color = color});
            }
        }

        private void RenderSegments(IEnumerable<RenderedSegment> segments)
        {
            foreach (var segment in segments)
            {
                var startX = GetPixelXForGraphValue((int) segment.Start.X);
                var startY = GetPixelYForGraphValue((int) segment.Start.Y);
                var endX = GetPixelXForGraphValue((int) segment.End.X);
                var endY = GetPixelYForGraphValue((int) segment.End.Y);
                
                var start = new SKPoint(startX, startY);
                var end = new SKPoint(endX, endY);
                var color = new SKColor(segment.Color.R, segment.Color.G, segment.Color.B);
                
                _surface.Canvas.DrawLine(start, end, new SKPaint{Color = color});
            }
        }

        private void DrawXAxisGraphLines(SKPaint linePaint, SKPaint labelPaint)
        {
            var zoomedPixelsPerXUnit = _basePixelsPerXUnit * ZoomFactor;
            var totalGraphValueCount = (int)(_usableWidth / zoomedPixelsPerXUnit);
            var valueIncrement = totalGraphValueCount <= MaxGridLineCount
                ? 1
                : (int)Math.Ceiling((float)totalGraphValueCount / MaxGridLineCount);

            var minimumDisplayedValue = (int) Origin.X - totalGraphValueCount / 2;
            for (var x = 0; x <= totalGraphValueCount; x += valueIncrement)
            {
                var value = minimumDisplayedValue + x;
                DrawXAxisLineAt(value, linePaint, labelPaint);
            }
        }
        
        private void DrawYAxisGraphLines(SKPaint linePaint, SKPaint labelPaint)
        {
            var zoomedPixelsPerYUnit = _basePixelsPerYUnit * ZoomFactor;
            var totalGraphValueCount = (int)(_usableHeight / zoomedPixelsPerYUnit);
            var valueIncrement = totalGraphValueCount <= MaxGridLineCount
                ? 1
                : (int)Math.Ceiling((float)totalGraphValueCount / MaxGridLineCount);

            var minimumDisplayedValue = (int) Origin.Y - totalGraphValueCount / 2;
            for (var x = 0; x <= totalGraphValueCount; x += valueIncrement)
            {
                var value = minimumDisplayedValue + x;
                DrawYAxisLineAt(value, linePaint, labelPaint);
            }
        }

        private void DrawXAxisLineAt(int value, SKPaint linePaint, SKPaint skPaint, bool drawIfCloseToZeroAxis = false)
        {
            var pixelX = GetPixelXForGraphValue(value);
            if (pixelX < 0 || pixelX > _width)
            {
                return;
            }
            
            if (!drawIfCloseToZeroAxis && Math.Abs(pixelX - GetPixelXForGraphValue(0)) < ZeroAxisClearance)
            {
                return;
            }

            var start = new SKPoint(pixelX, GridLineMargin);
            var end = new SKPoint(pixelX, _height - GridLineMargin);
            _surface.Canvas.DrawLine(start, end, linePaint);

            var labelPoint = new SKPoint(pixelX, end.Y + 15);
            _surface.Canvas.DrawText(value.ToString(), labelPoint, skPaint);
        }
        
        private void DrawYAxisLineAt(int value, SKPaint linePaint, SKPaint skPaint, bool drawIfCloseToZeroAxis = false)
        {
            var pixelY = GetPixelYForGraphValue(value);
            if (pixelY < 0 || pixelY > _height)
            {
                return;
            }
            
            if (!drawIfCloseToZeroAxis && Math.Abs(pixelY - GetPixelYForGraphValue(0)) < ZeroAxisClearance)
            {
                return;
            }

            var start = new SKPoint(GridLineMargin, pixelY);
            var end = new SKPoint(_width - GridLineMargin, pixelY);
            _surface.Canvas.DrawLine(start, end, linePaint);

            var labelPoint = new SKPoint(GridLineMargin, pixelY);
            _surface.Canvas.DrawText(value.ToString(), labelPoint, skPaint);
        }

        private int GetPixelXForGraphValue(int value)
        {
            var zoomedPixelsPerXUnit = _basePixelsPerXUnit * ZoomFactor;
            var distanceFromOrigin = value - Origin.X;

            var pixelsFromCenter = (int)(zoomedPixelsPerXUnit * distanceFromOrigin);
            return _width / 2 + pixelsFromCenter;
        }

        private int GetPixelYForGraphValue(int value)
        {
            var zoomedPixelsPerYUnit = _basePixelsPerYUnit * ZoomFactor;
            var distanceFromOrigin = value - Origin.Y;

            var pixelsFromCenter = (int)(zoomedPixelsPerYUnit * distanceFromOrigin);
            return _height / 2 - pixelsFromCenter;
        }
    }
}