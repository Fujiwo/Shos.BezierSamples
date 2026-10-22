using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace BezierWpf;

static class MouseEventConverterExtension
{
    extension(MouseEventArgs e)
    {
        // マウスの位置を、イベントを受け取った要素 (sender) を基準とした座標で返す
        public Point ToPosition(object sender)
            => e.GetPosition((IInputElement)sender);
    }
}

// マウスの生のイベント (左ボタンの押下・移動・解放、キャプチャーの喪失) を、
// 「クリック」と「ドラッグ (開始・途中・終了)」のイベントに変換する。
//
// 状態は isStarted / isDragging の 2 つのフラグで表す。
//   待機中     (isStarted = false, isDragging = false)
//     │ 左ボタンを押す
//     ▼
//   押下中     (isStarted = true , isDragging = false) … 動いた位置を draggingPositions にためておく
//     │ 押した位置から dragStartLength 以上動く → DragStart と、ためた位置の Dragging をまとめて発生させる
//     ▼
//   ドラッグ中 (isStarted = true , isDragging = true ) … 動くたびに Dragging
//     │ ボタンを離す → DragEnd
//     ▼
//   待機中
// 押下中のまま (ほとんど動かさずに) ボタンを離すと Click になり、ためた位置は捨てる。
// 少しの手ぶれでクリックがドラッグと見なされないよう、一定距離動くまではドラッグにしない。
class MouseEventConverter
{
    public event Action<Point>? Click    ;
    public event Action<Point>? DragStart;
    public event Action<Point>? Dragging ;
    public event Action<Point>? DragEnd  ;

    // ドラッグと見なす移動距離 (ボタンを押した位置からの距離)
    const double dragStartLength   = 10.0;

    // ボタンが押されていて、クリックかドラッグかを判定中または判定済み
    bool         isStarted         = false;
    // ドラッグ中と判定済み
    bool         isDragging        = false;
    // ボタンを押した位置 (DragStart の位置)
    Point        startPosition;
    // 最後に分かったマウスの位置 (キャプチャーを失ったときの DragEnd の位置)
    Point        lastPosition;
    // ドラッグと判定されるまでに動いた位置。判定されたら Dragging として順に流し、軌跡が途切れないようにする
    Queue<Point> draggingPositions = new();

    public void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
    {
        OnMouseLeftButtonDown(e.ToPosition(sender));
        // 要素の外でボタンを離しても MouseLeftButtonUp を受け取れるようにする
        ((UIElement)sender).CaptureMouse();
    }

    public void OnMouseMove(object sender, MouseEventArgs e)
    {
        // ボタンを押さずに動かしたときの移動は無視する
        if (e.LeftButton == MouseButtonState.Pressed)
            OnMouseMove(e.ToPosition(sender));
    }

    public void OnMouseLeftButtonUp(object sender, MouseEventArgs e)
    {
        OnMouseLeftButtonUp(e.ToPosition(sender));
        // ReleaseMouseCapture() でも LostMouseCapture が発生するが、
        // 先に OnMouseLeftButtonUp で待機中に戻しているので、OnLostMouseCapture では何も起きない
        ((UIElement)sender).ReleaseMouseCapture();
    }

    public void OnLostMouseCapture(object sender, MouseEventArgs e) => OnLostMouseCapture();

    // 以下の Point を受け取るオーバーロードが状態遷移の本体。
    // 上の WPF のイベントハンドラーは、座標を取り出してマウスキャプチャーを操作し、これらを呼ぶだけ

    // 待機中 → 押下中
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
            // まだクリックかドラッグか分からないので、位置をためておく
            draggingPositions.Enqueue(position);
            // 押下中 → ドラッグ中: ためた位置をまとめて流して、ボタンを押した位置からの軌跡を受け手に渡す
            if (CanDragStart(position)) {
                RaiseDraggingWithDraggingPositions();
                isDragging = true;
            }
        }
    }

    public void OnMouseLeftButtonUp(Point position)
    {
        if (isStarted && !isDragging && CanDragStart(position))
            OnMouseMove(position);

        // ドラッグ中 → 待機中
        if (isDragging) {
            RaiseDragEnd(position);
            isDragging = isStarted = false;
        // 押下中 → 待機中: ドラッグと見なすほど動いていないのでクリック
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

    // ドラッグと判定した時点で、DragStart (ボタンを押した位置) と、それまでにためた位置の Dragging を順に発生させる
    void RaiseDraggingWithDraggingPositions()
    {
        RaiseDragStart(startPosition);
        draggingPositions.ForEach(draggingPosition => RaiseDragging(draggingPosition));
        draggingPositions.Clear();
    }

    void RaiseDragEnd(Point position) => DragEnd?.Invoke(position);
}
