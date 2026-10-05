using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LookUp.UI;

/// <summary>
/// The notebook seal: a filled circle with its text running round the ring and a number in the middle.
/// Same drawing as the seal reader.js stamps on entry pages, so the two read as one mark.
/// </summary>
sealed class SealStamp : FrameworkElement
{
    public static readonly DependencyProperty RingTextProperty = DependencyProperty.Register(
        nameof(RingText), typeof(string), typeof(SealStamp), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CenterTextProperty = DependencyProperty.Register(
        nameof(CenterText), typeof(string), typeof(SealStamp), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(SealStamp), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty InkProperty = DependencyProperty.Register(
        nameof(Ink), typeof(Brush), typeof(SealStamp), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public string RingText { get => (string)GetValue(RingTextProperty); set => SetValue(RingTextProperty, value); }
    public string CenterText { get => (string)GetValue(CenterTextProperty); set => SetValue(CenterTextProperty, value); }
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Brush? Ink { get => (Brush?)GetValue(InkProperty); set => SetValue(InkProperty, value); }

    public SealStamp()
    {
        SetResourceReference(FillProperty, "SealBrush");
        SetResourceReference(InkProperty, "SealInkBrush");
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var scale = size / 92; // drawn on the same 92-unit grid as the web seal
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        dc.DrawEllipse(Fill, null, center, size / 2 - scale, size / 2 - scale);

        var mono = (FontFamily)FindResource("MonoFont");
        var sans = (FontFamily)FindResource("UiFont");
        var monoFace = new Typeface(mono, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var sansFace = new Typeface(sans, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        // Ring: letters spread evenly so the text closes on itself, like SVG textLength.
        var text = RingText.ToUpperInvariant();
        if (text.Length > 0)
        {
            var radius = 34 * scale;
            var glyphs = text.Select(c => new FormattedText(c.ToString(), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, monoFace, 7.6 * scale, Ink, dpi)).ToList();
            var circumference = 2 * Math.PI * radius;
            var spacing = (circumference - glyphs.Sum(g => g.WidthIncludingTrailingWhitespace)) / glyphs.Count;
            var travelled = 0.0;
            foreach (var glyph in glyphs)
            {
                var width = glyph.WidthIncludingTrailingWhitespace;
                var degrees = (travelled + width / 2) / radius * 180 / Math.PI; // clockwise from 12 o'clock
                dc.PushTransform(new RotateTransform(degrees, center.X, center.Y));
                dc.DrawText(glyph, new Point(center.X - width / 2, center.Y - radius - glyph.Height / 2));
                dc.Pop();
                travelled += width + spacing;
            }
        }

        if (CenterText.Length > 0)
        {
            var number = new FormattedText(CenterText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                sansFace, 22 * scale, Ink, dpi);
            dc.DrawText(number, new Point(center.X - number.Width / 2, center.Y - number.Height / 2));
        }
    }
}
