using System;
using System.Windows.Forms;

namespace CPUFreq {
    static class Program {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 fm = new Form1();
            Application.Run();
        }
    }
}
