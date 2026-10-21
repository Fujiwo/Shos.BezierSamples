using System.Windows;
using System.Windows.Controls;

namespace BezierWpf;

/// <summary>
/// <see cref="MouseEventConverter"/> がマウスの生のイベントから変換した「クリック」「ドラッグ」を受け取る側のインターフェイス。
/// <see cref="View"/> から見た Controller の窓口で、<see cref="Controller"/> と各 <see cref="Command"/> が実装する。
/// 座標はいずれも描画先の Canvas 上の位置。
/// </summary>
public interface IMouseAcceptor
{
    /// <summary>
    /// 図形を追加する先 (Model)。この簡易サンプルでは、Canvas の子要素のコレクションをそのまま Model として使う。
    /// </summary>
    UIElementCollection Model { set; }

    /// <summary>
    /// ボタンを押した位置からほとんど動かさずに離したときに呼ばれる。
    /// </summary>
    void OnClick(Point position);
    /// <summary>
    /// ドラッグが始まったときに、ボタンを押した位置で呼ばれる。
    /// </summary>
    void OnDragStart(Point position);
    /// <summary>
    /// ドラッグ中にマウスが動くたびに呼ばれる。
    /// </summary>
    void OnDragging(Point position);
    /// <summary>
    /// ドラッグ中にボタンを離した (またはマウスキャプチャーを失った) ときに呼ばれる。
    /// </summary>
    void OnDragEnd(Point position);
}
