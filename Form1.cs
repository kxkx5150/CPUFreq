using System;
using System.Drawing;
using System.Management;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CPUFreq {
    public partial class Form1 : Form {
        public int interval = 1000;
        public bool showMemory = true;
        public int memCount = 5;
        public Color cpuColor = Color.Lime;
        public Color memColor = Color.Orange;

        private int maxFreq;
        private int count = -1;
        private NotifyIcon notifyIcon1 = new NotifyIcon();

        private void getOptions() {
            interval = 1000;
            showMemory = true;
            memCount = 5;
            cpuColor = Color.Lime;
            memColor = Color.DeepPink;




        }
        public Form1() {
            this.ShowInTaskbar = false;
            getOptions();
            maxFreq = getMaxFreq();
            setComponents();
            InitializeComponent();
        }

        private void setComponents() {
            notifyIcon1.Visible = true;
            notifyIcon1.Text = "";
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem menuItem2 = new ToolStripMenuItem();
            menuItem2.Text = "&Settings";
            menuItem2.Click += new EventHandler(openSettings);
            menu.Items.Add(menuItem2);

            ToolStripMenuItem menuItem = new ToolStripMenuItem();
            menuItem.Text = "&Exit";
            menuItem.Click += new EventHandler(Close_Click);
            menu.Items.Add(menuItem);

            notifyIcon1.ContextMenuStrip = menu;
            notifyIcon1.DoubleClick += new EventHandler(NotifyIcon1_DoubleClick);
        }

        private void getInfo() {
            count++;
            if (count == 0) {
                timer1.Stop();
                timer1.Interval = interval;
                timer1.Start();
            } else if (showMemory && count % memCount == 0) {
                count = 0;
                int mem = getMemoryInfo();
                CreateTextIcon(mem.ToString("D2"), memColor);
            } else {
                int tval = timer1.Interval;
                int frq = getCPUFrequency();
                CreateTextIcon(frq.ToString("D2"), cpuColor);
            }
        }
        public void CreateTextIcon(string str, Color color) {
            Font fontToUse = new Font("MS Gothic", 13, FontStyle.Bold, GraphicsUnit.Pixel);
            Brush brushToUse = new SolidBrush(color);
            Bitmap bitmapText = new Bitmap(16, 16);
            Graphics g = System.Drawing.Graphics.FromImage(bitmapText);

            g.Clear(Color.Black);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            g.DrawString(str, fontToUse, brushToUse, -1, 2);
            IntPtr hIcon = (bitmapText.GetHicon());
            notifyIcon1.Icon = System.Drawing.Icon.FromHandle(hIcon);

            g.Dispose();
            bitmapText.Dispose();
            brushToUse.Dispose();
            fontToUse.Dispose();
        }

        private int getMemoryInfo() {
            Int64 phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();
            Int64 tot = PerformanceInfo.GetTotalMemoryInMiB();
            decimal percentFree = ((decimal)phav / (decimal)tot) * 100;
            decimal percentOccupied = 100 - percentFree;
            return Convert.ToInt32(percentOccupied);
        }

        private int getCPUFrequency() {
            Double cpuPerf = 0;
            ManagementObjectSearcher objSearchPerf = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM  Win32_PerfFormattedData_Counters_ProcessorInformation");
            ManagementObjectCollection colPerf = objSearchPerf.Get();
            foreach (ManagementObject objPerf in colPerf) {
                cpuPerf += Double.Parse(objPerf["PercentProcessorPerformance"].ToString());
            }
            Double tmp = (maxFreq * (cpuPerf / (10000 * colPerf.Count)));
            return Convert.ToInt32(Math.Ceiling(tmp));
        }
        private int getMaxFreq() {
            ManagementObjectSearcher objSearchCPU = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM  Win32_Processor");
            ManagementObjectCollection.ManagementObjectEnumerator cpuEnum = objSearchCPU.Get().GetEnumerator();
            cpuEnum.MoveNext();
            return System.Int32.Parse(cpuEnum.Current["MaxClockSpeed"].ToString());
        }
        private void timer1_Tick(object sender, EventArgs e) {
            getInfo();
        }
        private void Close_Click(object sender, EventArgs e) {
            notifyIcon1.Visible = false;
            Application.Exit();
        }
        private void openSettings(object sender, EventArgs e) {
            this.Show();
        }
        private void NotifyIcon1_DoubleClick(object sender, EventArgs e) {
            this.Show();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                Hide();
            }
        }
    }
    public static class PerformanceInfo {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public static Int64 GetPhysicalAvailableMemoryInMiB() {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi))) {
                return Convert.ToInt64((pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            } else {
                return -1;
            }

        }

        public static Int64 GetTotalMemoryInMiB() {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi))) {
                return Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            } else {
                return -1;
            }

        }
    }

}
