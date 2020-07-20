using System;
using Microsoft.Xna.Framework;

namespace SharpPlotter.Rendering
{
    public readonly struct RenderedFunction
    {
        public readonly Func<float, float> Function;
        public readonly Color Color;

        public RenderedFunction(Color color, Func<float, float> function)
        {
            Color = color;
            Function = function;
        }
    }
}