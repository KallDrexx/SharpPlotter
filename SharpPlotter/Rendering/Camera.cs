using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
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

        /// <summary>
        /// How many pixels a whole graph unit takes up by default on any given axis.  Used primarily for resetting
        /// the aspect ratio on the graph.
        /// </summary>
        private const int StandardPixelsPerGraphUnit = 90;

        /// <summary>
        /// The number of pixels between each function call.  This allows us to not have to run the function for every
        /// horizontal pixel and can help cut with performance.  Instead lines will be drawn between each function
        /// called value
        /// </summary>
        private const int FunctionPixelResolution = 10;

        private readonly OnScreenLogger _onScreenLogger;
        private int _width, _height, _usableWidth, _usableHeight;
        private SKSurface _surface;
        private int _basePixelsPerXUnit, _basePixelsPerYUnit;
        private Point2d _origin;
        private float _zoomFactor;
        private bool _hideGridLines;

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
                RecalculateGraphBounds();
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
                    RecalculateGraphBounds();
                }
            }
        }

        /// <summary>
        /// When true, the grid lines will not be rendered
        /// </summary>
        public bool HideGridLines
        {
            get => _hideGridLines;
            set
            {
                _hideGridLines = value;
                CameraHasMoved = true;
            }
        }
        
        /// <summary>
        /// the smallest X and Y graph values on the visible portion of the graph
        /// </summary>
        public Point2d MinimumGraphBounds { get; private set; }
        
        /// <summary>
        /// The largest X and Y graph values on the visible portion of the graph
        /// </summary>
        public Point2d MaximumGraphBounds { get; private set; }
        
        /// <summary>
        /// If true than that means the camera is either in a new position or the zoom factor has changed.  This helps
        /// know if the view should be re-rendered or not.
        /// </summary>
        public bool CameraHasMoved { get; private set; }

        public Camera(int width, int height, OnScreenLogger onScreenLogger)
        {
            ResizeViewport(height, width);

            _onScreenLogger = onScreenLogger;
            
            _basePixelsPerXUnit = StandardPixelsPerGraphUnit;
            _basePixelsPerYUnit = StandardPixelsPerGraphUnit;
            
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

        /// <summary>
        /// Allows adjusting how many graph coordinates are viewable on a horizontal or vertical basis, instead of
        /// forcing the graph to always be locked in a specific aspect ratio.  A positive change value effectively
        /// increases the field of view and allows more values to be visible
        /// </summary>
        public void ChangeFieldOfView(int horizontalChange, int verticalChange)
        {
            _basePixelsPerXUnit -= horizontalChange;
            _basePixelsPerYUnit -= verticalChange;

            if (_basePixelsPerXUnit < 1) _basePixelsPerXUnit = 1;
            if (_basePixelsPerYUnit < 1) _basePixelsPerYUnit = 1;

            CameraHasMoved = true;
        }

        /// <summary>
        /// Resets the field of view values to match the default aspect ratio and values
        /// </summary>
        public void ResetFieldOfView()
        {
            _basePixelsPerXUnit = StandardPixelsPerGraphUnit;
            _basePixelsPerYUnit = StandardPixelsPerGraphUnit;

            CameraHasMoved = true;
        }

        /// <summary>
        /// Sets the camera to a predefined position and field of view that guarantees all 4 edges of the screen
        /// represent the defined boundary points on the graph
        /// </summary>
        public void SetGraphBounds((float min, float max) x, (float min, float max) y)
        {
            if (x.min >= x.max || y.min >= y.max)
            {
                var message = $"Invalid graph boundaries specified: X={x.min}/{x.max}, y={y.min}/{y.max}";
                _onScreenLogger.LogMessage(message);

                return;
            }

            // Without a buffer the points around the edge are right up against the edge of the canvas, which makes
            // it inconvenient to look at.  So we want to make sure there is some graph space available around the edge
            // points.
            const int boundsBufferInPixels = 40;
            
            _basePixelsPerXUnit = (int)((_usableWidth - boundsBufferInPixels * 2) / (x.max - x.min));
            _basePixelsPerYUnit = (int)((_usableHeight - boundsBufferInPixels * 2) / (y.max - y.min));

            var originX = x.max - (x.max - x.min) / 2f;
            var originY = y.max - (y.max - y.min) / 2f;
            
            Origin = new Point2d(originX, originY);
            ZoomFactor = 1f;
            CameraHasMoved = true;
        }

        /// <summary>
        /// Updates the height and width of the target image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ResizeViewport(int width, int height)
        {
            _width = width;
            _usableWidth = width - GridLineMargin * 2;
            _height = height;
            _usableHeight = height - GridLineMargin * 2;
            
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(info);

            CameraHasMoved = true;
        }

        /// <summary>
        /// Returns the X/Y point on the grid for a specific pixel coordinates.  Will return null if the pixel
        /// coordinates refer to a part of the graph that's off screen
        /// </summary>
        public Point2d? GetGraphPointForPixelCoordinates(int x, int y)
        {
            if (x < GridLineMargin || 
                y < GridLineMargin || 
                x > _usableWidth + GridLineMargin || 
                y > _usableHeight + GridLineMargin)
            {
                // Off screen
                return null;
            }

            var horizontalPixelsFromCenter = x - _width / 2;
            var verticalPixelsFromCenter = y - _height / 2;
            
            var horizontalUnitsPerPixel = _basePixelsPerXUnit * ZoomFactor;
            var verticalUnitsPerPixel = _basePixelsPerYUnit * ZoomFactor;

            var gridX = Origin.X + horizontalPixelsFromCenter / horizontalUnitsPerPixel;
            var gridY = Origin.Y - verticalPixelsFromCenter / verticalUnitsPerPixel;
            
            return new Point2d(gridX, gridY);
        }

        public SKImage Render(ItemsToRender itemsToRender)
        {
            var points = itemsToRender?.Points ?? Array.Empty<RenderedPoint>();
            var segments = itemsToRender?.Segments ?? Array.Empty<RenderedSegment>();
            var functions = itemsToRender?.Functions ?? Array.Empty<RenderedFunction>();
            var arrows = itemsToRender?.Arrows ?? Array.Empty<RenderedArrow>();
            var polygons = itemsToRender?.Polygons ?? Array.Empty<RenderedPolygon>();
            
            _surface.Canvas.Clear(SKColors.Black);
            RenderGridLines();
            RenderPolygons(polygons);
            RenderSegments(segments);
            RenderPoints(points);
            RenderFunctions(functions);
            RenderArrows(arrows);

            CameraHasMoved = false;
            
            return _surface.Snapshot();
        }

        private void RecalculateGraphBounds()
        {
            var zoomedPixelsPerXUnit = _basePixelsPerXUnit * ZoomFactor;
            var zoomedPixelsPerYUnit = _basePixelsPerYUnit * ZoomFactor;
            var horizontalGraphValueCount = (int)(_usableWidth / zoomedPixelsPerXUnit);
            var verticalGraphValueCount = (int) (_usableHeight / zoomedPixelsPerYUnit);
            var minimumHorizontalGraphValue = (int) Math.Floor(Origin.X - (float) horizontalGraphValueCount / 2);
            var maximumHorizontalGraphValue = (int) Math.Ceiling(Origin.X + (float) horizontalGraphValueCount / 2);
            var minimumVerticalGraphValue = (int) Math.Floor(Origin.Y - (float) verticalGraphValueCount / 2);
            var maximumVerticalGraphValue = (int) Math.Ceiling(Origin.Y + (float) verticalGraphValueCount / 2);
            
            MinimumGraphBounds = new Point2d(minimumHorizontalGraphValue, minimumVerticalGraphValue);
            MaximumGraphBounds = new Point2d(maximumHorizontalGraphValue, maximumVerticalGraphValue);
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
                var x = GetPixelXForGraphValue(point.Point.X);
                var y = GetPixelYForGraphValue(point.Point.Y);
                var color = new SKColor(point.Color.R, point.Color.G, point.Color.B);
                
                _surface.Canvas.DrawCircle(x, y, 5, new SKPaint{Color = color});
            }
        }

        private void RenderSegments(IEnumerable<RenderedSegment> segments)
        {
            foreach (var segment in segments)
            {
                var startX = GetPixelXForGraphValue(segment.Start.X);
                var startY = GetPixelYForGraphValue(segment.Start.Y);
                var endX = GetPixelXForGraphValue(segment.End.X);
                var endY = GetPixelYForGraphValue(segment.End.Y);
                
                var start = new SKPoint(startX, startY);
                var end = new SKPoint(endX, endY);
                var color = new SKColor(segment.Color.R, segment.Color.G, segment.Color.B);
                
                _surface.Canvas.DrawLine(start, end, new SKPaint{Color = color});
            }
        }

        private void RenderPolygons(IEnumerable<RenderedPolygon> polygons)
        {
            foreach (var polygon in polygons)
            {
                var path = new SKPath();
                for (var x = 0; x < polygon.Points.Count; x++)
                {
                    var xPos = GetPixelXForGraphValue(polygon.Points[x].X);
                    var yPos = GetPixelYForGraphValue(polygon.Points[x].Y);
                    
                    if (x == 0)
                    {
                        path.MoveTo(xPos, yPos);
                    }
                    else
                    {
                        path.LineTo(xPos, yPos);
                    }
                }
                
                path.Close();
                var color = new SKColor(polygon.FillColor.R, polygon.FillColor.G, polygon.FillColor.B);
                var paint = new SKPaint
                {
                    Color = color,
                    Style = SKPaintStyle.Fill
                };
                
                _surface.Canvas.DrawPath(path, paint);
            }
        }

        private void RenderFunctions(IEnumerable<RenderedFunction> renderedFunctions)
        {
            var minGraphValue = GetMinHorizontalGraphValue();
            var horizontalUnits = 1 / (_basePixelsPerXUnit * ZoomFactor);
            
            foreach (var renderedFunction in renderedFunctions)
            {
                var paint = new SKPaint
                {
                    Color = new SKColor(
                        renderedFunction.Color.R,
                        renderedFunction.Color.G,
                        renderedFunction.Color.B)
                };
                
                var currentColumn = 0;
                var lastPoint = (SKPoint?) null;
                while (currentColumn < _usableWidth)
                {
                    var pixelColumn = currentColumn + GridLineMargin;
                    var graphXValue = horizontalUnits * currentColumn + minGraphValue;
                    var graphYValue = renderedFunction.Function(graphXValue);
                    var pixelRow = GetPixelYForGraphValue(graphYValue);
                    var currentPoint = new SKPoint(pixelColumn, pixelRow);
                    if (lastPoint != null)
                    {
                        _surface.Canvas.DrawLine(lastPoint.Value, currentPoint, paint);
                    }

                    lastPoint = currentPoint;
                    currentColumn += FunctionPixelResolution;
                }
            }
        }

        private void RenderArrows(IEnumerable<RenderedArrow> arrows)
        {
            foreach (var arrow in arrows)
            {
                var paint = new SKPaint
                {
                    Color = new SKColor(arrow.Color.R, arrow.Color.G, arrow.Color.B),
                    Style = SKPaintStyle.Fill,
                };
                
                var graphStartX = GetPixelXForGraphValue(arrow.Start.X);
                var graphStartY = GetPixelYForGraphValue(arrow.Start.Y);
                var graphEndX = GetPixelXForGraphValue(arrow.End.X);
                var graphEndY = GetPixelYForGraphValue(arrow.End.Y);
                
                _surface.Canvas.DrawLine(graphStartX, graphStartY, graphEndX, graphEndY, paint);
                
                // Draw triangle pointing at the end point
                const float sideLength = 10;
                const double halfTriangleAngleRadians = 30 * (Math.PI / 180); // assumes equilateral triangle

                var changeInX = (float) graphEndX - graphStartX;
                var changeInY = (float) graphEndY - graphStartY; // Start - end since canvas pixels are larger on the bottom
                
                // We need to "rotate" the calculated angle by 180 degrees due to the Y axis being reversed on the 
                // canvas (Y increases as we go down canvas).
                var lineAngleInRadians = Math.PI + Math.Atan2(changeInY,  changeInX);
                if (double.IsNaN(lineAngleInRadians))
                {
                    lineAngleInRadians = 0;
                }
                
                // We know how long the left and right sides are, so we can find out where each end point is by 
                // visualizing a circle of `sideLength` radius, and use trig to find the x and y offsets to the rotated
                // of the point after it has been rotated by the same angle as the line angle
                var leftYAdjust = (float)(sideLength * Math.Sin(lineAngleInRadians + halfTriangleAngleRadians));
                var leftXAdjust = (float)(sideLength * Math.Cos(lineAngleInRadians + halfTriangleAngleRadians));
                var rightYAdjust = (float)(sideLength * Math.Sin(lineAngleInRadians - halfTriangleAngleRadians));
                var rightXAdjust = (float)(sideLength * Math.Cos(lineAngleInRadians - halfTriangleAngleRadians));
                
                var point1 = new SKPoint(graphEndX, graphEndY);
                var point2 = new SKPoint(graphEndX + leftXAdjust, graphEndY + leftYAdjust);
                var point3 = new SKPoint(graphEndX + rightXAdjust, graphEndY + rightYAdjust);
                
                var path = new SKPath();
                path.MoveTo(point1);
                path.LineTo(point2);
                path.LineTo(point3);
                path.Close();
                
                _surface.Canvas.DrawPath(path, paint);
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
            
            if (!HideGridLines)
            {
                _surface.Canvas.DrawLine(start, end, linePaint);
            }

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
            
            if (!HideGridLines)
            {
                _surface.Canvas.DrawLine(start, end, linePaint);
            }

            var labelPoint = new SKPoint(GridLineMargin, pixelY);
            _surface.Canvas.DrawText(value.ToString(), labelPoint, skPaint);
        }

        private int GetPixelXForGraphValue(float value)
        {
            var zoomedPixelsPerXUnit = _basePixelsPerXUnit * ZoomFactor;
            var distanceFromOrigin = value - Origin.X;

            var pixelsFromCenter = (int)(zoomedPixelsPerXUnit * distanceFromOrigin);
            return _width / 2 + pixelsFromCenter;
        }

        private int GetPixelYForGraphValue(float value)
        {
            var zoomedPixelsPerYUnit = _basePixelsPerYUnit * ZoomFactor;
            var distanceFromOrigin = value - Origin.Y;

            var pixelsFromCenter = (int)(zoomedPixelsPerYUnit * distanceFromOrigin);
            return _height / 2 - pixelsFromCenter;
        }

        private float GetMinHorizontalGraphValue()
        {
            var zoomedPixelsPerXUnit = _basePixelsPerXUnit * ZoomFactor;
            var totalGraphValueCount = _usableWidth / zoomedPixelsPerXUnit;
            var minimumDisplayedValue = Origin.X - totalGraphValueCount / 2;

            return minimumDisplayedValue;
        }
    }
}