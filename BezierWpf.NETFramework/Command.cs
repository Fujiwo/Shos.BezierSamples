using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    /// <summary>
    /// 描画モード (編集・線分・矩形・曲線など) ごとのマウス操作の処理の基底クラス (Command パターン)。
    /// <see cref="Controller"/> は受け取ったクリック/ドラッグを現在の Command にそのまま委譲するので、
    /// モードの違いはすべて派生クラスに閉じ込められる。何もしない既定の実装を持ち、派生クラスは必要なものだけをオーバーライドする。
    /// </summary>
    public abstract class Command : IMouseAcceptor
    {
        /// <summary>
        /// 図形を追加する先 (Canvas の子要素)。<see cref="Controller"/> が設定する。
        /// </summary>
        public UIElementCollection Model { protected get; set; }

        /// <summary>
        /// クリックされたときの処理。既定では何もしない。
        /// </summary>
        public virtual void OnClick    (Point position) {}
        /// <summary>
        /// ドラッグが始まったときの処理。既定では何もしない。
        /// </summary>
        public virtual void OnDragStart(Point position) {}
        /// <summary>
        /// ドラッグ中の処理。既定では何もしない。
        /// </summary>
        public virtual void OnDragging (Point position) {}
        /// <summary>
        /// ドラッグが終わったときの処理。既定では何もしない。
        /// </summary>
        public virtual void OnDragEnd  (Point position) {}
    }

    /// <summary>
    /// 編集モード。このサンプルでは選択などの編集機能は未実装で、マウス操作に対して何もしない (起動直後のモード)。
    /// </summary>
    public class EditCommand : Command
    { }

    /// <summary>
    /// ドラッグで図形を 1 つ追加するモードの基底クラス (Template Method パターン)。
    /// ドラッグの流れ (図形の生成・属性の設定・Model への追加) はここで共通に扱い、
    /// どんな図形を作り、マウスの位置に合わせてどう形を変えるかを派生クラスが決める。
    /// </summary>
    public abstract class AddCommand : Command
    {
        const double StrokeThickness = 3.0;

        static Brush Stroke
        {
            get { return Brushes.Black; }
        }

        /// <summary>
        /// ドラッグを始めた位置 (マウスのボタンを押した位置)。
        /// </summary>
        protected Point StartPosition { get; private set; }
        /// <summary>
        /// 現在ドラッグで描いている図形。ドラッグ中以外は null。
        /// </summary>
        protected Shape Shape { get; private set; }

        /// <summary>
        /// ドラッグの開始位置を覚える。図形はまだ作らない (最初の <see cref="OnDragging"/> で作る)。
        /// </summary>
        public override void OnDragStart(Point position)
        {
            StartPosition = position;
            Shape = null;
            OnStart();
        }

        /// <summary>
        /// 最初の呼び出しで図形を作って Model (Canvas) に追加し、以降はマウスの位置に合わせて形を変える。
        /// </summary>
        public override void OnDragging(Point position)
        {
            // 図形を Canvas に追加すると、その場で画面に表示される。
            // 以降は Shape のプロパティを書き換えるだけで、WPF が自動的に描き直す
            if (Shape == null) {
                Shape = CreateShape();
                SetAttribute(Shape);
                if (Model != null)
                    Model.Add(Shape);
            }
            Reshape(position);
        }

        /// <summary>
        /// 最後の位置で形を確定する。図形は Canvas に残したまま、描いている図形の参照だけを手放す。
        /// </summary>
        public override void OnDragEnd(Point position)
        {
            ReshapeLast(position);
            Shape = null;
        }

        /// <summary>
        /// ドラッグの開始時に呼ばれる。派生クラスが、ドラッグごとの状態を初期化するのに使う。
        /// </summary>
        protected virtual void OnStart() {}
        /// <summary>
        /// 追加する図形を作る。最初の <see cref="OnDragging"/> で 1 度だけ呼ばれる。
        /// </summary>
        protected abstract Shape CreateShape();
        /// <summary>
        /// ドラッグ中のマウスの位置に合わせて、<see cref="Shape"/> の形を変える。
        /// </summary>
        protected abstract void Reshape(Point position);

        /// <summary>
        /// ドラッグを終えた位置で形を確定する。既定では <see cref="Reshape"/> と同じ。
        /// </summary>
        protected virtual void ReshapeLast(Point position)
        {
            Reshape(position);
        }

        void SetAttribute(Shape shape)
        {
            shape.Stroke = Stroke;
            shape.StrokeThickness = StrokeThickness;
        }
    }

    /// <summary>
    /// ドラッグの開始位置から現在位置まで線分を描くモード。
    /// </summary>
    public class LineCommand : AddCommand
    {
        /// <summary>
        /// 始点をドラッグの開始位置にした線分を作る。
        /// </summary>
        protected override Shape CreateShape()
        {
            return new Line { X1 = StartPosition.X, Y1 = StartPosition.Y };
        }

        /// <summary>
        /// 線分の終点をマウスの位置に動かす。
        /// </summary>
        protected override void Reshape(Point position)
        {
            ((Line)Shape).X2 = position.X;
            ((Line)Shape).Y2 = position.Y;
        }
    }

    /// <summary>
    /// ドラッグの開始位置と現在位置を対角とする矩形を描くモード。
    /// </summary>
    public class RectangleCommand : AddCommand
    {
        /// <summary>
        /// 矩形を作る。位置と大きさは <see cref="Reshape"/> で決める。
        /// </summary>
        protected override Shape CreateShape()
        {
            return new Rectangle();
        }

        /// <summary>
        /// 開始位置とマウスの位置を対角とするように、矩形の位置と大きさを変える。
        /// </summary>
        protected override void Reshape(Point position)
        {
            // Rectangle は位置を持たず Width / Height は負にできないので、2 点から正規化した Rect を作り、
            // 左上を Canvas の添付プロパティ (Left / Top) で、大きさを Width / Height で指定する。
            // こうすると、左上以外の方向へドラッグしても正しく描ける
            var rect = new Rect(StartPosition, position);
            Canvas.SetLeft(Shape, rect.Left);
            Canvas.SetTop (Shape, rect.Top );
            ((Rectangle)Shape).Width  = rect.Width ;
            ((Rectangle)Shape).Height = rect.Height;
        }
    }

    /// <summary>
    /// ドラッグの軌跡を通過点として、それらを滑らかに通るベジエ曲線を描くモード。
    /// </summary>
    public class BezierCommand : AddCommand
    {
        // 点の間引きの距離。直前の通過点からこれより遠くへ動いたときだけ、新しい通過点にする
        const double sensitivity = 16.0;

        // 今回のドラッグで集めた通過点
        List<Point> positions = null;

        /// <summary>
        /// 通過点の列を、ドラッグの開始位置だけで始める。
        /// </summary>
        protected override void OnStart()
        {
            positions  = new List<Point> { StartPosition };
        }

        /// <summary>
        /// 曲線を描く <see cref="Path"/> を作る。形 (Data) は <see cref="Reshape"/> で設定する。
        /// </summary>
        protected override Shape CreateShape()
        {
            return new Path();
        }

        /// <summary>
        /// 通過点を間引きながら集め、そのたびに曲線のジオメトリーを作り直す。
        /// </summary>
        protected override void Reshape(Point position)
        {
            // マウスの移動イベントは 1 ピクセル単位でも来るので、すべてを通過点にすると、手の細かい揺れで曲線が波打つ。
            // 直前の通過点から sensitivity より遠くへ動いたときだけ追加して、点を間引く
            if (positions.Count > 0) {
                var lastPosition = positions[positions.Count - 1];
                if ((position - lastPosition).Length > sensitivity)
                    positions.Add(position);
            }
            // 1 点では線にならず、2 点では直線にしかならないので、3 点以上たまってから描く
            if (positions.Count > 2)
                ((Path)Shape).Data = GraphicHelper.BezierPathGeometry(positions);
        }

        /// <summary>
        /// ドラッグを終えた位置を最後の通過点にして、曲線を確定する。
        /// </summary>
        // ドラッグの終点は間隔に関係なく必ず通るようにする (直前の点に近すぎる場合はその点を置き換える)
        protected override void ReshapeLast(Point position)
        {
            var lastPosition = positions[positions.Count - 1];
            if (positions.Count >= 2 && (position - lastPosition).Length <= sensitivity)
                positions[positions.Count - 1] = position;
            else if (position != lastPosition)
                positions.Add(position);
            if (positions.Count > 2)
                ((Path)Shape).Data = GraphicHelper.BezierPathGeometry(positions);
        }
    }
}
