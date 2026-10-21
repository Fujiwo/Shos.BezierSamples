using System;
using System.Windows.Forms;

namespace BezierWinForm
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 視覚スタイルの有効化や高 DPI の設定など、プロジェクト設定に基づく初期化をまとめて行う。
            ApplicationConfiguration.Initialize();
            Application.Run(new BezierWinForm());
        }
    }
}
