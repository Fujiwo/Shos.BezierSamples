using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WpfCad2Lib.Core;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf
{
    public class CadData : IEnumerable<Figure>
    {
        readonly IList<Figure>   figures         = new List<Figure>();
        readonly FigureAttribute figureAttribute = new FigureAttribute();
        readonly CadUndo         undo            = new CadUndo();

        public FigureAttribute FigureAttribute
        {
            get { return figureAttribute; }
        }

        public bool CanUndo
        {
            get { return undo.CanUndo; }
        }

        public bool CanRedo
        {
            get { return undo.CanRedo; }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public int Count
        {
            get { return figures.Count; }
        }

        public IEnumerator<Figure> GetEnumerator()
        {
            return figures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

        public void Add(Figure figure)
        {
            undo.Add(figures, figure);
        }

        public void Remove(Figure figure)
        {
            undo.Remove(figures, figure);
        }
            
        public bool Update(IList<Figure> figures, Figure olderFigure, Figure newerFigure)
        {
            return undo.Update(figures, olderFigure, newerFigure);
        }

        public void RemoveAll()
        {
            for (var index = figures.Count - 1; index >= 0; index--)
                Remove(figures[index]);
        }

        public void Select(UIElementCollection elementCollection, Point position, double nearDistance)
        {
            var figure = Find(position, nearDistance);
            if (figure == null)
                SelectAll(elementCollection, false);
            else
                Select(elementCollection, figure);
        }

        void SelectAll(UIElementCollection elementCollection, bool isSelected = true)
        {
            figures.ForEach(figure => figure.Select(elementCollection, isSelected));
        }

        void Select(UIElementCollection elementCollection, Figure figure)
        {
            figures.ForEach(aFigure => aFigure.Select(elementCollection, object.ReferenceEquals(aFigure, figure)));
        }

        public IEnumerable<Figure> GetSelected(bool isSelected = true)
        {
            foreach (var figure in figures) {
                if (figure.IsSelected == isSelected)
                    yield return figure;
            }
        }

        public Figure GetSelectedOne(bool isSelected = true)
        {
            return figures.FirstOrDefault(figure => figure.IsSelected == isSelected);
        }

        public Figure Find(Point point, double nearDistance)
        {
            Figure result   	   = null;
            double minimumDistance = double.MaxValue;

            figures.Aggregate(minimumDistance,
                (minDistance, figure) => {
                    if (figure.IsNear(point, nearDistance, ref minDistance))
                        result = figure;
                    return minDistance;
                });
            return result;
        }

        public void Undo()
        {
            undo.Undo(figures);
            //SelectAll(false);
        }

        public void Redo()
        {
            undo.Redo(figures);
            //SelectAll(false);
        }

        public void Clear()
        {
            figures.Clear();
            undo.Clear();
        }

        public void StartCommand()
        {
            undo.StartCommand();
        }

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
}
