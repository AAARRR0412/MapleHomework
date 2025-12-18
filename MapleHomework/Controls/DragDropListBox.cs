// 드래그 앤 드롭 시 카드들이 자리를 비키며 이동하는 애니메이션을 지원하는 ListBox
// 모든 타입에 명시적 네임스페이스를 사용하여 System.Windows.Forms와의 충돌 방지

namespace MapleHomework.Controls
{
    public class DragDropListBox : System.Windows.Controls.ListBox
    {
        private System.Windows.Point _dragStartPoint;
        private int _dragStartIndex = -1;
        private int _currentDropIndex = -1;
        private DragAdorner? _dragAdorner;
        private System.Windows.Documents.AdornerLayer? _adornerLayer;
        private System.Windows.FrameworkElement? _draggedElement;
        private bool _isDragging;
        private System.Windows.UIElement? _windowContent;
        private const double CardHeight = 100; // 카드 높이 + Margin

        public static readonly System.Windows.RoutedEvent ItemsReorderedEvent =
            System.Windows.EventManager.RegisterRoutedEvent(
                "ItemsReordered",
                System.Windows.RoutingStrategy.Bubble,
                typeof(System.Windows.RoutedEventHandler),
                typeof(DragDropListBox));

        public event System.Windows.RoutedEventHandler ItemsReordered
        {
            add => AddHandler(ItemsReorderedEvent, value);
            remove => RemoveHandler(ItemsReorderedEvent, value);
        }

        public DragDropListBox()
        {
            AllowDrop = true;
            SelectionMode = System.Windows.Controls.SelectionMode.Single;

            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewMouseMove += OnPreviewMouseMove;
            PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            DragOver += OnDragOver;
            Drop += OnDrop;
            DragLeave += OnDragLeave;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            // 클릭한 ListBoxItem 찾기
            var element = e.OriginalSource as System.Windows.DependencyObject;
            while (element != null && !(element is System.Windows.Controls.ListBoxItem))
            {
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }

            if (element is System.Windows.Controls.ListBoxItem item)
            {
                _dragStartIndex = ItemContainerGenerator.IndexFromContainer(item);
                _draggedElement = item;
            }
        }

        private void OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed || _dragStartIndex < 0 || _isDragging) return;

            var currentPos = e.GetPosition(null);
            var diff = _dragStartPoint - currentPos;

            if (System.Math.Abs(diff.X) > 5 || System.Math.Abs(diff.Y) > 5)
            {
                _isDragging = true;

                // Adorner 생성
                CreateDragAdorner();

                // QueryContinueDrag 이벤트로 드래그 중 마우스 위치 추적
                QueryContinueDrag += OnQueryContinueDrag;

                var dragData = new System.Windows.DataObject("DragDropListBoxItem", Items[_dragStartIndex]);
                System.Windows.DragDrop.DoDragDrop(this, dragData, System.Windows.DragDropEffects.Move);

                // 드래그 종료 후 정리
                QueryContinueDrag -= OnQueryContinueDrag;
                RemoveDragAdorner();
                ResetAllTransforms();
                _isDragging = false;
                _dragStartIndex = -1;
                _currentDropIndex = -1;
                _windowContent = null;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        private void OnQueryContinueDrag(object sender, System.Windows.QueryContinueDragEventArgs e)
        {
            if (_dragAdorner == null || _windowContent == null) return;

            // Win32 API로 스크린 좌표 가져오기
            if (GetCursorPos(out POINT screenPoint))
            {
                // 스크린 좌표를 _windowContent 좌표로 변환 (Adorner가 여기에 연결됨)
                var visual = _windowContent as System.Windows.Media.Visual;
                if (visual != null)
                {
                    var contentPoint = visual.PointFromScreen(new System.Windows.Point(screenPoint.X, screenPoint.Y));
                    _dragAdorner.UpdatePosition(contentPoint.X, contentPoint.Y);
                }
            }
        }

        private void OnPreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isDragging && _dragStartIndex >= 0)
            {
                // 드래그가 시작되지 않았다면 선택
                SelectedIndex = _dragStartIndex;
            }
            _dragStartIndex = -1;
        }

        private void CreateDragAdorner()
        {
            if (_draggedElement == null) return;

            // Window 레벨 AdornerLayer 사용
            var window = System.Windows.Window.GetWindow(this);
            if (window == null) return;

            _windowContent = window.Content as System.Windows.UIElement;
            if (_windowContent == null) return;

            _adornerLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(_windowContent);
            if (_adornerLayer == null)
            {
                _adornerLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(this);
            }
            if (_adornerLayer == null) return;

            // DrawingVisual을 사용하여 전체 요소 캡처 (ScrollViewer 클리핑과 무관)
            var width = _draggedElement.ActualWidth;
            var height = _draggedElement.ActualHeight;
            if (width <= 0 || height <= 0) return;

            // DrawingVisual로 요소를 비트맵으로 렌더링
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var visualBrush = new System.Windows.Media.VisualBrush(_draggedElement)
                {
                    Stretch = System.Windows.Media.Stretch.None,
                    AlignmentX = System.Windows.Media.AlignmentX.Left,
                    AlignmentY = System.Windows.Media.AlignmentY.Top
                };
                drawingContext.DrawRectangle(visualBrush, null, new System.Windows.Rect(0, 0, width, height));
            }

            var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)width, (int)height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);

            var imageBrush = new System.Windows.Media.ImageBrush(renderBitmap)
            {
                Stretch = System.Windows.Media.Stretch.None
            };

            _dragAdorner = new DragAdorner(
                _windowContent,
                width,
                height,
                imageBrush
            );

            var mousePos = System.Windows.Input.Mouse.GetPosition(_draggedElement);
            _dragAdorner.SetOffsets(mousePos.X, mousePos.Y);
            _adornerLayer.Add(_dragAdorner);

            // 초기 위치 설정 (_windowContent 좌표계 사용)
            if (GetCursorPos(out POINT screenPoint))
            {
                var visual = _windowContent as System.Windows.Media.Visual;
                if (visual != null)
                {
                    var contentPoint = visual.PointFromScreen(new System.Windows.Point(screenPoint.X, screenPoint.Y));
                    _dragAdorner.UpdatePosition(contentPoint.X, contentPoint.Y);
                }
            }
        }

        private void RemoveDragAdorner()
        {
            if (_adornerLayer != null && _dragAdorner != null)
            {
                _adornerLayer.Remove(_dragAdorner);
            }
            _adornerLayer = null;
            _dragAdorner = null;
        }

        private void OnDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("DragDropListBoxItem"))
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = System.Windows.DragDropEffects.Move;

            // Adorner 위치 업데이트 (Window 좌표 사용)
            if (_dragAdorner != null && _windowContent != null)
            {
                var pos = e.GetPosition(_windowContent);
                _dragAdorner.UpdatePosition(pos.X, pos.Y);
            }

            // 드롭 위치 계산 및 카드 이동 애니메이션
            var dropIndex = GetDropIndex(e);
            if (dropIndex != _currentDropIndex)
            {
                _currentDropIndex = dropIndex;
                AnimateCardDisplacement(dropIndex);
            }

            e.Handled = true;
        }

        private void OnDragLeave(object sender, System.Windows.DragEventArgs e)
        {
            ResetAllTransforms();
            _currentDropIndex = -1;
        }

        private int GetDropIndex(System.Windows.DragEventArgs e)
        {
            var point = e.GetPosition(this);

            for (int i = 0; i < Items.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is System.Windows.Controls.ListBoxItem container)
                {
                    var itemPos = container.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                    var itemRect = new System.Windows.Rect(itemPos, new System.Windows.Size(container.ActualWidth, container.ActualHeight));

                    if (point.Y < itemRect.Top + itemRect.Height / 2)
                    {
                        return i;
                    }
                }
            }
            return Items.Count;
        }

        private void AnimateCardDisplacement(int targetIndex)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is System.Windows.Controls.ListBoxItem container)
                {
                    var transform = container.RenderTransform as System.Windows.Media.TranslateTransform;
                    if (transform == null)
                    {
                        transform = new System.Windows.Media.TranslateTransform();
                        container.RenderTransform = transform;
                    }

                    double targetY = 0;

                    // 드래그 중인 아이템이 아래로 이동하는 경우
                    if (_dragStartIndex < targetIndex)
                    {
                        // 드래그 시작 위치보다 뒤에 있고, 타겟 위치보다 앞에 있는 아이템들은 위로 이동
                        if (i > _dragStartIndex && i < targetIndex)
                        {
                            targetY = -CardHeight;
                        }
                    }
                    // 드래그 중인 아이템이 위로 이동하는 경우
                    else if (_dragStartIndex > targetIndex)
                    {
                        // 타겟 위치보다 뒤에 있고, 드래그 시작 위치보다 앞에 있는 아이템들은 아래로 이동
                        if (i >= targetIndex && i < _dragStartIndex)
                        {
                            targetY = CardHeight;
                        }
                    }

                    // 드래그 중인 아이템 자체는 완전히 숨김 (고스트 카드만 표시)
                    if (i == _dragStartIndex)
                    {
                        container.Opacity = 0;
                    }
                    else
                    {
                        container.Opacity = 1.0;
                    }

                    // 부드러운 애니메이션
                    var animation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = targetY,
                        Duration = System.TimeSpan.FromMilliseconds(200),
                        EasingFunction = new System.Windows.Media.Animation.CubicEase
                        {
                            EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                        }
                    };
                    transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, animation);
                }
            }
        }

        private void ResetAllTransforms()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is System.Windows.Controls.ListBoxItem container)
                {
                    container.Opacity = 1.0;

                    if (container.RenderTransform is System.Windows.Media.TranslateTransform transform)
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 0,
                            Duration = System.TimeSpan.FromMilliseconds(150),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase
                            {
                                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                            }
                        };
                        transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, animation);
                    }
                }
            }
        }

        private void OnDrop(object sender, System.Windows.DragEventArgs e)
        {
            RemoveDragAdorner();

            if (!e.Data.GetDataPresent("DragDropListBoxItem")) return;

            var dropIndex = GetDropIndex(e);

            if (dropIndex != _dragStartIndex && _dragStartIndex >= 0)
            {
                // 아이템 재정렬 이벤트 발생
                RaiseEvent(new ItemsReorderedEventArgs(
                    ItemsReorderedEvent,
                    this,
                    _dragStartIndex,
                    dropIndex > _dragStartIndex ? dropIndex - 1 : dropIndex
                ));
            }

            ResetAllTransforms();
            e.Handled = true;
        }
    }

    public class ItemsReorderedEventArgs : System.Windows.RoutedEventArgs
    {
        public int OldIndex { get; }
        public int NewIndex { get; }

        public ItemsReorderedEventArgs(System.Windows.RoutedEvent routedEvent, object source, int oldIndex, int newIndex)
            : base(routedEvent, source)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
}
