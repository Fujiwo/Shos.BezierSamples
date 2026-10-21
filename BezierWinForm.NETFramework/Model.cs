using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace BezierWinForm
{
    /// <summary>
    /// MVC の Model。確定した図形を順に保持し、図形が追加されたら Update で通知する。
    /// </summary>
    /// <remarks>
    /// 描画は行わない。View が列挙 (foreach) して各図形を描く。
    /// デザイナーで配置できるよう Component として実装している。
    /// </remarks>
    public partial class Model : Component, IEnumerable<Figure>, IDrawableSubject
    {
        /// <summary>
        /// 図形が追加されたときに発生する。引数は追加された図形。
        /// </summary>
        public event Action<IDrawable> Update;

        // 確定した図形 (追加した順。描く順でもある)。
        List<Figure> data = new List<Figure>();

        /// <summary>
        /// Model を初期化する。
        /// </summary>
        public Model()
        { InitializeComponent(); }

        /// <summary>
        /// Model を初期化し、指定されたコンテナー (フォームの components) に登録する。
        /// </summary>
        public Model(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        /// <summary>
        /// 図形を追加し、Update で通知する。
        /// </summary>
        public void Add(Figure figure)
        {
            data.Add(figure);
            if (Update != null)
                Update(figure);
        }

        /// <summary>
        /// 保持している図形を、追加した順に列挙する。
        /// </summary>
        public IEnumerator<Figure> GetEnumerator()
        { return data.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
    }
}
