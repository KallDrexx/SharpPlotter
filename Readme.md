# SharpPlotter

SharpPlotter is an application which allows you to quickly draw values onto a graph using C#, Javascript, or Python and your favorite code editor.  Once you load up a script in SharpPlotter, every time you save changes to the code you are writing will automatically render the results of the graph to the screen.

[![Demonstration Video](https://raw.githubusercontent.com/KallDrexx/SharpPlotter/master/docs/Youtube Thumbnail.PNG)](https://www.youtube.com/watch?v=wfOljHUPfhg&feature=youtu.be)

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**

- [Settings](#settings)
- [Scripting Languages](#scripting-languages)
  - [C# Scripting](#c-scripting)
  - [Javascript](#javascript)
  - [Python](#python)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Settings

There are two primary settings that you can change from within the application, the directory that scripts are in and what code editor you wish to open automatically (if any).

For simplicity all scripts are restricted to a single directory.  By default this directory is `%MyDocuments%\SharpPlotter` on windows and `~/SharpPlotter` on unix variants.  

The code editor executable can be changed as well.  The default is set to `code` for visual studio code, but it can be changed to any executable as long as it is in your path, or removed if you don't want SharpPlotter to load the text editor for you.  When the text editor is launched for any given file it will pass in the full path to the script to load as an argument.

## Scripting Languages

As of right now 3 languages are support - C#, Javascript, and Python.  It is important that all script files use the correct language extension (`.cs`, `.js` and `.py` respectively), as that is how SharpPlotter knows which scripting engine to use for each file.  

### C# Scripting

SharpPlotter uses the Rosly for C# 8 compiling and execution.  Scripts being written do not need to apply to a lot of formalities of most C# projects, meaning that the code does not need to be enclosed in a namespace or class, and class/struct definitions, function definitions, and execution code can all exist on the same indentation levels.  

The scripting runtime comes with the XNA `Color` structure for defining colors for different operations.  This allows you use predefined colors such as `Color.Red` and `Color.CornflowerBlue`, or define your own with the `Color(byte r, byte g, byte b)` constructor.  

In order to draw on the graph the script has access to an object instance named `Graph`.  This object contains methods to draw points and line segments onto the graphs, with an optional color argument as the first parameter.

Some example drawing calls are:

* `Graph.Points((1,2))` - draws a single white point on the graph at x=1, y=2
* `Graph.Points(Color.Red, (3,2), (4,1))` - draws two red points on the graph at x=3, y=2 and x=4, y=1
* `Graph.Segments(1,1), (2,2), (3,3))` - Draws 2 white line segments, one from (1,1) to (2,2) and a second from (2,2) to (3,3)
* `Graph.Segments(Color.Green, anEnumerableOfPoints)` - Draws green line segments from each point in the enumerable to the next

As can be seen, all points specified are expected to be numeric 2 item tuples.

An example script that draws a function `f(x) = |x*x|` for all integer values between -10 and 10 is:

```
using System;
using System.Linq;

readonly struct Point
{
    public readonly int X;
    public readonly int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

void RenderGeneratePoint(Point point, int count)
{
    var colors = new[] {Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Magenta};
    var color = colors[Math.Abs(count % colors.Length - 1)];
    
    Graph.Points(color, (point.X, point.Y));
}

var points = Enumerable.Range(-10, 21)
    .Select(x => new Point(x, x * x))
    .ToArray();

for (var x = 0; x < points.Length; x++)
{
    RenderGeneratePoint(points[x], x);
}

Graph.Segments(Color.Orange, points.Select(p => (p.X, p.Y)));
```

### Javascript

SharpPlotter uses a full ECMA 5.1 compliance compiler and execution engine for its javascript support.  Due to the nature of javascript there are 3 primary ways to specify a single point on the graph.

* An object with `x` and `y` properties (e.g. `{x: 1, y: 2})`)
* An array with exactly 2 numeric values representing x and y coordinates (e.g. `[1, 2]`)
* Calling the built-in `p()` function with 2 numeric values (e.g. `p(1,2)`)

Multiple points can be combined into an array to batch them together to draw them to the graph.

Color values can be specified by calling the constructor on the `color` type, with `r`, `g`, `b` arguments passed in containing values between `0` and `255`.  For example, a cyan color can be created by calling `new color(0, 128, 128);`.  The `color` object also has properties for pre-made values, such as `Color.Red` and `Color.CornflowerBlue`.

**Warning**: One mistake is accidentally redefining the `color` global, and that can cause errors when attempting to draw colored points to the graph.

Once you have a set of points you can then use those points to draw the points themselves or line segments through the points by using the `graph` global object.  You can then call the `graph.Points()` function to draw isolated points, or `graph.Segments()` to draw line segments.  Each function takes an optional color parameter and one or more points.

An example of a script that draws the function `f(x) = x*x` for integer values -9 to 9 is:

```
function renderPoint(point) {
    var allColors = [color.Red, color.Yellow, color.Blue, color.Magenta, color.Cyan];
    var index = Math.abs(point.x + point.y) % allColors.length;
    var chosenColor = allColors[index];

    graph.Points(chosenColor, point);
}

var points = Array(9).fill().map((_, i) => ({x: i, y: i * i}));
var inversePoints = points.map(p => ({x: -p.x, y: -p.y}));

points.forEach(renderPoint);
inversePoints.forEach(renderPoint);
graph.Segments(color.Green, points);
graph.Segments(color.Red, inversePoints);
```

### Python

SharpPlotter contains a Python 2.7 compatible python interpretor.  

Individual points are specified by tuples (e.g. `(1,2)` for x=1, y=2).  

Colors are defined by calling the `Color(r, g, b)` function with integer values between 0 and 255.  A set of predefined colors exist as properties on the `Color` object, such as `Color.Green`, `Color.CornflowerBlue`, etc...

The graph can be drawn to by calling `graph.Points()` and `graph.Segments()`.  These functions optionally take a color and a set of points.

An example of a script that draws the function `f(x) = x*x` for integer values 0-8 is:

```
def renderPoints(point):
    allColors = [Color.Red, Color.Yellow, Color.Blue, Color.Magenta, Color.Cyan]
    index = abs(point[0] + point[1]) % len(allColors)
    chosenColor = allColors[index]

    graph.Points(chosenColor, point)

points = []
for x in range(8):
    points.append((x, x * x))

for point in points:
    renderPoints(point)

graph.Segments(Color.Blue, points)
```

