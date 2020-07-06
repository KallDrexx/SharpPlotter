using System;
using System.Collections.Generic;
using System.Linq;
using MathPlayground.Primitives;
using SkiaSharp;

namespace MathPlayground
{
    public class Canvas : IDisposable
    {
        private const int LineMargin = 20;
        private const int ZeroAxisClearance = 20;

        private readonly List<GraphPoint2d> _points = new List<GraphPoint2d>();
        private readonly List<Path2d> _paths = new List<Path2d>();
        private readonly int _width, _height;
        private readonly SKSurface _surface;
        private int _minX, _minY, _maxX, _maxY, _horizontalLineCount, _verticalLineCount;
        private int _usableWidth, _usableHeight;
        private float _pixelsPerXUnit, _pixelsPerYUnit, _zeroCanvasX, _zeroCanvasY;

        public Canvas(int width, int height)
        {
            _width = width;
            _height = height;
            
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(info);

            _minX = 0;
            _minY = 0;
            _maxX = 0;
            _maxY = 0;
            _horizontalLineCount = 10;
            _verticalLineCount = 10;
        }

        public void Dispose()
        {
            _surface?.Dispose();
        }

        public void Clear()
        {
            _points.Clear();
            _paths.Clear();
        }

        public void DrawPoints(params GraphPoint2d[] points)
        {
            points ??= Array.Empty<GraphPoint2d>();
            foreach (var point in points)
            {
                AdjustBoundsForPoint(point);
            }
            
            _points.AddRange(points);
        }

        public void DrawPolygon(params GraphPoint2d[] points)
        {
            points ??= Array.Empty<GraphPoint2d>();
            foreach (var point in points)
            {
                AdjustBoundsForPoint(point);
            }
            
            var path = new Path2d(points, true);
            _paths.Add(path);
        }

        public SKImage Render()
        {
            RecalculateGraphBounds();
            _surface.Canvas.Clear(SKColors.Black);
            
            RenderAxes();
            RenderPoints();
            RenderPaths();

            return _surface.Snapshot();
        }

        private void RecalculateGraphBounds()
        {
            if (_minX >= _maxX || _minY >= _maxY)
            {
                var message = $"Invalid min/max bounds: X = {_minX}/{_maxX}, Y = {_minY}/{_maxY}";
            }
            
            _usableWidth = (int)(_width - LineMargin * 2.5);
            _pixelsPerXUnit = (float)_usableWidth / (_maxX - _minX);
            
            _usableHeight = (int)(_height - LineMargin * 2.5);
            _pixelsPerYUnit = (float) _usableHeight / (_maxY - _minY);

            _zeroCanvasX = GetCanvasX(0);
            _zeroCanvasY = GetCanvasY(0);
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
            var paint = new SKPaint{Color = SKColors.White};
            
            foreach (var point in _points)
            {
                var x = GetCanvasX(point.X);
                var y = GetCanvasY(point.Y);
                
                _surface.Canvas.DrawCircle(new SKPoint(x, y), 5, paint);
            }
        }

        private void RenderPaths()
        {
            var paint = new SKPaint{Color = SKColors.White};

            foreach (var path in _paths)
            {
                var firstPoint = (SKPoint?) null;
                var lastPoint = (SKPoint?) null;
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

        private void AdjustBoundsForPoint(GraphPoint2d point)
        {
            if (_minX > point.X) _minX = (int) point.X;
            if (_maxX < point.X) _maxX = (int) point.X;
            if (_minY > point.Y) _minY = (int) point.Y;
            if (_maxY < point.Y) _maxY = (int) point.Y;
        }
    }
}