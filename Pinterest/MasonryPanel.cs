using System;
using System.Windows;
using System.Windows.Controls;

namespace Pinterest
{
    public class MasonryPanel : Panel
    {
        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(MasonryPanel),
                new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        protected override Size MeasureOverride(Size availableSize)
        {
            double itemWidth = ItemWidth;
            int columns = Math.Max(1, (int)(availableSize.Width / itemWidth));

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(itemWidth, double.PositiveInfinity));
            }

            double[] columnHeights = new double[columns];
            foreach (UIElement child in InternalChildren)
            {
                int targetColumn = 0;
                double minHeight = columnHeights[0];
                for (int i = 1; i < columns; i++)
                {
                    if (columnHeights[i] < minHeight)
                    {
                        minHeight = columnHeights[i];
                        targetColumn = i;
                    }
                }
                columnHeights[targetColumn] += child.DesiredSize.Height;
            }

            double maxHeight = 0;
            foreach (var h in columnHeights)
                if (h > maxHeight) maxHeight = h;

            double width = columns * itemWidth;
            double height = double.IsInfinity(availableSize.Height) ? maxHeight : Math.Min(maxHeight, availableSize.Height);

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double itemWidth = ItemWidth;
            int columns = Math.Max(1, (int)(finalSize.Width / itemWidth));

            double[] columnHeights = new double[columns];

            foreach (UIElement child in InternalChildren)
            {
                int targetColumn = 0;
                double minHeight = columnHeights[0];
                for (int i = 1; i < columns; i++)
                {
                    if (columnHeights[i] < minHeight)
                    {
                        minHeight = columnHeights[i];
                        targetColumn = i;
                    }
                }

                double x = targetColumn * itemWidth;
                double y = columnHeights[targetColumn];
                double childHeight = child.DesiredSize.Height;

                child.Arrange(new Rect(new Point(x, y), new Size(itemWidth, childHeight)));

                columnHeights[targetColumn] += childHeight;
            }

            double maxHeight = 0;
            foreach (var h in columnHeights)
                if (h > maxHeight) maxHeight = h;

            return new Size(columns * itemWidth, maxHeight);
        }
    }
}
