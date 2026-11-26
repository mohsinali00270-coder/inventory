using Microsoft.Maui.Layouts;

namespace ShopLedger.Controls;

/// <summary>Responsive card grid: fits as many equal-width columns as the width allows
/// (each at least <see cref="MinColumnWidth"/>, at most <see cref="MaxColumns"/>). One column on phones,
/// 2-4 on tablets and desktop. Like the built-in MAUI layouts it arranges left-to-right; for Urdu the platform
/// mirrors the whole page (FlowDirection = RightToLeft), so the first item ends up on the right.</summary>
public class AdaptiveGrid : Layout
{
    public static readonly BindableProperty MinColumnWidthProperty =
        BindableProperty.Create(nameof(MinColumnWidth), typeof(double), typeof(AdaptiveGrid), 160d, propertyChanged: Invalidate);
    public static readonly BindableProperty MaxColumnsProperty =
        BindableProperty.Create(nameof(MaxColumns), typeof(int), typeof(AdaptiveGrid), 4, propertyChanged: Invalidate);
    public static readonly BindableProperty ColumnSpacingProperty =
        BindableProperty.Create(nameof(ColumnSpacing), typeof(double), typeof(AdaptiveGrid), 12d, propertyChanged: Invalidate);
    public static readonly BindableProperty RowSpacingProperty =
        BindableProperty.Create(nameof(RowSpacing), typeof(double), typeof(AdaptiveGrid), 12d, propertyChanged: Invalidate);

    public double MinColumnWidth { get => (double)GetValue(MinColumnWidthProperty); set => SetValue(MinColumnWidthProperty, value); }
    public int MaxColumns { get => (int)GetValue(MaxColumnsProperty); set => SetValue(MaxColumnsProperty, value); }
    public double ColumnSpacing { get => (double)GetValue(ColumnSpacingProperty); set => SetValue(ColumnSpacingProperty, value); }
    public double RowSpacing { get => (double)GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    private static void Invalidate(BindableObject b, object o, object n) => ((AdaptiveGrid)b).InvalidateMeasure();

    protected override ILayoutManager CreateLayoutManager() => new Manager(this);

    internal static int ColumnsFor(double width, double min, double spacing, int max)
    {
        if (double.IsInfinity(width) || width <= 0) return 1;
        var cols = (int)Math.Floor((width + spacing) / (min + spacing));
        return Math.Clamp(cols, 1, Math.Max(1, max));
    }

    private sealed class Manager : LayoutManager
    {
        private readonly AdaptiveGrid _g;
        public Manager(AdaptiveGrid g) : base(g) => _g = g;

        private List<IView> Visible() => _g.Cast<IView>().Where(v => v.Visibility != Visibility.Collapsed).ToList();

        /// <summary>Measures every child at the column width; returns the row heights.</summary>
        private (int Cols, double ColWidth, List<double> Rows) Compute(double width)
        {
            var items = Visible();
            var pad = _g.Padding;
            var inner = double.IsInfinity(width) ? _g.MinColumnWidth : Math.Max(0, width - pad.HorizontalThickness);
            var cols = ColumnsFor(inner, _g.MinColumnWidth, _g.ColumnSpacing, _g.MaxColumns);
            var colWidth = Math.Max(0, (inner - (cols - 1) * _g.ColumnSpacing) / cols);
            var rows = new List<double>();
            for (var i = 0; i < items.Count; i++)
            {
                var size = items[i].Measure(colWidth, double.PositiveInfinity);
                var r = i / cols;
                if (r == rows.Count) rows.Add(0);
                rows[r] = Math.Max(rows[r], size.Height);
            }
            return (cols, colWidth, rows);
        }

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var (cols, colWidth, rows) = Compute(widthConstraint);
            var pad = _g.Padding;
            var h = rows.Sum() + Math.Max(0, rows.Count - 1) * _g.RowSpacing + pad.VerticalThickness;
            var w = double.IsInfinity(widthConstraint) ? cols * colWidth + (cols - 1) * _g.ColumnSpacing + pad.HorizontalThickness : widthConstraint;
            return new Size(w, h);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var (cols, colWidth, rows) = Compute(bounds.Width);
            var items = Visible();
            var pad = _g.Padding;
            double y = bounds.Top + pad.Top;
            for (var i = 0; i < items.Count; i++)
            {
                int r = i / cols, c = i % cols;
                if (c == 0 && r > 0) y += rows[r - 1] + _g.RowSpacing;
                var x = bounds.Left + pad.Left + c * (colWidth + _g.ColumnSpacing);
                items[i].Arrange(new Rect(x, y, colWidth, rows[r]));
            }
            return bounds.Size;
        }
    }
}

/// <summary>Attached property for a CollectionView with a GridItemsLayout: sets Span from the available width.
/// XAML: c:Responsive.MinItemWidth="340"</summary>
public static class Responsive
{
    public static readonly BindableProperty MinItemWidthProperty =
        BindableProperty.CreateAttached("MinItemWidth", typeof(double), typeof(Responsive), 0d, propertyChanged: OnChanged);
    public static readonly BindableProperty MaxSpanProperty =
        BindableProperty.CreateAttached("MaxSpan", typeof(int), typeof(Responsive), 3);

    public static double GetMinItemWidth(BindableObject b) => (double)b.GetValue(MinItemWidthProperty);
    public static void SetMinItemWidth(BindableObject b, double v) => b.SetValue(MinItemWidthProperty, v);
    public static int GetMaxSpan(BindableObject b) => (int)b.GetValue(MaxSpanProperty);
    public static void SetMaxSpan(BindableObject b, int v) => b.SetValue(MaxSpanProperty, v);

    private static void OnChanged(BindableObject b, object o, object n)
    {
        if (b is not CollectionView cv) return;
        cv.SizeChanged -= OnSizeChanged;
        if ((double)n > 0) cv.SizeChanged += OnSizeChanged;
    }

    private static void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not CollectionView { ItemsLayout: GridItemsLayout grid } cv || cv.Width <= 0) return;
        var span = AdaptiveGrid.ColumnsFor(cv.Width, GetMinItemWidth(cv), grid.HorizontalItemSpacing, GetMaxSpan(cv));
        if (grid.Span != span) grid.Span = span;
    }
}
