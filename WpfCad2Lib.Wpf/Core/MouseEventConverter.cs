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
        public event Action<Point> Click    ;
        public event Action<Point> DragStart;
        public event Action<Point> Dragging ;
        public event Action<Point> DragEnd  ;

        const double          dragStartLength   = 10.0;

        bool                  isStarted         = false;
        bool                  isDragging        = false;
        Point                 startPosition;
        readonly Queue<Point> draggingPositions = new Queue<Point>();

        public void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            OnMouseLeftButtonDown(e.ToPosition(sender));
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                OnMouseMove(e.ToPosition(sender));
        }

        public void OnMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            OnMouseLeftButtonUp(e.ToPosition(sender));
        }

        public void OnMouseLeftButtonDown(Point position)
        {
            isStarted     = true ;
            isDragging    = false;
            startPosition = position;
        }

        bool canDragStart(Point position)
        {
            return (startPosition - position).Length >= dragStartLength;
        }

        public void OnMouseMove(Point position)
        {
            if (isDragging) {
                RaiseDragging(position);
            } else if (isStarted) {
                draggingPositions.Enqueue(position);
                if (canDragStart(position)) {
                    RaizeDraggingWithDraggingPositions();
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

        void RaiseClick(Point position)
        {
            if (Click != null)
                Click(position);
        }

        void RaiseDragStart(Point position)
        {
            if (DragStart != null)
                DragStart(position);
        }

        void RaiseDragging(Point position)
        {
            if (Dragging != null)
                Dragging(position);
        }

        void RaizeDraggingWithDraggingPositions()
        {
            RaiseDragStart(startPosition);
            draggingPositions.ForEach(draggingPosition => RaiseDragging(draggingPosition));
            draggingPositions.Clear();
        }

        void RaiseDragEnd(Point position)
        {
            if (DragEnd != null)
                DragEnd(position);
        }
    }
}
