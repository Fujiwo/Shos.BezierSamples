using System;
using System.ComponentModel;
using System.Drawing;

namespace BezierWinForm
{
    /// <summary>
    /// MVC の Controller。マウスのドラッグから曲線 (<see cref="BezierLine"/>) を作り、確定したら Model に追加する。
    /// </summary>
    /// <remarks>
    /// ドラッグ中の曲線はまだ Model に入れず、Controller 自身が持つ。
    /// 点が増えるたびに Update で自分自身 (IDrawable) を View に渡し、View にドラッグ中の曲線を描かせる。
    /// デザイナーで配置できるよう Component として実装している。
    /// </remarks>
    public partial class Controller : Component,  IDrawable, IDrawableSubject
    {
        /// <summary>
        /// ドラッグ中の曲線に通過点が追加されたときに発生する。引数はこの Controller 自身。
        /// </summary>
        public event Action<IDrawable> Update;

        // ドラッグ中の曲線。ドラッグの状態も表し、null ならドラッグしていない、null でなければドラッグ中。
        // MouseDown で作成し、MouseUp で Model に渡して null に戻す。
        BezierLine bezierLine = null;

        /// <summary>
        /// ドラッグを終えて確定した曲線の追加先。
        /// </summary>
        public Model Model { get; set; }

        /// <summary>
        /// Controller を初期化する。
        /// </summary>
        public Controller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Controller を初期化し、指定されたコンテナー (フォームの components) に登録する。
        /// </summary>
        public Controller(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        /// <summary>
        /// ドラッグを開始する。押した位置を始点とする新しい曲線を作る。
        /// </summary>
        public void OnMouseDown(PointF point)
        {
            bezierLine = new BezierLine();
            bezierLine.Add(point);
        }

        /// <summary>
        /// ドラッグ中なら、マウスの位置を曲線の通過点として追加する。
        /// </summary>
        public void OnMouseMove(PointF point)
        {
            // ドラッグしていないとき (bezierLine が null) のマウス移動は無視する。
            if (bezierLine != null) {
                // 直前の点に近すぎて間引かれたときは形が変わらないので、再描画を要求しない。
                if (bezierLine.Add(point) && Update != null)
                    Update(this);
            }
        }

        /// <summary>
        /// ドラッグを終える。離した位置を終点にして曲線を確定し、Model に追加する。
        /// </summary>
        public void OnMouseUp(PointF point)
        {
            if (bezierLine != null) {
                bezierLine.AddLast(point);
                // Model.Add が Model の Update を発生させ、View が確定済みの図形をすべて描き直す。
                if (Model != null)
                    Model.Add(bezierLine);
                // ドラッグ終了。以降 Draw は何も描かないので、曲線は Model 側からだけ描かれる。
                bezierLine = null;
            }
        }

        /// <summary>
        /// ドラッグ中の曲線を描く。ドラッグしていないときは何も描かない。
        /// </summary>
        public void Draw(Graphics graphics)
        {
            if (bezierLine != null)
                bezierLine.Draw(graphics);
        }
    }
}
