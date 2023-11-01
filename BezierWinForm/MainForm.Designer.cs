namespace BezierWinForm
{
    partial class BezierWinForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.view = new View();
            this.model = new Model(this.components);
            this.controller = new Controller(this.components);
            this.SuspendLayout();
            // 
            // view
            // 
            this.view.DataSource = this.model;
            this.view.Dock = System.Windows.Forms.DockStyle.Fill;
            this.view.Location = new System.Drawing.Point(0, 0);
            this.view.Margin = new System.Windows.Forms.Padding(32, 27, 32, 27);
            this.view.Name = "view";
            this.view.Size = new System.Drawing.Size(1849, 1683);
            this.view.TabIndex = 0;
            // 
            // controller
            // 
            this.controller.Model = this.model;
            // 
            // BezierWinForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(19F, 36F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1849, 1683);
            this.Controls.Add(this.view);
            this.Margin = new System.Windows.Forms.Padding(10, 9, 10, 9);
            this.Name = "BezierWinForm";
            this.Text = "BezierWinForm";
            this.ResumeLayout(false);

        }

        #endregion

        private View view;
        private Model model;
        private Controller controller;
    }
}

