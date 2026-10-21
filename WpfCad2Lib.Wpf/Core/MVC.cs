namespace WpfCad2Lib.Wpf;

/// <summary>
/// View・Model・Controller を結線する入口のクラス。
/// 利用側は View (XAML に置いた UserControl) と最初の Command を渡して生成し、
/// 以後は <see cref="Command"/> に新しい Command を設定して描画モードを切り替える。
/// </summary>
/// <remarks>
/// 役割分担:
/// Model は図形データ (<see cref="CadData"/>) と現在の図形属性を持つ。
/// View はマウス入力を受けてクリック/ドラッグに変換し、Controller に渡す。また拡大・縮小・パンを行う。
/// Controller は受け取った操作を現在の Command にそのまま委譲する。
/// Command (Edit / 線分 / 矩形 など) が実際の処理 (図形の作成・選択) を行う。
/// </remarks>
public class MVC
{
    readonly Controller controller = new();
    readonly Model      model      = new();

    /// <summary>図形データと現在の図形属性を持つ Model。</summary>
    public Model Model => model;

    /// <summary>現在の描画モード。設定すると Controller 経由で Command に Model と描画先が渡される。</summary>
    public Command Command
    {
        set => controller.Command = value;
    }

    /// <summary>View・Model・Controller を結線し、最初の Command を設定する。</summary>
    /// <param name="view">描画先となる View。</param>
    /// <param name="defaultCommand">最初に有効にする Command (通常は EditCommand)。</param>
    public MVC(View view, Command defaultCommand)
    {
        // 順序が重要: View.Controller のセッターが Controller に Model と Canvas.Children を渡し、
        // Controller.Command のセッターがそれを Command に渡す。
        // そのため Model → Controller → Command の順に設定する。
        view.Model         = model;
        view.Controller    = controller;
        controller.Command = defaultCommand;
    }
}
