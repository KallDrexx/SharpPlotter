using System;
using System.Collections.Generic;
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

            _minX = -10;
            _minY = -10;
            _maxX = 10;
            _maxY = 10;
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

        public SKImage Render()
        {
            RecalculateGraphBounds();
            _surface.Canvas.Clear(SKColors.Black);
            
            DrawAxes();

            return _surface.Snapshot();
        }

        private void RecalculateGraphBounds()
        {
            _usableWidth = (int)(_width - LineMargin * 2.5);
            _pixelsPerXUnit = (float)_usableWidth / (_maxX - _minX);
            
            _usableHeight = (int)(_height - LineMargin * 2.5);
            _pixelsPerYUnit = (float) _usableHeight / (_maxY - _minY);

            _zeroCanvasX = GetCanvasX(0);
            _zeroCanvasY = GetCanvasY(0);
        }

        private void DrawAxes()
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
                var message = $"Invalid min/max for X axis values: min = {_minX}, max = {_maxX}";
                throw new InvalidOperationException(message);
            }
            
            for (var x = 0; x <= _maxX - _minX; x += xIncrement)
            {
                DrawXValueAxis(_minX + x, standardLinePaint, labelPaint);
            }

            var yIncrement = (_maxY - _minY) / _horizontalLineCount;
            if (yIncrement <= 0)
            {
                var message = $"Invalid min/max for Y axis values: min = {_minX}, max = {_maxX}";
                throw new InvalidOperationException(message);
            }
            
            for (var x = 0; x <= _maxY - _minY; x += yIncrement)
            {
                DrawYValueAxis(_minY + x, standardLinePaint, labelPaint);
            }
            
            DrawXValueAxis(0, importantLinePaint, labelPaint);
            DrawYValueAxis(0, importantLinePaint, labelPaint);
        }

        private void DrawXValueAxis(int value, SKPaint linePaint, SKPaint labelPaint)
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
        
        private void DrawYValueAxis(int value, SKPaint linePaint, SKPaint labelPaint)
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

        private float GetCanvasX(int value)
        {
            var numbersFromStart = value - _minX;
            return LineMargin * 1.5f + _pixelsPerXUnit * numbersFromStart;
        }

        private float GetCanvasY(int value)
        {
            var numbersFromStart = value - _minY;
            return _height - LineMargin * 1.5f - _pixelsPerYUnit * numbersFromStart;
        }
    }
}