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
            // コントロールを OS の視覚スタイルで描き、テキストは GDI (TextRenderer) で描く既定の設定にする。
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BezierWinForm());
        }
    }
}
