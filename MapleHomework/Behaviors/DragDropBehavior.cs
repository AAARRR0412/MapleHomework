using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;

namespace MapleHomework.Behaviors
{
    /// <summary>
    /// 드래그 앤 드롭 Attached Behavior
    /// </summary>
    public static class DragDropBehavior
    {
        #region IsDragSource

        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.RegisterAttached(
                "IsDragSource",
                typeof(bool),
                typeof(DragDropBehavior),
                new PropertyMetadata(false, OnIsDragSourceChanged));

        public static bool GetIsDragSource(DependencyObject obj)
            => (bool)obj.GetValue(IsDragSourceProperty);

        public static void SetIsDragSource(DependencyObject obj, bool value)
            => obj.SetValue(IsDragSourceProperty, value);

        private static void OnIsDragSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                    element.PreviewMouseMove += OnPreviewMouseMove;
                    element.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
                }
                else
                {
                    element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                    element.PreviewMouseMove -= OnPreviewMouseMove;
                    element.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
                }
            }
        }

        #endregion

        #region IsDropTarget

        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsDropTarget",
                typeof(bool),
                typeof(DragDropBehavior),
                new PropertyMetadata(false, OnIsDropTargetChanged));

        public static bool GetIsDropTarget(DependencyObject obj)
            => (bool)obj.GetValue(IsDropTargetProperty);

        public static void SetIsDropTarget(DependencyObject obj, bool value)
            => obj.SetValue(IsDropTargetProperty, value);

        private static void OnIsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.AllowDrop = true;
                    element.DragEnter += OnDragEnter;
                    element.DragLeave += OnDragLeave;
                    element.DragOver += OnDragOver;
                    element.Drop += OnDrop;
                }
                else
                {
                    element.AllowDrop = false;
                    element.DragEnter -= OnDragEnter;
                    element.DragLeave -= OnDragLeave;
                    element.DragOver -= OnDragOver;
                    element.Drop -= OnDrop;
                }
            }
        }

        #endregion

        #region DropCommand

        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(DragDropBehavior),
                new PropertyMetadata(null));

        public static ICommand GetDropCommand(DependencyObject obj)
            => (ICommand)obj.GetValue(DropCommandProperty);

        public static void SetDropCommand(DependencyObject obj, ICommand value)
            => obj.SetValue(DropCommandProperty, value);

        #endregion

        #region DragDataType

        public static readonly DependencyProperty DragDataTypeProperty =
            DependencyProperty.RegisterAttached(
                "DragDataType",
                typeof(string),
                typeof(DragDropBehavior),
                new PropertyMetadata("Item"));

        public static string GetDragDataType(DependencyObject obj)
            => (string)obj.GetValue(DragDataTypeProperty);

        public static void SetDragDataType(DependencyObject obj, string value)
            => obj.SetValue(DragDataTypeProperty, value);

        #endregion

        private static Point _startPoint;
        private static bool _isDragging;

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _isDragging = false;
        }

        private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
                return;

            var element = sender as FrameworkElement;
            if (element == null) return;

            Point position = e.GetPosition(null);
            Vector diff = _startPoint - position;

            // 최소 드래그 거리 확인
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = true;

                // 드래그 데이터 생성
                var data = element.DataContext;
                if (data != null)
                {
                    var dataType = GetDragDataType(element);
                    var dragData = new DataObject(dataType, data);
                    DragDrop.DoDragDrop(element, dragData, DragDropEffects.Move);
                }

                _isDragging = false;
            }
        }

        private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private static void OnDragEnter(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                // 드롭 가능 표시
                var dataType = GetDragDataType(element);
                if (e.Data.GetDataPresent(dataType))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private static void OnDragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var dataType = GetDragDataType(element);
                if (e.Data.GetDataPresent(dataType))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var dataType = GetDragDataType(element);
                if (e.Data.GetDataPresent(dataType))
                {
                    var data = e.Data.GetData(dataType);
                    var target = element.DataContext;
                    var command = GetDropCommand(element);

                    if (command != null && command.CanExecute(new DropEventArgs(data, target)))
                    {
                        command.Execute(new DropEventArgs(data, target));
                    }
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// 드롭 이벤트 인자
    /// </summary>
    public class DropEventArgs
    {
        public object? DraggedItem { get; }
        public object? TargetItem { get; }

        public DropEventArgs(object? draggedItem, object? targetItem)
        {
            DraggedItem = draggedItem;
            TargetItem = targetItem;
        }
    }
}
