using System.Windows.Forms;

namespace BezierWinForm
{
    public partial class BezierWinForm : Form
    {
        public BezierWinForm()
        {
            InitializeComponent();

            view.MouseDown += (sender, e) => controller.OnMouseDown(e.Location);
            view.MouseMove += (sender, e) => controller.OnMouseMove(e.Location);
            view.MouseUp   += (sender, e) => controller.OnMouseUp  (e.Location);

            model.Update += view.OnUpdateModel;
            controller.Update += view.OnUpdateController;
        }
    }
}
