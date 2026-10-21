using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;



namespace BezierWinForm
{
    public partial class View : UserControl
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Model? DataSource { get; set; }

        IDrawable? dragging = null;

        public View()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        // ドラッグ中の曲線も OnPaint で確定済みの図形と同じ設定 (アンチエイリアス) で描く
        public void OnUpdateController(IDrawable drawable)
        {
            dragging = drawable;
            Invalidate();
        }

        public void OnUpdateModel(IDrawable drawable)
        {
            Invalidate();
        }

        void OnPaint(object sender, PaintEventArgs e)
        {
            SetAntiAlias(e.Graphics);
            if (DataSource is not null) {
                foreach (var figure in DataSource)
                    figure.Draw(e.Graphics);
            }
            dragging?.Draw(e.Graphics);
        }

        static void SetAntiAlias(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
        }
    }
}
