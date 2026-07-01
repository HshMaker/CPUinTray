using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Timers;

namespace CPUInTray
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            CalculateValue.Initialize();
            ApplicationConfiguration.Initialize();
            Application.Run(new OverlayForm());
        }       
    }

    public class OverlayForm : Form
    {

        private System.Windows.Forms.Timer timer;

        private float cpu;
        private float ram;

        public OverlayForm()
        {
            
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;

            this.Width = 200;
            this.Height = 50;
            this.DoubleBuffered = true;
            PositionToTaskbar();

            this.Shown += (s, e) => PositionToTaskbar();
            timer = new System.Windows.Forms.Timer
            {
                Interval = 10
            };

            timer.Tick += (s, e) =>
            {
                cpu = CalculateValue.cpu;
                ram = CalculateValue.ram;
                
                if (IsFullscreen())
                {
                    this.Visible = false;   // 완전히 숨김
                }
                else
                {
                    this.Visible = true;

                    // 다시 최상단 올림
                    SetWindowPos(
                        this.Handle,
                        HWND_TOPMOST,
                        0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW
                    );
                }


                Invalidate();
            };

            timer.Start();
        }
        
        private void PositionToTaskbar()
        {
            
            var screen = Screen.PrimaryScreen!.Bounds; // ✅ 전체 화면

            this.Location = new Point(
                screen.Width - this.Width - (screen.Width/5),
                screen.Height - this.Height - 25 // 아래 살짝 띄우기
            );

        }

        
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string text = $"CPU {cpu:0.00}%\nRAM {ram:0.00}%";

            Brush cpuBrush = cpu > 70 ? Brushes.Red : Brushes.White;
            Brush ramBrush = ram > 90 ? Brushes.Red : Brushes.White;

            Font font = new Font("Segoe UI", 9, FontStyle.Bold);

            g.DrawString("CPU", font, cpuBrush, 0, 0);
            g.DrawString("RAM", font, ramBrush, 0, 23);
            g.DrawString($"{cpu:0.00}%", font, cpuBrush, 100, 0);
            g.DrawString($"{ram:0.00}%", font, ramBrush, 100, 23);
        }
        
        
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT

                cp.ExStyle |= 0x80; // TOOLWINDOW
                return cp;
            }
        }

        
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_SHOWWINDOW = 0x0040;


        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        bool IsFullscreen()
        {
            var hwnd = GetForegroundWindow();
            GetWindowRect(hwnd, out RECT rect);

            var screen = Screen.PrimaryScreen!.Bounds;

            return rect.Left == screen.Left &&
                rect.Top == screen.Top &&
                rect.Right == screen.Right &&
                rect.Bottom == screen.Bottom;
        }


        protected override bool ShowWithoutActivation => true;

        
    }


    class CalculateValue
    {
        private static readonly string cpuCategoryName = "Processor Information";
        private static readonly string cpuCounterName = "% Processor Utility";
        private static readonly string cpuInstanceName = "_Total";
        private static double totalRam = 0.0f;

        private static ManagementClass? ramClass = null;
        private static PerformanceCounter? cpuCounter = null;
        private static PerformanceCounter? ramCounter = null;
        public static float cpu;
        public static float ram;

        public static void Initialize()
        {
            cpuCounter = new PerformanceCounter(cpuCategoryName, cpuCounterName, cpuInstanceName);
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            ramClass = new ManagementClass("Win32_OperatingSystem");

            totalRam = GetTotalRam();

            System.Threading.Timer timer = new System.Threading.Timer(UpdateRates, null, 100, 2000);

            void UpdateRates(object? state)
            {
                cpu = GetCPURate();
                ram = GetRamRate();
            }

        }

        public static float GetCPURate()
        {
            if (cpuCounter == null) return 0.0f;

            float cpuPercent = cpuCounter.NextValue();

            return cpuPercent;
        }

        public static float GetRamRate()
        {
            if (ramCounter == null) return 0.0f;

            float ramPercent = ramCounter.NextValue();

            return (float)((1 - (ramPercent/(totalRam/1024)) ) * 100);
        }

        public static double GetTotalRam()
        {
            if (ramClass == null) return 0.0f;
            ManagementObjectCollection instances = ramClass.GetInstances();
            
            foreach (ManagementObject info in instances)
            {
                double totalRam = double.Parse(info["TotalVisibleMemorySize"].ToString()!);
                return totalRam;
            }

            return 0;
        }
    }
}
