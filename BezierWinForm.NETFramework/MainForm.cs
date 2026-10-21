using System.Windows.Forms;

namespace BezierWinForm
{
    public partial class BezierWinForm : Form
    {
        public BezierWinForm()
        {
            InitializeComponent();

            view.MouseDown += (sender, e) => { if (e.Button == MouseButtons.Left) controller.OnMouseDown(e.Location); };
            view.MouseMove += (sender, e) => controller.OnMouseMove(e.Location);
            view.MouseUp   += (sender, e) => { if (e.Button == MouseButtons.Left) controller.OnMouseUp  (e.Location); };

            model.Update += view.OnUpdateModel;
            controller.Update += view.OnUpdateController;
        }
    }
}
