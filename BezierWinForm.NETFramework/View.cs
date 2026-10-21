using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BezierWinForm
{
    /// <summary>
    /// MVC の View。Model の図形とドラッグ中の曲線を描く。
    /// </summary>
    /// <remarks>
    /// Model / Controller から更新通知を受けたら、Invalidate で再描画を要求するだけにし、
    /// 実際の描画はすべて Paint イベント (OnPaint) でまとめて行う。
    /// マウス イベントはフォームが Controller へ渡す。
    /// </remarks>
    public partial class View : UserControl
    {
        /// <summary>
        /// 描画する図形を保持する Model。
        /// </summary>
        public Model DataSource { get; set; }

        // ドラッグ中の曲線を描くもの (Controller)。最初に Controller から通知を受けたときに設定される。
        // ドラッグしていないときの Controller は何も描かないので、ドラッグ後も保持したままでよい。
        IDrawable dragging = null;

        /// <summary>
        /// View を初期化する。
        /// </summary>
        public View()
        {
            InitializeComponent();
            // 描画中の画面が見えてちらつかないよう、裏の画面に描いてからまとめて表示する。
            DoubleBuffered = true;
        }

        // ドラッグ中の曲線も OnPaint で確定済みの図形と同じ設定 (アンチエイリアス) で描く
        /// <summary>
        /// Controller の Update を受け取る。ドラッグ中の曲線が変わったので再描画を要求する。
        /// </summary>
        public void OnUpdateController(IDrawable drawable)
        {
            dragging = drawable;
            Invalidate();
        }

        /// <summary>
        /// Model の Update を受け取る。図形が追加されたので全体の再描画を要求する。
        /// </summary>
        public void OnUpdateModel(IDrawable drawable)
        {
            Invalidate();
        }

        // Paint イベントのハンドラー (View.Designer.cs で登録している)。
        // 確定済みの図形を追加した順に描き、最後にドラッグ中の曲線を一番上に重ねて描く。
        void OnPaint(object sender, PaintEventArgs e)
        {
            SetAntiAlias(e.Graphics);
            if (DataSource != null) {
                foreach (var figure in DataSource)
                    figure.Draw(e.Graphics);
            }
            if (dragging != null)
                dragging.Draw(e.Graphics);
        }

        // 線をアンチエイリアスで滑らかに描くよう設定する。
        // PixelOffsetMode.Half は、ピクセルを半ピクセル (−0.5) ずらして扱い、アンチエイリアスの見た目を整える。
        static void SetAntiAlias(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
        }
    }
}
