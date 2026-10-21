using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// モデル座標 (図形の座標) とビュー座標 (View 上のピクセル位置) の変換行列を管理する。ズーム・パン・初期表示への復帰を行う。
/// </summary>
/// <remarks>
/// WPF の Matrix は「点 × 行列」(行ベクトル) で変換する。
/// <see cref="ModelToViewMatrix"/> は、まず <see cref="ModelArea"/> 全体が <see cref="ViewArea"/> に収まる倍率 (縦横で小さい方) に
/// ズーム率を掛けて拡大し、そのあとビュー座標での平行移動 (ズームの中心合わせ・パン) を行う。
/// View はこの行列を Canvas の RenderTransform に設定するので、図形はモデル座標のまま Canvas に置けばよい。
/// </remarks>
public class Device
{
    Rect         modelArea         = new(new Size(1.0, 1.0));
    Rect         viewArea          = new(new Size(1.0, 1.0));
    // 平行移動量。現在は Pan() / Zoom() が行列を直接平行移動するため、行列の計算には使われていない
    Vector       offset            = new();
    // ズーム率。1.0 が初期表示 (ModelArea 全体が ViewArea に収まる倍率)
    double       rate              = 1.0;
    Matrix       modelToViewMatrix = new();
    Matrix       viewToModelMatrix = new();

    /// <summary>表示したいモデル座標の範囲 (Model.Area)。変わると行列を作り直す。</summary>
    public Rect ModelArea
    {
        get => modelArea;
        set
        {
            if (value != modelArea) {
                modelArea = value;
                SetModelToViewMatrix();
            }
        }
    }

    /// <summary>表示先のビュー座標の範囲 (View の大きさ)。変わると行列を作り直す。</summary>
    /// <remarks>行列を作り直すと、それまでのズームの中心合わせとパンの平行移動は失われる。</remarks>
    public Rect ViewArea
    {
        get => viewArea;
        set
        {
            if (value != viewArea) {
                viewArea = value;
                SetModelToViewMatrix();
            }
        }
    }

    /// <summary>初期表示の倍率かどうか。ズーム率だけで判定し、パンしたかどうかは見ない。</summary>
    public bool IsHome => Rate == 1.0;

    /// <summary>モデル座標をビュー座標に変換する行列。</summary>
    public Matrix ModelToViewMatrix => modelToViewMatrix;

    /// <summary>ビュー座標をモデル座標に変換する行列 (<see cref="ModelToViewMatrix"/> の逆行列)。</summary>
    public Matrix ViewToModelMatrix => viewToModelMatrix;

    // ズーム率。ZoomMinimumRate ～ ZoomMaximumRate の範囲に収め、変わったときだけ行列を作り直す
    double Rate
    {
        get => rate;
        set
        {
            var newRate = Math.Min(Math.Max(value, Common.ZoomMinimumRate), Common.ZoomMaximumRate);
            if (newRate != rate) {
                rate = newRate;
                SetModelToViewMatrix();
            }
        }
    }

    Vector Offset
    {
        get => offset;
        set
        {
            if (value != offset) {
                offset = value;
                SetModelToViewMatrix();
            }
        }
    }

    /// <summary>ズームとパンを取り消して、ModelArea 全体が見える初期表示に戻す。</summary>
    public void Home()
    {
        // Pan() / Zoom() は行列を直接平行移動するため、offset と rate が初期値のままでも行列は作り直す
        offset = new();
        rate   = 1.0;
        SetModelToViewMatrix();
    }

    /// <summary>
    /// <paramref name="zoomCenterModelPoint"/> の画面上の位置を動かさずに、ズーム率を <paramref name="rate"/> 倍する。
    /// </summary>
    /// <param name="zoomCenterModelPoint">ズームの中心 (モデル座標)。マウスホイールを回したときのマウス位置など。</param>
    /// <param name="rate">ズーム率に掛ける倍率。1 より大きければ拡大、小さければ縮小。</param>
    public void Zoom(Point zoomCenterModelPoint, double rate)
    {
        // 1. 中心点の、ズーム前のビュー座標を求める
        // 2. ズーム率を変える (変わった場合は Rate のセッターで、平行移動を含まない拡大だけの行列に作り直される)
        // 3. 中心点の、ズーム後のビュー座標を求める
        // 4. その差だけビュー座標で逆向きに平行移動し、中心点を元の画面位置に戻す
        //    (ズーム前の行列が持っていた平行移動 (パンなど) も、この差に含まれるので引き継がれる)
        var oldZoomCenterViewPoint        = zoomCenterModelPoint * ModelToViewMatrix;
        Rate *= rate;
        var newZoomCenterViewPoint        = zoomCenterModelPoint * ModelToViewMatrix;
        var zoomCenterViewPointDifference = newZoomCenterViewPoint - oldZoomCenterViewPoint;
        modelToViewMatrix.Translate(-zoomCenterViewPointDifference.X, -zoomCenterViewPointDifference.Y);
        SetViewToModelMatrix();
    }

    /// <summary>表示範囲を <paramref name="direction"/> の向きに動かす。</summary>
    /// <param name="direction">表示範囲を動かす向き (図形はその逆向きに動いて見える)。</param>
    /// <param name="rate">移動量。ViewArea の対角線の長さに対する比率。</param>
    public void Pan(Direction direction, double rate)
    {
        // 移動量は View の大きさに比例させる (ズーム率には依存しない)
        var viewAreaTopLeft     = ViewArea.TopLeft    ;
        var viewAreaBottomRight = ViewArea.BottomRight;
        var panDistance         = viewAreaTopLeft.Distance(viewAreaBottomRight) * rate;
        var (panX, panY)        = direction switch {
            Direction.Left  => ( panDistance,          0.0),
            Direction.Right => (-panDistance,          0.0),
            Direction.Up    => (         0.0,  panDistance),
            Direction.Down  => (         0.0, -panDistance),
            _               => (         0.0,          0.0)
        };
        // 表示範囲を左へ動かす = 図形を右 (+X) へ平行移動する。拡大後のビュー座標で平行移動する
        modelToViewMatrix.Translate(panX, panY);
        SetViewToModelMatrix();
    }

    // 行列を初期状態 (ModelArea・ViewArea・ズーム率だけから決まる拡大行列) から作り直し、逆行列も更新する
    void SetModelToViewMatrix()
    {
        modelToViewMatrix = GetModelToViewMatrix();
        SetViewToModelMatrix();
    }

    // modelToViewMatrix を変えたら、必ずこれで逆行列も合わせる
    void SetViewToModelMatrix()
    {
        viewToModelMatrix = modelToViewMatrix;
        viewToModelMatrix.Invert();
    }

    Matrix GetModelToViewMatrix()
    {
        var matrix = new Matrix(); 
        //var modelAreaCenter = modelArea.Center();
        //matrix.Translate(-modelAreaCenter.X - offset.X, -modelAreaCenter.Y - offset.Y);

        // 縦と横のうち、はみ出さない方 (小さい方) の倍率を使い、ModelArea 全体が ViewArea に収まるようにする。
        // モデル座標の原点がビューの左上に来る (中央寄せはしない)
        Debug.Assert(modelArea.Width != 0.0 && modelArea.Height != 0.0);
        var xRate           = modelArea.Width  == 0.0 ? viewArea.Width  : viewArea.Width  / modelArea.Width ;
        var yRate           = modelArea.Height == 0.0 ? viewArea.Height : viewArea.Height / modelArea.Height;
        var scaleRate       = Math.Min(xRate, yRate) * rate;
        matrix.Scale(scaleRate, scaleRate);

        //var viewAreaCenter  = viewArea.Center();
        //matrix.Translate(viewAreaCenter.X, viewAreaCenter.Y);

        return matrix;
    }
}
