using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CPUFreq {

  public partial class Form1 : Form {
    public bool barMode = true;
    public bool cpuUsage = false;
    public int interval = 1000;
    public bool showMemory = true;
    public int memCount = 5;
    public Color cpuColor = Color.Lime;
    public Color memColor = Color.Orange;
    public Color usageColor = Color.Yellow;

    private int maxFreq;
    private int count = -1;
    private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    [DllImport("PowrProf.dll")]
    public static extern UInt32 PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, UInt32 AcessFlags, UInt32 Index, ref Guid Buffer, ref UInt32 BufferSize);

    [DllImport("PowrProf.dll")]
    public static extern UInt32 PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref UInt32 BufferSize);

    public enum AccessFlags : uint {
      ACCESS_SCHEME = 16,
      ACCESS_SUBGROUP = 17,
      ACCESS_INDIVIDUAL_SETTING = 18
    }

    private static string ReadFriendlyName(Guid schemeGuid) {
      uint sizeName = 1024;
      IntPtr pSizeName = Marshal.AllocHGlobal((int)sizeName);
      string friendlyName;

      try {
        PowerReadFriendlyName(IntPtr.Zero, ref schemeGuid, IntPtr.Zero, IntPtr.Zero, pSizeName, ref sizeName);
        friendlyName = Marshal.PtrToStringUni(pSizeName);
      } finally {
        Marshal.FreeHGlobal(pSizeName);
      }

      return friendlyName;
    }

    public static IEnumerable<Guid> GetAll() {
      var schemeGuid = Guid.Empty;
      uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
      uint schemeIndex = 0;

      while (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)AccessFlags.ACCESS_SCHEME, schemeIndex, ref schemeGuid, ref sizeSchemeGuid) == 0) {
        yield return schemeGuid;
        schemeIndex++;
      }
    }

    public class ComboboxItem {
      public string Text { get; set; }
      public string Value { get; set; }

      public override string ToString() {
        return Text;
      }
    }

    private void getPowerPlans() {
      var guidPlans = GetAll();
      foreach (Guid guidPlan in guidPlans) {
        ComboboxItem item = new ComboboxItem();
        item.Text = ReadFriendlyName(guidPlan);
        item.Value = guidPlan.ToString("D");
        toolStripComboBox1.Items.Add(item);
      }
      var count = toolStripComboBox1.Items.Count;
      if (count < 2) {
        addDefultPowerPlan();
      }
    }

    private void addDefultPowerPlan() {
      ComboboxItem item = new ComboboxItem();
      item.Text = "Balanced";
      item.Value = "381b4222-f694-41f0-9685-ff5bb260df2e";
      toolStripComboBox1.Items.Add(item);

      ComboboxItem item2 = new ComboboxItem();
      item2.Text = "High performance";
      item2.Value = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
      toolStripComboBox1.Items.Add(item2);

      ComboboxItem item3 = new ComboboxItem();
      item3.Text = "Power saver";
      item3.Value = "a1841308-3541-4fab-bc81-f71556f20b4a";
      toolStripComboBox1.Items.Add(item3);
    }

    private void timer1_Tick(object sender, EventArgs e) {
      getInfo();
    }

    private void CreateTextIcon(string str, Color color) {
      SolidBrush brush = new SolidBrush(color);
      Font fontToUse = new Font("MS Gothic", 14, FontStyle.Bold, GraphicsUnit.Pixel);
      Bitmap bitmap = new Bitmap(16, 16);
      Graphics g = Graphics.FromImage(bitmap);
      g.FillRectangle(Brushes.Black, g.VisibleClipBounds);
      g.DrawString(str, fontToUse, brush, -1, 1);

      IntPtr Hicon = bitmap.GetHicon();
      Icon newIcon = Icon.FromHandle(Hicon);
      notifyIcon1.Icon = newIcon;

      brush.Dispose();
      fontToUse.Dispose();

      bitmap.Dispose();
      g.Dispose();
      newIcon.Dispose();
      DestroyIcon(newIcon.Handle);
    }

    private void CreateLineIcon(int cpu, int usage, int mem) {
      Bitmap bitmap = new Bitmap(16, 16);
      Graphics g = Graphics.FromImage(bitmap);
      g.FillRectangle(Brushes.Black, g.VisibleClipBounds);

      Pen pen1 = new Pen(cpuColor, 2);
      g.DrawLine(pen1, 3, 16, 3, 16 - (int)(Math.Ceiling(cpu * 0.16)));

      Pen pen2 = new Pen(usageColor, 2);
      g.DrawLine(pen2, 8, 16, 8, 16 - (int)(Math.Ceiling(usage * 0.16)));

      Pen pen3 = new Pen(memColor, 2);
      g.DrawLine(pen3, 13, 16, 13, 16 - (int)(Math.Ceiling(mem * 0.16)));

      IntPtr Hicon = bitmap.GetHicon();
      Icon newIcon = Icon.FromHandle(Hicon);
      notifyIcon1.Icon = newIcon;

      pen1.Dispose();
      pen2.Dispose();
      pen3.Dispose();

      bitmap.Dispose();
      g.Dispose();
      newIcon.Dispose();
      DestroyIcon(newIcon.Handle);
    }

    private void getInfo() {
      count++;

      if (count == 0) {
        changeInteval();
      } else if (barMode) {
        int frq = getCPUFrequency();
        int usage = getCPUUsage();
        int mem = getMemoryInfo();
        CreateLineIcon(frq * 100 / maxFreq, usage, mem);
      } else if (showMemory && count % memCount == 0) {
        count = 0;
        int mem = getMemoryInfo();
        CreateTextIcon(mem.ToString("D2"), memColor);
      } else if (cpuUsage) {
        int usage = getCPUUsage();
        CreateTextIcon(usage.ToString("D2"), usageColor);
      } else {
        int frq = getCPUFrequency();
        frq = frq / 100;
        CreateTextIcon(frq.ToString("D2"), cpuColor);
      }
    }

    private void getOptions() {
      barMode = Properties.Settings.Default._barMode;
      cpuUsage = Properties.Settings.Default._cpuUsage;
      interval = Properties.Settings.Default._interval;
      showMemory = Properties.Settings.Default._showMemory;
      memCount = Properties.Settings.Default._memCount;
      cpuColor = Properties.Settings.Default._cpuColor;
      memColor = Properties.Settings.Default._memColor;
      usageColor = Properties.Settings.Default._usageColor;
    }

    private void setOptions() {
      Properties.Settings.Default._barMode = barMode;
      Properties.Settings.Default._cpuUsage = cpuUsage;
      Properties.Settings.Default._interval = interval;
      Properties.Settings.Default._showMemory = showMemory;
      Properties.Settings.Default._memCount = memCount;
      Properties.Settings.Default._cpuColor = cpuColor;
      Properties.Settings.Default._memColor = memColor;
      Properties.Settings.Default._usageColor = usageColor;
      Properties.Settings.Default.Save();
    }

    private void setFormOptions() {
      if (barMode) {
        comboBox1.SelectedIndex = 2;
      } else if (cpuUsage) {
        comboBox1.SelectedIndex = 1;
      } else {
        comboBox1.SelectedIndex = 0;
      }
      numericUpDown1.Value = interval;
      checkBox1.Checked = showMemory;
      numericUpDown2.Value = memCount;
      label5.BackColor = cpuColor;
      label6.BackColor = memColor;
      label9.BackColor = usageColor;
    }

    private void Form1_Load(object sender, EventArgs e) {
      setFormOptions();
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
      if (e.CloseReason == CloseReason.UserClosing) {
        e.Cancel = true;
        Hide();
      }
    }

    public Form1() {
      this.ShowInTaskbar = false;
      getOptions();
      maxFreq = getMaxFreq();
      InitializeComponent();
      getPowerPlans();
    }

    private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e) {
      ComboboxItem selectedCar = (ComboboxItem)toolStripComboBox1.SelectedItem;
      Console.WriteLine(selectedCar.Value);
      if (selectedCar != null) changePowerPlan("/c powercfg -setactive " + selectedCar.Value);
    }

    private void changePowerPlan(string mode) {
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.WindowStyle = ProcessWindowStyle.Hidden;
      startInfo.FileName = "cmd.exe";
      startInfo.Arguments = mode;
      process.StartInfo = startInfo;
      process.Start();
    }

    private void changeInteval() {
      timer1.Stop();
      timer1.Interval = interval;
      timer1.Start();
    }

    private int getMemoryInfo() {
      Int64 phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();
      Int64 tot = PerformanceInfo.GetTotalMemoryInMiB();
      decimal percentFree = ((decimal)phav / (decimal)tot) * 100;
      decimal percentOccupied = 100 - percentFree;
      return (int)(percentOccupied);
    }

    private int getCPUUsage() {
      return (int)(cpuCounter.NextValue());
    }

    private int getCPUFrequency() {
      Double cpuPerf = 0;
      ManagementObjectSearcher objSearchPerf = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM  Win32_PerfFormattedData_Counters_ProcessorInformation");
      ManagementObjectCollection colPerf = objSearchPerf.Get();
      foreach (ManagementObject objPerf in colPerf) {
        cpuPerf += Double.Parse(objPerf["PercentProcessorPerformance"].ToString());
      }
      Double tmp = (maxFreq * (cpuPerf / (100 * colPerf.Count)));
      return (int)(Math.Ceiling(tmp));
    }

    private int getMaxFreq() {
      ManagementObjectSearcher objSearchCPU = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM  Win32_Processor");
      ManagementObjectCollection.ManagementObjectEnumerator cpuEnum = objSearchCPU.Get().GetEnumerator();
      cpuEnum.MoveNext();
      return System.Int32.Parse(cpuEnum.Current["MaxClockSpeed"].ToString());
    }

    private void NotifyIcon1_DoubleClick() {
      this.Show();
    }

    private void label5_Click(object sender, EventArgs e) {
      DialogResult dr = colorDialog1.ShowDialog();
      if (dr == System.Windows.Forms.DialogResult.OK) {
        cpuColor = colorDialog1.Color;
        label5.BackColor = colorDialog1.Color;
        setOptions();
      }
    }

    private void label6_Click(object sender, EventArgs e) {
      DialogResult dr = colorDialog1.ShowDialog();
      if (dr == System.Windows.Forms.DialogResult.OK) {
        memColor = colorDialog1.Color;
        label6.BackColor = colorDialog1.Color;
        setOptions();
      }
    }

    private void label9_Click(object sender, EventArgs e) {
      DialogResult dr = colorDialog1.ShowDialog();
      if (dr == System.Windows.Forms.DialogResult.OK) {
        usageColor = colorDialog1.Color;
        label9.BackColor = colorDialog1.Color;
        setOptions();
      }
    }

    private void checkBox1_CheckedChanged(object sender, EventArgs e) {
      if (checkBox1.Checked) {
        groupBox1.Show();
        showMemory = true;
      } else {
        groupBox1.Hide();
        showMemory = false;
      }
      setOptions();
    }

    private void numericUpDown1_ValueChanged(object sender, EventArgs e) {
      interval = Decimal.ToInt32(numericUpDown1.Value);
      setOptions();
      changeInteval();
    }

    private void numericUpDown2_ValueChanged(object sender, EventArgs e) {
      memCount = Decimal.ToInt32(numericUpDown2.Value);
      count = 0;
      setOptions();
    }

    private void button1_Click(object sender, EventArgs e) {
      RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
      rkApp.SetValue("CPUFreq_ando", Application.ExecutablePath);
    }

    private void button2_Click(object sender, EventArgs e) {
      RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
      rkApp.DeleteValue("CPUFreq_ando", false);
    }

    private void comboBox1_SelectedValueChanged(object sender, EventArgs e) {
      if (comboBox1.SelectedIndex == 0) {
        cpuUsage = false;
        barMode = false;
        checkBox1.Show();
        groupBox1.Show();
      } else if (comboBox1.SelectedIndex == 2) {
        cpuUsage = false;
        barMode = true;
        checkBox1.Hide();
        groupBox1.Hide();
      } else {
        cpuUsage = true;
        barMode = false;
        checkBox1.Show();
        groupBox1.Show();
      }
      setOptions();
    }

    private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e) {
      NotifyIcon1_DoubleClick();
    }

    private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
      this.Show();
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
      notifyIcon1.Visible = false;
      Application.Exit();
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