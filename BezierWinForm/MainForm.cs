using System.Windows.Forms;

namespace BezierWinForm;

/// <summary>
/// メイン フォーム。デザイナーで配置した Model / View / Controller を結線する。
/// </summary>
/// <remarks>
/// 各部の役割とイベントの流れは次のとおり。
/// <list type="bullet">
/// <item>View: マウス操作を受け取り、Controller へ渡す。描画もここで行う。</item>
/// <item>Controller: ドラッグ中の曲線を組み立て、点が増えるたびに Update で View に再描画させる。</item>
/// <item>Model: 確定した図形を保持し、追加されたら Update で View に再描画させる。</item>
/// </list>
/// view.DataSource と controller.Model への model の設定は InitializeComponent (デザイナー) が行う。
/// </remarks>
public partial class BezierWinForm : Form
{
    /// <summary>
    /// フォームを初期化し、マウス イベントと更新通知を結線する。
    /// </summary>
    public BezierWinForm()
    {
        InitializeComponent();

        // View のマウス イベントを Controller に渡す。
        // ドラッグの開始と終了は左ボタンだけを対象にする。
        // 解放通知を取りこぼした場合も、左ボタンなしの移動で作図を終了する。
        view.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) controller.OnMouseDown(e.Location); };
        view.MouseMove += (_, e) => {
            if ((e.Button & MouseButtons.Left) != 0)
                controller.OnMouseMove(e.Location);
            else
                controller.OnMouseCaptureLost();
        };
        view.MouseUp   += (_, e) => { if (e.Button == MouseButtons.Left) controller.OnMouseUp  (e.Location); };
        view.MouseCaptureChanged += (_, _) => { if (!view.Capture) controller.OnMouseCaptureLost(); };

        // 確定した図形の追加 (Model) と、ドラッグ中の曲線の変化 (Controller) を View に通知する。
        model.Update += view.OnUpdateModel;
        controller.Update += view.OnUpdateController;
    }
}
