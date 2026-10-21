using WpfCad2Lib.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 図形の一覧と、次に作る図形の属性、Undo/Redo の履歴をまとめて持つ。
/// 図形の追加・削除・更新は必ず <see cref="CadUndo"/> を通して行い、履歴に記録する。
/// </summary>
/// <remarks>
/// 管理するのは図形の一覧 (データ) だけで、Undo/Redo しても Canvas 上の要素は自動では増減しない。
/// </remarks>
public class CadData : IEnumerable<Figure>
{
    readonly IList<Figure>   figures         = [];
    readonly CadUndo         undo            = new();

    /// <summary>次に作る図形の属性。</summary>
    public FigureAttribute FigureAttribute { get; } = new();
    /// <summary>元に戻せる操作があるかどうか。</summary>
    public bool CanUndo => undo.CanUndo;
    /// <summary>やり直せる操作があるかどうか。</summary>
    public bool CanRedo => undo.CanRedo;
    /// <summary>図形が 1 つも無いかどうか。</summary>
    public bool IsEmpty => Count == 0;
    /// <summary>図形の数。</summary>
    public int Count => figures.Count;

    /// <summary>図形を追加順に列挙する。</summary>
    public IEnumerator<Figure> GetEnumerator() => figures.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    //public int IndexOf(Figure item)
    //{
    //    return figures.IndexOf(item);
    //}

    //public void Insert(int index, Figure figure)
    //{
    //    figures.Insert(index, figure);
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

    /// <summary>図形を追加し、Undo の履歴に記録する。</summary>
    public void Add(Figure figure) => undo.Add(figures, figure);

    /// <summary>図形を削除し、Undo の履歴に記録する。</summary>
    public void Remove(Figure figure) => undo.Remove(figures, figure);
        
    /// <summary>
    /// <paramref name="olderFigure"/> を <paramref name="newerFigure"/> に置き換え (同じ位置に入れ替え)、Undo の履歴に記録する。
    /// </summary>
    /// <param name="figures">置き換えを行う図形の一覧。</param>
    /// <param name="olderFigure">置き換えられる図形。</param>
    /// <param name="newerFigure">新しい図形。</param>
    /// <returns><paramref name="olderFigure"/> が一覧に無く、置き換えなかった場合は false。</returns>
    public bool Update(IList<Figure> figures, Figure olderFigure, Figure newerFigure)
        => undo.Update(figures, olderFigure, newerFigure);

    /// <summary>すべての図形を 1 つずつ削除し、それぞれを Undo の履歴に記録する。</summary>
    public void RemoveAll()
    {
        // 削除しながら走査するので、インデックスがずれない末尾から削除する
        for (var index = figures.Count - 1; index >= 0; index--)
            Remove(figures[index]);
    }

    /// <summary>
    /// <paramref name="position"/> から <paramref name="nearDistance"/> 以内で最も近い図形だけを選択し、他の選択を外す。
    /// 該当する図形が無ければすべての選択を外す。
    /// </summary>
    /// <param name="elementCollection">選択マークの追加先 (View の Canvas.Children)。</param>
    /// <param name="position">クリックした位置 (モデル座標)。</param>
    /// <param name="nearDistance">「近い」とみなす距離。</param>
    public void Select(UIElementCollection elementCollection, Point position, double nearDistance)
    {
        var figure = Find(position, nearDistance);
        if (figure is null)
            SelectAll(elementCollection, false);
        else
            Select(elementCollection, figure);
    }

    void SelectAll(UIElementCollection elementCollection, bool isSelected = true)
        => figures.ForEach(figure => figure.Select(elementCollection, isSelected));

    // figure だけを選択状態にし、他はすべて選択を外す
    void Select(UIElementCollection elementCollection, Figure figure)
        => figures.ForEach(aFigure => aFigure.Select(elementCollection, object.ReferenceEquals(aFigure, figure)));

    /// <summary>選択状態が <paramref name="isSelected"/> と一致する図形をすべて返す。</summary>
    public IEnumerable<Figure> GetSelected(bool isSelected = true)
    {
        foreach (var figure in figures) {
            if (figure.IsSelected == isSelected)
                yield return figure;
        }
    }

    /// <summary>選択状態が <paramref name="isSelected"/> と一致する最初の図形を返す。無ければ null。</summary>
    public Figure? GetSelectedOne(bool isSelected = true)
        => figures.FirstOrDefault(figure => figure.IsSelected == isSelected);

    /// <summary><paramref name="point"/> から <paramref name="nearDistance"/> 以内で最も近い図形を返す。無ければ null。</summary>
    public Figure? Find(Point point, double nearDistance)
    {
        // IsNear は「これまでの最小距離より近い」ときだけ true を返し minimumDistance を更新するので、
        // 最後に true を返した図形が最も近い図形になる
        Figure? result          = null;
        double  minimumDistance = double.MaxValue;

        foreach (var figure in figures) {
            if (figure.IsNear(point, nearDistance, ref minimumDistance))
                result = figure;
        }
        return result;
    }

    /// <summary>直前の操作グループを元に戻す。</summary>
    public void Undo()
    {
        undo.Undo(figures);
        //SelectAll(false);
    }

    /// <summary>元に戻した操作グループをやり直す。</summary>
    public void Redo()
    {
        undo.Redo(figures);
        //SelectAll(false);
    }

    /// <summary>図形と Undo の履歴をすべて消す (この操作自体は元に戻せない)。</summary>
    public void Clear()
    {
        figures.Clear();
        undo.Clear();
    }

    /// <summary>
    /// 新しい操作グループを始める。これ以降に記録した操作は、次に StartCommand を呼ぶまで 1 回の Undo/Redo でまとめて戻る。
    /// </summary>
    /// <remarks>現在はどこからも呼ばれていないため、記録したすべての操作が 1 つのグループになる (1 回の Undo ですべて戻る)。</remarks>
    public void StartCommand() => undo.StartCommand();

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
}
