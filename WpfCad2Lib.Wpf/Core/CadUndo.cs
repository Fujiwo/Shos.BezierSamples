using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WpfCad2Lib.Wpf
{
    class CadUndo
    {
        class UndoData
        {
            public enum UndoType
            {
                Add, Remove, Update
            }

            public UndoType Type         { get; private set; }
            public bool     IsStart      { get;         set; }
            public Figure?  OlderFigure  { get; private set; }
            public Figure?  NewerFigure  { get; private set; }

            UndoData(UndoType type, Figure? olderFigure, Figure? newerFigure)
            {
                //if (olderFigure is not null)
                //    olderFigure.IsSelected = false;
                Type        = type;
                OlderFigure = olderFigure;
                NewerFigure = newerFigure;
            }

            public static UndoData AddData(Figure figure)
            {
                return new UndoData(UndoType.Add, null, figure);
            }

            public static UndoData RemoveData(Figure figure)
            {
                return new UndoData(UndoType.Remove, figure, null);
            }

            public static UndoData UpdateData(Figure olderFigure, Figure newerFigure)
            {
                return new UndoData(UndoType.Update, olderFigure, newerFigure);
            }
        }

        class UndoBuffer
        {
            const int           bufferMaxSize = 1000;
            readonly UndoData[] data          = new UndoData[bufferMaxSize];

            int  top = 0;
            int  current = 0;
            int  bottom = 0;
            bool isStart = true;

            public void Start()
            {
                isStart = true;
            }

            public void Push(UndoData undoData)
            {
                undoData.IsStart = isStart;
                data[current] = undoData;
                current = NextOf(current);
                bottom = current;
                if (current == top)
                    top = NextOf(top);
                if (isStart)
                    isStart = false;
            }

            public bool CanPop
            {
                get { return current != top; }
            }

            public bool CanForward
            {
                get { return current != bottom; }
            }

            public IEnumerable<UndoData> Undo()
            {
                for (var undoData = Pop(); undoData is not null; undoData = Pop()) {
                    yield return undoData;
                    if (undoData.IsStart)
                        break;
                }
            }

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

            UndoData? Pop()
            {
                if (!CanPop)
                    return null;
                current = PreviousOf(current);
                return data[current];
            }

            UndoData? Forward()
            {
                if (!CanForward)
                    return null;
                var result = data[current];
                current = NextOf(current);
                return result;
            }

            static int PreviousOf(int pointer)
            {
                return (pointer + bufferMaxSize - 1) % bufferMaxSize;
            }

            static int NextOf(int pointer)
            {
                return (pointer + 1) % bufferMaxSize;
            }
        }

        readonly UndoBuffer undoBuffer = new UndoBuffer();

        public bool CanUndo
        {
            get { return undoBuffer.CanPop; }
        }

        public bool CanRedo
        {
            get { return undoBuffer.CanForward; }
        }

        public void StartCommand()
        {
            undoBuffer.Start();
        }

        public void Add(IList<Figure> figures, Figure figure)
        {
            figures.Add(figure);
            undoBuffer.Push(UndoData.AddData(figure));
        }

        public void Remove(IList<Figure> figures, Figure figure)
        {
            figures.Remove(figure);
            undoBuffer.Push(UndoData.RemoveData(figure));
        }

        public bool Update(IList<Figure> figures, Figure olderFigure, Figure newerFigure)
        {
            int index = figures.IndexOf(olderFigure);
            if (index < 0)
                return false;
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

        public void Clear()
        {
            undoBuffer.Clear();
        }

        void Undo(IList<Figure> figures, UndoData _undoData)
        {
            switch (_undoData.Type) {
                case UndoData.UndoType.Add:
                    if (_undoData.NewerFigure is not null)
                        figures.Remove(_undoData.NewerFigure);
                    break;
                case UndoData.UndoType.Remove:
                    if (_undoData.OlderFigure is not null)
                        figures.Add(_undoData.OlderFigure);
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
}