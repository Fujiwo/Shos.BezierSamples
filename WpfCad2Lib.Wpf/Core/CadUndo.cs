using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WpfCad2Lib.Wpf;

// 図形一覧に対する操作 (追加・削除・置き換え) を記録し、Undo/Redo する。
// 図形一覧そのものは持たず、CadData から渡された一覧を書き換える。
// 記録の単位は「操作 1 つ」だが、StartCommand で区切った「操作グループ」単位でまとめて Undo/Redo する
// (例えば、複数の図形を一度に削除した操作を 1 回の Undo で戻せる)。
class CadUndo
{
    // 1 回分の操作の記録。どの図形がどうなったかを、操作前 (OlderFigure) と操作後 (NewerFigure) で持つ。
    //   Add    : OlderFigure = null  , NewerFigure = 追加した図形
    //   Remove : OlderFigure = 削除した図形, NewerFigure = null
    //   Update : OlderFigure = 置き換え前, NewerFigure = 置き換え後
    class UndoData
    {
        public enum UndoType
        {
            Add, Remove, Update
        }

        public UndoType Type         { get; private set; }
        // 操作グループの先頭の記録なら true。Undo はここまで戻り、Redo はこの手前で止まる
        public bool     IsStart      { get;         set; }
        public Figure?  OlderFigure  { get; private set; }
        public Figure?  NewerFigure  { get; private set; }
        // 削除直前の位置。グループを逆順に戻すことで、その時点の並びも復元される。
        public int      RemovedIndex { get; private set; }

        UndoData(UndoType type, Figure? olderFigure, Figure? newerFigure, int removedIndex = -1)
        {
            //if (olderFigure is not null)
            //    olderFigure.IsSelected = false;
            Type         = type;
            OlderFigure  = olderFigure;
            NewerFigure  = newerFigure;
            RemovedIndex = removedIndex;
        }

        public static UndoData AddData(Figure figure)
            => new(UndoType.Add, null, figure);

        public static UndoData RemoveData(Figure figure, int index)
            => new(UndoType.Remove, figure, null, index);

        public static UndoData UpdateData(Figure olderFigure, Figure newerFigure)
            => new(UndoType.Update, olderFigure, newerFigure);
    }

    // 操作の記録を固定長の配列に循環させて保持するリングバッファー。
    //
    //   top     : 残っている最も古い記録の位置。これより前には Undo できない
    //   current : 次に記録を書き込む位置 (= 現在の状態の直後)。Undo で戻り、Redo で進む
    //   bottom  : 記録の終わり (最も新しい記録の直後)。Redo はここまで進める
    //
    //   [top ... current) が Undo できる記録、[current ... bottom) が Undo した後に Redo できる記録。
    //   新しく記録すると bottom = current となり、Redo できる記録は捨てられる。
    //   いっぱいになると top を進めて最も古い記録を捨てる (current == top は「空」を表すので、保持できるのは bufferMaxSize - 1 件)。
    class UndoBuffer
    {
        const int           bufferMaxSize = 1000;
        readonly UndoData[] data          = new UndoData[bufferMaxSize];

        int  top = 0;
        int  current = 0;
        int  bottom = 0;
        // true のとき、次に Push する記録を操作グループの先頭にする
        bool isStart = true;

        // 新しい操作グループを始める
        public void Start() => isStart = true;

        public void Push(UndoData undoData)
        {
            // グループの先頭かどうかを記録に刻み、current の位置に書き込んで進める
            undoData.IsStart = isStart;
            data[current] = undoData;
            current = NextOf(current);
            // ここより先 (Redo できた記録) は無効になる
            bottom = current;
            // 1 周して最も古い記録に追いついたら、それを捨てる
            if (current == top)
                top = NextOf(top);
            if (isStart)
                isStart = false;
        }

        public bool CanPop => current != top;

        public bool CanForward => current != bottom;

        // 操作グループ 1 つ分を新しい順に取り出す。グループの先頭 (IsStart) を取り出したところで止まる。
        // 遅延列挙で、列挙するたびに Pop して current を動かすので、呼び出し側は 1 回だけ列挙すること
        public IEnumerable<UndoData> Undo()
        {
            for (var undoData = Pop(); undoData is not null; undoData = Pop()) {
                yield return undoData;
                if (undoData.IsStart)
                    break;
            }
        }

        // 操作グループ 1 つ分を古い順に取り出す。次の記録が別のグループの先頭 (IsStart) なら、その手前で止まる。
        // Undo() と同じく遅延列挙で、列挙するたびに Forward して current を動かす
        public IEnumerable<UndoData> Redo()
        {
            for (var undoData = Forward(); undoData is not null; undoData = Forward()) {
                yield return undoData;
                if (CanForward && data[current].IsStart)
                    break;
            }
        }

        public void Clear()
        {
            top =
            current =
            bottom = 0;
        }

        // 1 つ戻って、その位置の記録を返す
        UndoData? Pop()
        {
            if (!CanPop)
                return null;
            current = PreviousOf(current);
            return data[current];
        }

        // 現在位置の記録を返して、1 つ進む
        UndoData? Forward()
        {
            if (!CanForward)
                return null;
            var result = data[current];
            current = NextOf(current);
            return result;
        }

        // 配列の端で反対側に回り込むよう、bufferMaxSize を法として前後の位置を求める
        static int PreviousOf(int pointer) => (pointer + bufferMaxSize - 1) % bufferMaxSize;

        static int NextOf(int pointer) => (pointer + 1) % bufferMaxSize;
    }

    readonly UndoBuffer undoBuffer = new();

    public bool CanUndo => undoBuffer.CanPop;

    public bool CanRedo => undoBuffer.CanForward;

    public void StartCommand() => undoBuffer.Start();

    // 以下の Add / Remove / Update は、図形一覧を書き換えたうえで、その操作を記録する

    public void Add(IList<Figure> figures, Figure figure)
    {
        figures.Add(figure);
        undoBuffer.Push(UndoData.AddData(figure));
    }

    public void Remove(IList<Figure> figures, Figure figure)
    {
        int index = figures.IndexOf(figure);
        if (index < 0)
            return;
        figures.RemoveAt(index);
        undoBuffer.Push(UndoData.RemoveData(figure, index));
    }

    public bool Update(IList<Figure> figures, Figure olderFigure, Figure newerFigure)
    {
        int index = figures.IndexOf(olderFigure);
        if (index < 0)
            return false;
        // 描画順 (一覧の順序) を保つため、同じ位置に入れてから古い方を取り除く
        figures.Insert(index, newerFigure);
        figures.Remove(olderFigure);
        undoBuffer.Push(UndoData.UpdateData(olderFigure, newerFigure));
        return true;
    }

    public bool Undo(IList<Figure> figures)
    {
        // UndoBuffer.Undo() は列挙するたびに Pop するため、先に確定させてから使う
        var poppedFigures = undoBuffer.Undo().ToList();
        poppedFigures.ForEach(poppedFigure => Undo(figures, poppedFigure));
        return poppedFigures.Count > 0;
    }

    public bool Redo(IList<Figure> figures)
    {
        // UndoBuffer.Redo() は列挙するたびに Forward するため、先に確定させてから使う
        var poppedFigures = undoBuffer.Redo().ToList();
        poppedFigures.ForEach(poppedFigure => Redo(figures, poppedFigure));
        return poppedFigures.Count > 0;
    }

    public void Clear() => undoBuffer.Clear();

    // 記録 1 つ分の操作を逆向きに適用する (追加 → 削除、削除 → 追加、置き換え → 元に戻す)
    void Undo(IList<Figure> figures, UndoData _undoData)
    {
        switch (_undoData.Type) {
            case UndoData.UndoType.Add:
                if (_undoData.NewerFigure is not null)
                    figures.Remove(_undoData.NewerFigure);
                break;
            case UndoData.UndoType.Remove:
                if (_undoData.OlderFigure is not null)
                    figures.Insert(_undoData.RemovedIndex, _undoData.OlderFigure);
                break;
            case UndoData.UndoType.Update:
                if (_undoData.OlderFigure is not null && _undoData.NewerFigure is not null) {
                    int index = figures.IndexOf(_undoData.NewerFigure);
                    if (index >= 0) {
                        figures.Insert(index, _undoData.OlderFigure);
                        figures.Remove(_undoData.NewerFigure);
                    } else {
                        Debug.Assert(false);
                    }
                }
                break;
        }
    }

    // 記録 1 つ分の操作をもう一度適用する
    void Redo(IList<Figure> figures, UndoData _undoData)
    {
        switch (_undoData.Type) {
            case UndoData.UndoType.Add:
                if (_undoData.NewerFigure is not null)
                    figures.Add(_undoData.NewerFigure);
                break;
            case UndoData.UndoType.Remove:
                if (_undoData.OlderFigure is not null)
                    figures.Remove(_undoData.OlderFigure);
                break;
            case UndoData.UndoType.Update:
                if (_undoData.OlderFigure is not null && _undoData.NewerFigure is not null) {
                    int index = figures.IndexOf(_undoData.OlderFigure);
                    if (index >= 0) {
                        figures.Insert(index, _undoData.NewerFigure);
                        figures.Remove(_undoData.OlderFigure);
                    } else {
                        Debug.Assert(false);
                    }
                }
                break;
        }
    }        
}