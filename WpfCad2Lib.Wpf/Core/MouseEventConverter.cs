using WpfCad2Lib.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WpfCad2Lib.Wpf;

static class MouseEventConverterExtension
{
    extension(MouseEventArgs e)
    {
        // sender (イベントを登録した要素 = View の Canvas) を基準にしたマウス位置を返す。
        // Canvas には RenderTransform (ズーム・パン) が掛かっているので、得られるのは変換前のモデル座標になる
        public Point ToPosition(object sender) => e.GetPosition((IInputElement)sender);
    }
}

// WPF のマウスイベント (左ボタンの Down / Move / Up) を、クリックとドラッグに変換して通知する。
//
// 状態遷移:
//   待機     (isStarted = false, isDragging = false)
//     ─ ボタンを押す ─→ 押下中
//   押下中   (isStarted = true , isDragging = false)
//     ─ 押した位置から dragStartLength 以上動く ─→ DragStart + たまっていた Dragging を通知してドラッグ中へ
//     ─ その前にボタンを離す ─→ Click を通知して待機へ
//   ドラッグ中 (isStarted = true , isDragging = true)
//     ─ 動かす ─→ Dragging を通知
//     ─ ボタンを離す / キャプチャーを失う ─→ DragEnd を通知して待機へ
//
// 押下中の小さな移動は「手ぶれ」かもしれないので、すぐには通知せず draggingPositions にためておく。
// ドラッグと確定した時点でまとめて Dragging として流すので、ドラッグの軌跡 (自由曲線など) は欠けない。
class MouseEventConverter
{
    public event Action<Point>? Click;
    public event Action<Point>? DragStart;
    public event Action<Point>? Dragging;
    public event Action<Point>? DragEnd;

    // ボタンを押した位置からこの距離 (Canvas 上の座標) 以上動いたらドラッグとみなす
    const double          dragStartLength   = 10.0;

    bool                  isStarted         = false;
    bool                  isDragging        = false;
    Point                 startPosition;
    // キャプチャーを失ったときに DragEnd へ渡すための、最後に受け取った位置
    Point                 lastPosition;
    // 押下中 (ドラッグ確定前) に受け取った移動位置
    readonly Queue<Point> draggingPositions = new();

    public void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
    {
        OnMouseLeftButtonDown(e.ToPosition(sender));
        // 要素の外でボタンを離しても MouseLeftButtonUp を受け取れるようにする
        ((UIElement)sender).CaptureMouse();
    }

    public void OnMouseMove(object sender, MouseEventArgs e)
    {
        // ボタンを押していない移動 (ホバー) は無視する
        if (e.LeftButton == MouseButtonState.Pressed)
            OnMouseMove(e.ToPosition(sender));
    }

    public void OnMouseLeftButtonUp(object sender, MouseEventArgs e)
    {
        OnMouseLeftButtonUp(e.ToPosition(sender));
        // 先に状態を待機に戻してから解放する。解放で発生する LostMouseCapture では何も起きない
        ((UIElement)sender).ReleaseMouseCapture();
    }

    public void OnLostMouseCapture(object sender, MouseEventArgs e) => OnLostMouseCapture();

    public void OnMouseLeftButtonDown(Point position)
    {
        isStarted     = true ;
        isDragging    = false;
        startPosition =
        lastPosition  = position;
    }

    bool CanDragStart(Point position) => (startPosition - position).Length >= dragStartLength;

    public void OnMouseMove(Point position)
    {
        lastPosition = position;
        if (isDragging) {
            RaiseDragging(position);
        } else if (isStarted) {
            // まだクリックかドラッグか決まっていない。位置をためておき、十分離れたらドラッグに確定する
            draggingPositions.Enqueue(position);
            if (CanDragStart(position)) {
                RaiseDraggingWithDraggingPositions();
                isDragging = true;
            }
        }
    }

    // ドラッグ中ならドラッグ終了、押下中 (ほとんど動いていない) ならクリックとして通知する
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

    void RaiseClick(Point position) => Click?.Invoke(position);

    void RaiseDragStart(Point position) => DragStart?.Invoke(position);

    void RaiseDragging(Point position) => Dragging?.Invoke(position);

    // ドラッグ確定時の通知。DragStart は「動き始めた位置」ではなく「ボタンを押した位置」で通知し、
    // その後ためていた位置を順に Dragging として流す
    void RaiseDraggingWithDraggingPositions()
    {
        RaiseDragStart(startPosition);
        draggingPositions.ForEach(draggingPosition => RaiseDragging(draggingPosition));
        draggingPositions.Clear();
    }

    void RaiseDragEnd(Point position) => DragEnd?.Invoke(position);
}
