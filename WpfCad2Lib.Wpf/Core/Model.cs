using WpfCad2Lib.Core;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// CAD の Model。図形データ (<see cref="CadData"/>) と、次に作る図形の属性を持つ。
/// 図形の追加と選択を受け付ける。
/// </summary>
public class Model
{
    readonly CadData         data            = new();
    //readonly IList<Figure>   figures         = new List<Figure>();
    //readonly FigureAttribute figureAttribute = new FigureAttribute();         
    // モデル座標で表した作図領域の既定の大きさ。View はこの領域がちょうど収まるように表示倍率を決める
    const double             defaultSize     = 1000;

    /// <summary>
    /// 次に作る図形の属性。ここを変えても作成済みの図形は変わらない (AddCommand が図形ごとに複製を渡すため)。
    /// </summary>
    public FigureAttribute FigureAttribute => data.FigureAttribute;

    /// <summary>作図領域 (モデル座標)。View はこれを基準に拡大率を決め、選択の判定距離もこの大きさから求める。</summary>
    public Rect Area { get; set; } = new(new Point(0, 0), new Size(defaultSize, defaultSize));

    // クリック位置から図形までがこの距離以内なら「近い」とみなす。作図領域の大きさに比例させる
    double NearDistance => MathHelper.Average(Area.Width, Area.Height) * Common.NearDistanceRate;

    //IList<Figure> Data
    //{
    //    get { return data; }
    //}

    /// <summary>図形を追加する (Undo の履歴にも記録される)。図形の要素はすでに Canvas に追加されている前提。</summary>
    public void Add(Figure item) => data.Add(item);
    
    //public Figure Find(Point point)
    //{
    //    return data.Find(point, NearDistance);
    //}

    /// <summary>
    /// <paramref name="position"/> に最も近い図形だけを選択する。近くに図形が無ければすべての選択を外す。
    /// </summary>
    /// <param name="elementCollection">選択マークの追加先 (View の Canvas.Children)。</param>
    /// <param name="position">クリックした位置 (モデル座標)。</param>
    public void Select(UIElementCollection elementCollection, Point position)
        => data.Select(elementCollection, position, NearDistance);

    //public IEnumerator<Figure> GetEnumerator()
    //{
    //    return figures.GetEnumerator();
    //}

    //IEnumerator IEnumerable.GetEnumerator()
    //{
    //    return GetEnumerator();
    //}

    //public int IndexOf(Figure item)
    //{
    //    return figures.IndexOf(item);
    //}

    //public void Insert(int index, Figure item)
    //{
    //    figures.Insert(index, item);
    //}

    //public void RemoveAt(int index)
    //{
    //    figures.RemoveAt(index);
    //}

    //public Figure this[int index]
    //{
    //    get
    //    {
    //        return figures[index];
    //    }
    //    set
    //    {
    //        figures[index] = value;
    //    }
    //}

    //public void Add(Figure item)
    //{
    //    figures.Add(item);
    //}

    //public void Clear()
    //{
    //    figures.Clear();
    //}

    //public bool Contains(Figure item)
    //{
    //    return figures.Contains(item);
    //}

    //public void CopyTo(Figure[] array, int arrayIndex)
    //{
    //    figures.CopyTo(array, arrayIndex);
    //}

    //public int Count
    //{
    //    get { return figures.Count; }
    //}

    //public bool IsReadOnly
    //{
    //    get { return figures.IsReadOnly; }
    //}

    //public bool Remove(Figure item)
    //{
    //    return figures.Remove(item);
    //}
}
