using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// MouseEventConverter が変換したクリック/ドラッグを受け取る側のインターフェイス。
/// Controller と Command が実装する。
/// 座標はすべて Canvas 上の座標 (= ズーム・パン前のモデル座標)。
/// </summary>
public interface IMouseAcceptor
{
    /// <summary>操作対象の Model。</summary>
    Model               Model             { set; }
    /// <summary>図形の UIElement を追加・削除する先 (View の Canvas.Children)。</summary>
    UIElementCollection ElementCollection { set; }
    
    /// <summary>ほとんど動かさずにボタンを離したときに呼ばれる。</summary>
    void OnClick    (Point position);
    /// <summary>ドラッグと判定されたときに、ボタンを押した位置で 1 回呼ばれる。</summary>
    void OnDragStart(Point position);
    /// <summary>ドラッグ中のマウス移動ごとに呼ばれる。</summary>
    void OnDragging (Point position);
    /// <summary>ドラッグ中にボタンを離したとき (またはキャプチャーを失ったとき) に呼ばれる。</summary>
    void OnDragEnd  (Point position);
}
