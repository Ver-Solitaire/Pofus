using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Pofus.Hud.Switcher;

/// <summary>
/// Builds small original glyphs (24×24) loosely themed after each class's
/// archetype using generic, genre-wide fantasy-RPG iconography (sword =
/// warrior, shield = protector, bow = archer...) — hand-composed from simple
/// filled primitives, not copied from Dofus's own artwork (Ankama's
/// copyrighted assets). Returns null for any class name it doesn't
/// recognise, so the caller can fall back to the text-abbreviation badge.
/// </summary>
internal static class ClassIconFactory
{
    private static readonly Brush Fill = Brushes.White;
    private const double Thickness = 1.8;

    public static UIElement? TryCreate(string className)
    {
        var canvas = new Canvas { Width = 24, Height = 24 };

        switch (Normalize(className))
        {
            case "iop":
                // Broadsword: tapered blade, crossguard, pommel.
                FilledPolygon(canvas, (12, 2), (14.2, 4.5), (13.2, 15), (10.8, 15), (9.8, 4.5));
                FilledRect(canvas, 7, 15, 10, 2, 1);
                Circle(canvas, 12, 19.5, 1.7, filled: true);
                break;
            case "cra":
                // Bow (arc, unfilled) with a filled arrowhead.
                Arc(canvas, (8, 3), (8, 21), 10, sweepLeft: true);
                Line(canvas, 8, 3, 8, 21, thickness: 1.3);
                Line(canvas, 6, 12, 18, 12);
                FilledPolygon(canvas, (14, 8), (19, 12), (14, 16));
                break;
            case "eniripsa":
                // Rounded medical cross.
                FilledRoundedRect(canvas, 9.5, 3.5, 5, 17, 2);
                FilledRoundedRect(canvas, 3.5, 9.5, 17, 5, 2);
                break;
            case "sram":
                // Curved dagger blade with crossguard.
                FilledPolygon(canvas, (5, 20), (16, 6.5), (18, 8), (7.5, 21.5));
                FilledRect(canvas, 12.5, 8, 6, 1.8, 1, angleDegrees: -50);
                break;
            case "xelor":
                // Clock face with hands.
                Circle(canvas, 12, 12, 8);
                Line(canvas, 12, 12, 12, 6.5, thickness: 2);
                Line(canvas, 12, 12, 16.5, 14.5, thickness: 2);
                Circle(canvas, 12, 12, 1.3, filled: true);
                break;
            case "ecaflip":
                // Six-sided die with pips.
                FilledRoundedRect(canvas, 4, 4, 16, 16, 3);
                Dot(canvas, 8, 8, dark: true);
                Dot(canvas, 16, 8, dark: true);
                Dot(canvas, 8, 16, dark: true);
                Dot(canvas, 16, 16, dark: true);
                Dot(canvas, 12, 12, dark: true);
                break;
            case "sadida":
                // Leaf with a center vein.
                FilledPolygon(canvas, (12, 2.5), (19, 12), (12, 21.5), (5, 12));
                Line(canvas, 12, 5, 12, 19, thickness: 1.3);
                break;
            case "osamodas":
                // Paw print.
                Ellipse(canvas, 12, 15.5, 5.5, 4.5, filled: true);
                Circle(canvas, 6.7, 8, 1.9, filled: true);
                Circle(canvas, 11.3, 5.3, 1.9, filled: true);
                Circle(canvas, 16.3, 8, 1.9, filled: true);
                break;
            case "enutrof":
                // Coin.
                Circle(canvas, 12, 12, 8, filled: true);
                Circle(canvas, 12, 12, 8);
                Dot(canvas, 12, 12, dark: true, radius: 3.2);
                break;
            case "sacrieur":
                // Blood drop.
                FilledPolygon(canvas, (12, 2.5), (18.5, 14), (12, 21.5), (5.5, 14));
                break;
            case "pandawa":
                // Mug with handle.
                FilledRoundedRect(canvas, 6, 7, 11, 12, 2);
                Arc(canvas, (17, 9), (17, 15), 4, sweepLeft: false);
                Line(canvas, 17, 9, 17, 15, thickness: 1.3);
                break;
            case "roublard":
                // Round bomb with a lit fuse.
                Circle(canvas, 11, 14, 6.5, filled: true);
                Line(canvas, 14.5, 8.5, 18, 5, thickness: 2);
                FilledPolygon(canvas, (18, 3.3), (20, 5), (18.3, 7));
                break;
            case "zobal":
            case "masqueraider":
                // Theatrical mask with eye cut-outs.
                FilledPolygon(canvas, (4, 10), (12, 4.5), (20, 10), (20, 15.5), (12, 19.5), (4, 15.5));
                Dot(canvas, 9, 11, dark: true);
                Dot(canvas, 15, 11, dark: true);
                break;
            case "steamer":
                // Gear.
                Circle(canvas, 12, 12, 5, filled: true);
                Circle(canvas, 12, 12, 2, dark: true);
                Line(canvas, 12, 2.5, 12, 6, thickness: 2.4);
                Line(canvas, 12, 18, 12, 21.5, thickness: 2.4);
                Line(canvas, 2.5, 12, 6, 12, thickness: 2.4);
                Line(canvas, 18, 12, 21.5, 12, thickness: 2.4);
                Line(canvas, 5.2, 5.2, 7.7, 7.7, thickness: 2.2);
                Line(canvas, 16.3, 16.3, 18.8, 18.8, thickness: 2.2);
                Line(canvas, 5.2, 18.8, 7.7, 16.3, thickness: 2.2);
                Line(canvas, 16.3, 7.7, 18.8, 5.2, thickness: 2.2);
                break;
            case "eliotrope":
                // Portal rings.
                Circle(canvas, 12, 12, 9, filled: true);
                Circle(canvas, 12, 12, 5.5, dark: true);
                Circle(canvas, 12, 12, 2, filled: true);
                break;
            case "huppermage":
                // Six-point sparkle/star.
                FilledPolygon(
                    canvas,
                    (12, 1.5), (14.4, 9.6), (22.5, 12), (14.4, 14.4),
                    (12, 22.5), (9.6, 14.4), (1.5, 12), (9.6, 9.6));
                break;
            case "ouginak":
                // Fangs.
                FilledPolygon(canvas, (7, 3.5), (10.5, 3.5), (8.75, 12));
                FilledPolygon(canvas, (13.5, 3.5), (17, 3.5), (15.25, 12));
                Line(canvas, 6, 16, 18, 16, thickness: 2);
                break;
            case "forgelance":
                // Lance with a triangular head.
                Line(canvas, 6, 20, 17, 6.5, thickness: 2.2);
                FilledPolygon(canvas, (13.5, 10.3), (17.5, 3.5), (21, 7.5));
                break;
            case "feca":
                // Shield.
                FilledPolygon(canvas, (12, 2.5), (19.5, 5.5), (19.5, 12), (12, 21.5), (4.5, 12), (4.5, 5.5));
                break;
            default:
                return null;
        }

        return canvas;
    }

    private static string Normalize(string className) => className.Trim().ToLowerInvariant();

    private static void Line(Canvas canvas, double x1, double y1, double x2, double y2, double thickness = Thickness)
    {
        canvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Fill,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        });
    }

    private static void Arc(Canvas canvas, (double X, double Y) start, (double X, double Y) end, double radius, bool sweepLeft)
    {
        var figure = new PathFigure { StartPoint = new Point(start.X, start.Y) };
        figure.Segments.Add(new ArcSegment(
            new Point(end.X, end.Y), new Size(radius, radius), 0,
            isLargeArc: false,
            sweepDirection: sweepLeft ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
            isStroked: true));
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        canvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = Fill,
            StrokeThickness = Thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        });
    }

    private static void FilledPolygon(Canvas canvas, params (double X, double Y)[] points)
    {
        var polygon = new Polygon { Fill = Fill };
        foreach (var (x, y) in points)
        {
            polygon.Points.Add(new Point(x, y));
        }

        canvas.Children.Add(polygon);
    }

    private static void FilledRect(Canvas canvas, double left, double top, double width, double height, double radius, double angleDegrees = 0)
    {
        var rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Fill,
            RadiusX = radius,
            RadiusY = radius,
        };
        if (angleDegrees != 0)
        {
            rectangle.RenderTransform = new RotateTransform(angleDegrees, width / 2, height / 2);
        }

        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
        canvas.Children.Add(rectangle);
    }

    private static void FilledRoundedRect(Canvas canvas, double left, double top, double width, double height, double radius)
    {
        var rectangle = new Rectangle { Width = width, Height = height, Fill = Fill, RadiusX = radius, RadiusY = radius };
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
        canvas.Children.Add(rectangle);
    }

    private static void Circle(Canvas canvas, double centerX, double centerY, double radius, bool filled = false, bool dark = false)
    {
        var ellipse = new Ellipse { Width = radius * 2, Height = radius * 2 };
        if (filled)
        {
            ellipse.Fill = dark ? DarkOverlay : Fill;
        }
        else
        {
            ellipse.Stroke = Fill;
            ellipse.StrokeThickness = Thickness;
        }

        Canvas.SetLeft(ellipse, centerX - radius);
        Canvas.SetTop(ellipse, centerY - radius);
        canvas.Children.Add(ellipse);
    }

    private static void Ellipse(Canvas canvas, double centerX, double centerY, double radiusX, double radiusY, bool filled)
    {
        var ellipse = new System.Windows.Shapes.Ellipse { Width = radiusX * 2, Height = radiusY * 2 };
        if (filled)
        {
            ellipse.Fill = Fill;
        }
        else
        {
            ellipse.Stroke = Fill;
            ellipse.StrokeThickness = Thickness;
        }

        Canvas.SetLeft(ellipse, centerX - radiusX);
        Canvas.SetTop(ellipse, centerY - radiusY);
        canvas.Children.Add(ellipse);
    }

    private static readonly Brush DarkOverlay = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));

    private static void Dot(Canvas canvas, double centerX, double centerY, bool dark = false, double radius = 1.3)
    {
        var ellipse = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = dark ? DarkOverlay : Fill };
        Canvas.SetLeft(ellipse, centerX - radius);
        Canvas.SetTop(ellipse, centerY - radius);
        canvas.Children.Add(ellipse);
    }
}
