using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using IronPython.Runtime;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SharpPlotter.Rendering;

namespace SharpPlotter
{
    public class GraphedItems
    {
        private readonly List<Frame> _frames;
        private int _currentRenderedFrameIndex;
        private double _secondsSinceCreation;
        private Point2d? _minCoordinates, _maxCoordinates;

        public Queue<string> Messages { get; } = new Queue<string>();

        /// <summary>
        /// The smallest X and Y values across all graphed items
        /// </summary>
        public Point2d? MinCoordinates
        {
            get
            {
                if (_minCoordinates == null)
                {
                    RecalculateMinAndMaxCoordinates();
                }

                return _minCoordinates;
            }
        }
        
        /// <summary>
        /// The largest X and Y values across all graphed items
        /// </summary>
        public Point2d? MaxCoordinates
        {
            get
            {
                if (_maxCoordinates == null)
                {
                    RecalculateMinAndMaxCoordinates();
                }

                return _maxCoordinates;
            }
        }

        /// <summary>
        /// How long each frame should be rendered
        /// </summary>
        public double SecondsPerFrame { get; set; } = 1;

        public GraphedItems()
        {
            _frames = new List<Frame> {new Frame()};

            // Always start with true, as if this is a new object then obviously something has changed.
            ItemsChangedSinceLastRender = true;
        }
        
        /// <summary>
        /// Returns true if any changes have been made to any collection of graph-able items.  This resets any time
        /// the list of items to render is retrieved.
        /// </summary>
        public bool ItemsChangedSinceLastRender { get; private set; }

        /// <summary>
        /// Adds points to the graph set to the specified color
        /// </summary>
        public void AddPoints(Color color, IEnumerable<Point2d> points)
        {
            points ??= Array.Empty<Point2d>();
            
            _frames[^1].Points.AddRange(points.Select(x => new RenderedPoint(x, color)));
            GraphItemsUpdated();
        }

        /// <summary>
        /// Adds line segments to the graph from each point specified to the next point.  
        /// </summary>
        public void AddSegments(Color color, IEnumerable<Point2d> points)
        {
            points ??= Array.Empty<Point2d>();

            var lastPoint = (Point2d?) null;
            foreach (var point in points)
            {
                if (lastPoint != null)
                {
                    _frames[^1].Segments.Add(new RenderedSegment(lastPoint.Value, point, color));
                }

                lastPoint = point;
            };
            
            GraphItemsUpdated();
        }

        /// <summary>
        /// Adds an unbounded function that will be rendered, with values calculated on demand
        /// </summary>
        public void AddFunction(Color color, Func<float, float> function)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
            
            _frames[^1].Functions.Add(new RenderedFunction(color, function));
        }

        /// <summary>
        /// Adds a line segment with a pointer at the end from the starting point to and ending point
        /// </summary>
        public void AddArrow(Color color, Point2d start, Point2d end)
        {
            _frames[^1].Arrows.Add(new RenderedArrow(start, end, color));
            GraphItemsUpdated();
        }

        /// <summary>
        /// Adds a filled in polygon between 3 or more points
        /// </summary>
        public void AddPolygon(Color color, IEnumerable<Point2d> points)
        {
            points ??= Array.Empty<Point2d>();
            
            var polygon = new RenderedPolygon(color, points);
            _frames[^1].Polygons.Add(polygon);
            GraphItemsUpdated();
        }

        /// <summary>
        /// Creates a new frame to add items into
        /// </summary>
        public void StartNextFrame()
        {
            _frames.Add(new Frame());
        }
        
        /// <summary>
        /// Provides the list of items that should be rendered.  This will reset `ItemsChangedSinceLastRender`
        /// </summary>
        internal ItemsToRender GetItemsToRender()
        {
            ItemsChangedSinceLastRender = false;
            var frame = _frames[_currentRenderedFrameIndex];
            
            return new ItemsToRender(frame.Points, 
                frame.Segments, 
                frame.Functions, 
                frame.Arrows, 
                frame.Polygons);
        }

        /// <summary>
        /// Updates the graphed items based on how long it's been since the last frame.  Used mainly to control
        /// multiple frames
        /// </summary>
        internal void Update(double secondsSinceLastFrame)
        {
            _secondsSinceCreation += secondsSinceLastFrame;

            var totalCycleTime = _frames.Count * SecondsPerFrame;
            var frameToRender = (int) (_secondsSinceCreation / totalCycleTime * _frames.Count) % _frames.Count;

            if (frameToRender != _currentRenderedFrameIndex)
            {
                _currentRenderedFrameIndex = frameToRender;
                ItemsChangedSinceLastRender = true;
            }
        }

        private void GraphItemsUpdated()
        {
            ItemsChangedSinceLastRender = true;

            _minCoordinates = null;
            _maxCoordinates = null;
        }

        private void RecalculateMinAndMaxCoordinates()
        {
            var allCoordinates = _frames.SelectMany(x => x.Points).Select(x => x.Point)
                .Union(_frames.SelectMany(x => x.Segments).Select(x => x.Start))
                .Union(_frames.SelectMany(x => x.Segments).Select(x => x.End))
                .Union(_frames.SelectMany(x => x.Arrows).Select(x => x.Start))
                .Union(_frames.SelectMany(x => x.Arrows).Select(x => x.End))
                .Union(_frames.SelectMany(x => x.Polygons).SelectMany(x => x.Points))
                .Distinct()
                .ToArray();

            if (allCoordinates.Any())
            {
                float minX = float.MaxValue,
                    minY = float.MaxValue,
                    maxX = float.MinValue,
                    maxY = float.MinValue;

                foreach (var coordinate in allCoordinates)
                {
                    if (minX > coordinate.X) minX = coordinate.X;
                    if (minY > coordinate.Y) minY = coordinate.Y;
                    if (maxX < coordinate.X) maxX = coordinate.X;
                    if (maxY < coordinate.Y) maxY = coordinate.Y;
                }
                
                _minCoordinates = new Point2d(minX, minY);
                _maxCoordinates = new Point2d(maxX, maxY);
            }
            else
            {
                _minCoordinates = null;
                _maxCoordinates = null;
            }
        }

        private class Frame
        {
            public List<RenderedPoint> Points { get; set; } = new List<RenderedPoint>();
            public List<RenderedSegment> Segments { get; set; } = new List<RenderedSegment>();
            public List<RenderedFunction> Functions { get; set; } = new List<RenderedFunction>();
            public List<RenderedArrow> Arrows { get; set; } = new List<RenderedArrow>();
            public List<RenderedPolygon> Polygons { get; set; } = new List<RenderedPolygon>();
        }
    }
}