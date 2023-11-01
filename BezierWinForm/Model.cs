﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace BezierWinForm
{
    public partial class Model : Component, IEnumerable<Figure>, IDrawableSubject
    {
        public event Action<IDrawable> Update;

        List<Figure> data = new List<Figure>();

        public Model()
        { InitializeComponent(); }

        public Model(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        public void Add(Figure figure)
        {
            data.Add(figure);
            if (Update != null)
                Update(figure);
        }

        public IEnumerator<Figure> GetEnumerator()
        { return data.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
    }
}
