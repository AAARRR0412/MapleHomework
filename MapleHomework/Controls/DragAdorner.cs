using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace MapleHomework.Controls
{
    /// <summary>
    /// 드래그 중 시각적 복제본을 표시하는 Adorner
    /// </summary>
    public class DragAdorner : Adorner
    {
        private readonly WpfRectangle _child;
        private readonly TranslateTransform _transform;
        private double _offsetX;
        private double _offsetY;

        public DragAdorner(UIElement adornedElement, double width, double height, System.Windows.Media.Brush visualBrush) : base(adornedElement)
        {
            _transform = new TranslateTransform();

            _child = new WpfRectangle
            {
                Width = width,
                Height = height,
                Fill = visualBrush,
                Opacity = 0.9,
                RadiusX = 12,
                RadiusY = 12,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 8,
                    Opacity = 0.5,
                    Color = Colors.Black
                },
                RenderTransform = _transform
            };

            IsHitTestVisible = false;
        }

        public void SetOffsets(double x, double y)
        {
            _offsetX = x;
            _offsetY = y;
        }

        public void UpdatePosition(double left, double top)
        {
            // RenderTransform 직접 업데이트 (즉각적인 반응)
            _transform.X = left - _offsetX;
            _transform.Y = top - _offsetY;
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _child;

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            _child.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return _child.DesiredSize;
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            _child.Arrange(new Rect(0, 0, _child.DesiredSize.Width, _child.DesiredSize.Height));
            return finalSize;
        }
    }
}
