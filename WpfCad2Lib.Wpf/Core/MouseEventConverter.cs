using WpfCad2Lib.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WpfCad2Lib.Wpf
{
    static class MouseEventConverterExtension
    {
        public static Point ToPosition(this MouseEventArgs e, object sender)
        {
            return e.GetPosition((IInputElement)sender);
        }
    }

    class MouseEventConverter
    {
        public event Action<Point>? Click;
        public event Action<Point>? DragStart;
        public event Action<Point>? Dragging;
        public event Action<Point>? DragEnd;

        const double          dragStartLength   = 10.0;

        bool                  isStarted         = false;
        bool                  isDragging        = false;
        Point                 startPosition;
        Point                 lastPosition;
        readonly Queue<Point> draggingPositions = new Queue<Point>();

        public void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            OnMouseLeftButtonDown(e.ToPosition(sender));
            // 要素の外でボタンを離しても MouseLeftButtonUp を受け取れるようにする
            ((UIElement)sender).CaptureMouse();
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                OnMouseMove(e.ToPosition(sender));
        }

        public void OnMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            OnMouseLeftButtonUp(e.ToPosition(sender));
            ((UIElement)sender).ReleaseMouseCapture();
        }

        public void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            OnLostMouseCapture();
        }

        public void OnMouseLeftButtonDown(Point position)
        {
            isStarted     = true ;
            isDragging    = false;
            startPosition =
            lastPosition  = position;
        }

        bool CanDragStart(Point position)
        {
            return (startPosition - position).Length >= dragStartLength;
        }

        public void OnMouseMove(Point position)
        {
            lastPosition = position;
            if (isDragging) {
                RaiseDragging(position);
            } else if (isStarted) {
                draggingPositions.Enqueue(position);
                if (CanDragStart(position)) {
                    RaiseDraggingWithDraggingPositions();
                    isDragging = true;
                }
            }
        }

        public void OnMouseLeftButtonUp(Point position)
        {
            if (isDragging) {
                RaiseDragEnd(position);
                isDragging = isStarted = false;
            } else if (isStarted) {
                RaiseClick(position);
                isStarted = false;
                draggingPositions.Clear();
            }
        }

        // ボタンを離す前にキャプチャーを失った (Alt+Tab など) 場合は、最後の位置でドラッグを終える
        public void OnLostMouseCapture()
        {
            if (isDragging)
                RaiseDragEnd(lastPosition);
            isDragging = isStarted = false;
            draggingPositions.Clear();
        }

        void RaiseClick(Point position)
        {
            Click?.Invoke(position);
        }

        void RaiseDragStart(Point position)
        {
            DragStart?.Invoke(position);
        }

        void RaiseDragging(Point position)
        {
            Dragging?.Invoke(position);
        }

        void RaiseDraggingWithDraggingPositions()
        {
            RaiseDragStart(startPosition);
            draggingPositions.ForEach(draggingPosition => RaiseDragging(draggingPosition));
            draggingPositions.Clear();
        }

        void RaiseDragEnd(Point position)
        {
            DragEnd?.Invoke(position);
        }
    }
}
