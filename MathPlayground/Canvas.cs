using System;
using System.Linq;
using MathPlayground.Primitives;
using SkiaSharp;

namespace MathPlayground
{
    public class Canvas : IDisposable
    {
        private const int LineMargin = 20;
        private const int ZeroAxisClearance = 20;

        private readonly GraphItems _graphItems;
        private readonly int _width, _height;
        private readonly SKSurface _surface;
        private readonly int _horizontalLineCount, _verticalLineCount;
        private int _usableWidth, _usableHeight;
        private float _pixelsPerXUnit, _pixelsPerYUnit, _zeroCanvasX, _zeroCanvasY;
        private int _minX, _maxX, _minY, _maxY;
        private bool _dynamicGraphBounds = true;
        
        public Canvas(int width, int height)
        {
            _graphItems = new GraphItems();
            _width = width;
            _height = height;
            
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(info);
            
            _horizontalLineCount = 10;
            _verticalLineCount = 10;
        }

        public void Dispose()
        {
            _surface?.Dispose();
        }

        public void Clear()
        {
            _graphItems.Clear();
        }

        /// <summary>
        /// Constrains the graph to a specific set of x and y coordinate values
        /// </summary>
        public void SetGraphBounds(int minX, int maxX, int minY, int maxY)
        {
            if (minX >= maxX || minY >= maxY)
            {
                var message = $"Invalid min/max bounds: X = {minX}/{maxX}, Y = {minY}/{maxY}";
                throw new InvalidOperationException(message);
            }

            _minX = minX;
            _maxX = maxX;
            _minY = minY;
            _maxY = maxY;

            _dynamicGraphBounds = false;
        }

        /// <summary>
        /// Set the graph to constrain itself to the enclosed items
        /// </summary>
        public void EnableDynamicGraphBounds()
        {
            _dynamicGraphBounds = true;
        }

        public void DrawPoints(params GraphPoint2d[] points)
        {
            _graphItems.AddPoints(points);
        }

        public void DrawPoints(Color color, params GraphPoint2d[] points)
        {
            _graphItems.AddPoints(points, color);
        }

        public void DrawPolygon(params GraphPoint2d[] points)
        {
            if (points?.Any() != true)
            {
                return;
            }
            
            _graphItems.AddPath(new Path2d(points, true));
        }
        
        public void DrawPolygon(Color color, params GraphPoint2d[] points)
        {
            if (points?.Any() != true)
            {
                return;
            }
            
            _graphItems.AddPath(new Path2d(points, true), color);
        }

        public void DrawSegments(params GraphPoint2d[] points)
        {
            if (points?.Any() != true)
            {
                return;
            }
            
            _graphItems.AddPath(new Path2d(points, false));
        }
        
        public void DrawSegments(Color color, params GraphPoint2d[] points)
        {
            if (points?.Any() != true)
            {
                return;
            }
            
            _graphItems.AddPath(new Path2d(points, false), color);
        }

        public SKImage Render()
        {
            if (_dynamicGraphBounds)
            {
                if (_graphItems.HasAnyItems)
                {
                    _minX = (int) _graphItems.MinX - 1;
                    _minY = (int) _graphItems.MinY - 1;
                    _maxX = (int) _graphItems.MaxX + 1;
                    _maxY = (int) _graphItems.MaxY + 1;
                }
                else
                {
                    _minX = -10;
                    _minY = -10;
                    _maxX = 10;
                    _maxX = 10;
                }
            }
            
            _usableWidth = (int)(_width - LineMargin * 2.5);
            _pixelsPerXUnit = (float)_usableWidth / (_maxX - _minX);
            
            _usableHeight = (int)(_height - LineMargin * 2.5);
            _pixelsPerYUnit = (float) _usableHeight / (_maxY - _minY);

            _zeroCanvasX = GetCanvasX(0);
            _zeroCanvasY = GetCanvasY(0);
            
            _surface.Canvas.Clear(SKColors.Black);
            
            RenderAxes();
            RenderPoints();
            RenderPaths();

            return _surface.Snapshot();
        }

        private void RenderAxes()
        {
            var importantLinePaint = new SKPaint{Color = SKColors.White, StrokeWidth = 2};
            var standardLinePaint = new SKPaint{
                Color = SKColors.Gray, 
                StrokeWidth = 1, 
                PathEffect = SKPathEffect.CreateDash(new[] {5f, 5f}, 5f),
            };
            
            var labelPaint = new SKPaint{Color = SKColors.White, TextAlign = SKTextAlign.Center};
            
            var xIncrement = (_maxX - _minX) / _verticalLineCount;
            if (xIncrement <= 0)
            {
                xIncrement = 1;
            }
            
            for (var x = 0; x <= _maxX - _minX; x += xIncrement)
            {
                RenderXValueAxis(_minX + x, standardLinePaint, labelPaint);
            }

            var yIncrement = (_maxY - _minY) / _horizontalLineCount;
            if (yIncrement <= 0)
            {
                yIncrement = 1;
            }
            
            for (var x = 0; x <= _maxY - _minY; x += yIncrement)
            {
                RenderYValueAxis(_minY + x, standardLinePaint, labelPaint);
            }
            
            RenderXValueAxis(0, importantLinePaint, labelPaint);
            RenderYValueAxis(0, importantLinePaint, labelPaint);
        }

        private void RenderPoints()
        {
            foreach (var point in _graphItems.Points)
            {
                var x = GetCanvasX(point.X);
                var y = GetCanvasY(point.Y);

                var paint = _graphItems.ColorMap.TryGetValue(point, out var color)
                    ? new SKPaint {Color = color}
                    : new SKPaint {Color = SKColors.White}; 
                
                _surface.Canvas.DrawCircle(new SKPoint(x, y), 5, paint);
            }
        }

        private void RenderPaths()
        {
            foreach (var path in _graphItems.Paths)
            {
                var firstPoint = (SKPoint?) null;
                var lastPoint = (SKPoint?) null;
                
                var paint = _graphItems.ColorMap.TryGetValue(path, out var color)
                    ? new SKPaint {Color = color}
                    : new SKPaint {Color = SKColors.White}; 
                
                foreach (var point in path.Points)
                {
                    var x = GetCanvasX(point.X);
                    var y = GetCanvasY(point.Y);
                    var currentPoint = new SKPoint(x, y);
                    
                    if (lastPoint != null)
                    {
                        _surface.Canvas.DrawLine(lastPoint.Value, currentPoint, paint);
                    }
                    else
                    {
                        firstPoint = currentPoint;
                    }

                    lastPoint = currentPoint;
                }

                if (path.ConnectEndToBeginning && firstPoint != null && lastPoint != null && firstPoint != lastPoint)
                {
                    _surface.Canvas.DrawLine(lastPoint.Value, firstPoint.Value, paint);
                }
            }
        }

        private void RenderXValueAxis(int value, SKPaint linePaint, SKPaint labelPaint)
        {
            var canvasX = GetCanvasX(value);
            if (value != 0 && Math.Abs(canvasX - _zeroCanvasX) < ZeroAxisClearance)
            {
                // We aren't drawing the 0 axis value, and this value is too close to where we would
                // draw the 0 axis value, so ignore it
                return;
            }
            
            var start = new SKPoint(canvasX, LineMargin);
            var end = new SKPoint(canvasX, _height - LineMargin);
            _surface.Canvas.DrawLine(start, end, linePaint);
                
            var labelPoint = new SKPoint(canvasX, end.Y + 15);
            _surface.Canvas.DrawText(value.ToString(), labelPoint, labelPaint);
        }
        
        private void RenderYValueAxis(int value, SKPaint linePaint, SKPaint labelPaint)
        {
            var canvasY = GetCanvasY(value);
            if (value != 0 && Math.Abs(canvasY - _zeroCanvasY) < ZeroAxisClearance)
            {
                // We aren't drawing the 0 axis value, and this value is too close to where we would
                // draw the 0 axis value, so ignore it
                return;
            }
            
            var start = new SKPoint(LineMargin, canvasY);
            var end = new SKPoint(_width - LineMargin, canvasY);
            _surface.Canvas.DrawLine(start, end, linePaint);
                
            var labelPoint = new SKPoint(LineMargin, canvasY);
            _surface.Canvas.DrawText(value.ToString(), labelPoint, labelPaint);
        }

        private float GetCanvasX(float value)
        {
            var numbersFromStart = value - _minX;
            return LineMargin * 1.5f + _pixelsPerXUnit * numbersFromStart;
        }

        private float GetCanvasY(float value)
        {
            var numbersFromStart = value - _minY;
            return _height - LineMargin * 1.5f - _pixelsPerYUnit * numbersFromStart;
        }
    }
}