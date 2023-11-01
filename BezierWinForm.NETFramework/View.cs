using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BezierWinForm
{
    public partial class View : UserControl
    {
        public Model DataSource { get; set; }

        public View()
        {
            InitializeComponent();
        }

        public void OnUpdateController(IDrawable drawable)
        {
            using (var graphics = CreateGraphics()) {
                drawable.Draw(graphics);
            }
        }

        public void OnUpdateModel(IDrawable drawable)
        {
            Invalidate();
        }

        void OnPaint(object sender, PaintEventArgs e)
        {
            if (DataSource == null)
                return;

            SetAntiAlias(e.Graphics);
            foreach (var figure in DataSource)
                figure.Draw(e.Graphics);
        }

        static void SetAntiAlias(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
        }
    }
}
