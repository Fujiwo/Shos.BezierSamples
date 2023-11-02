using WpfCad2Lib.Core;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf
{
    public class Model
    {
        readonly CadData         data            = new CadData();
        //readonly IList<Figure>   figures         = new List<Figure>();
        //readonly FigureAttribute figureAttribute = new FigureAttribute();         
        const double             defaultSize     = 1000;
        Rect                     area            = new Rect(new Point(0, 0), new Size(defaultSize, defaultSize));

        public FigureAttribute FigureAttribute
        {
            get { return data.FigureAttribute; }
        }

        public Rect Area
        {
            get { return area; }
            set { area = value; }
        }

        double NearDistance
        {
            get { return Math.Average(Area.Width, Area.Height) * Common.NearDistanceRate; }
        }

        //IList<Figure> Data
        //{
        //    get { return data; }
        //}

        public void Add(Figure item)
        {
            data.Add(item);
        }
        
        //public Figure Find(Point point)
        //{
        //    return data.Find(point, NearDistance);
        //}

        public void Select(UIElementCollection elementCollection, Point position)
        {
            data.Select(elementCollection, position, NearDistance);
        }

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
}
