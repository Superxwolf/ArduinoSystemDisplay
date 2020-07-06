using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms;
using SerialDisplay.Properties;

namespace SerialDisplay
{
    public static class PerformanceInfo
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
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

        public static Int64 GetPhysicalAvailableMemoryInMiB()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                return Convert.ToInt64((pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            }
            else
            {
                return -1;
            }

        }

        public static Int64 GetTotalMemoryInMiB()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                return Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            }
            else
            {
                return -1;
            }

        }
    }

    public class ArduinoSystemDisplayContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Thread serialRun;

        private SerialPort serial = new SerialPort("COM1", 9600);
        private string[] ports = { };
        private int curSerialRefreshRate = 250;

        private bool updateRefreshRate = true;

        protected void UpdateSystemInfo()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            decimal totalMemory = PerformanceInfo.GetTotalMemoryInMiB();

            byte[] data = { 101, 0, 0 };
            while (true)
            {
                if (!serial.IsOpen)
                {
                    UpdateContextMenu();
                    return;
                }

                if(updateRefreshRate)
                {
                    updateRefreshRate = false;
                    var intervalBytes = BitConverter.GetBytes(curSerialRefreshRate);
                    serial.Write(new byte[] { 100 }, 0, 1);
                    serial.Write(intervalBytes, 0, 4);

                }

                data[1] = (byte)cpuCounter.NextValue();

                var phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();

                decimal percentFree = ((decimal)phav / totalMemory) * 100;
                decimal percentOccupied = 100 - percentFree;

                data[2] = (byte)percentOccupied;

                try
                {
                    serial.Write(data, 0, 3);
                }
                catch
                {
                    serial.Close();
                    UpdateContextMenu();
                    return;
                }
                Thread.Sleep(curSerialRefreshRate);
            }
        }

        private void StartSerialThread(bool showError = false)
        {
            try
            {
                serial.Open();
            }
            catch
            {
                if(showError)
                    MessageBox.Show("Error opening COM port \"" + serial.PortName + "\"", "Arduino Serial Display");

                UpdateContextMenu();
                return;
            }

            updateRefreshRate = true;
            ThreadStart t = new ThreadStart(UpdateSystemInfo);
            serialRun = new Thread(t);
            serialRun.Start();
        }

        private void StopSerialThread()
        {
            if (serialRun != null && serialRun.ThreadState == System.Threading.ThreadState.Running)
                serialRun.Abort();

            if (serial.IsOpen)
                serial.Close();
        }

        private void UpdateContextMenu()
        {
            ports = SerialPort.GetPortNames();
            MenuItem[] comItems = new MenuItem[ports.Length];
            for(int i = 0; i < ports.Length; i++)
            {
                string cur_port = ports[i];
                comItems[i] = new MenuItem(cur_port, (e, a) =>
                {
                    StopSerialThread();

                    serial.PortName = cur_port;
                    StartSerialThread(true);
                });
            }

            MenuItem[] refreshItems = new MenuItem[5];
            int[] intervals = { 100, 250, 500, 750, 1000 };

            for(int i = 0; i < 5; i++)
            {
                var curInterval = intervals[i];
                refreshItems[i] = new MenuItem(curInterval.ToString(), (e, a) =>
                {
                    curSerialRefreshRate = curInterval;
                    updateRefreshRate = true;
                });
            }

            trayIcon.ContextMenu = new ContextMenu(new MenuItem[]{
                new MenuItem("Status: " + (serial.IsOpen ? "Running" : "Stopped")),
                ports.Length > 0 ? new MenuItem("COMs", comItems) : new MenuItem("No COM ports available"),
                new MenuItem("Refresh Rate", refreshItems),
                new MenuItem("Exit", Exit)
            });
        }

        public ArduinoSystemDisplayContext()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(),
                Visible = true
            };

            trayIcon.Click += (e, a) =>
            {
                UpdateContextMenu();
            };

            ports = SerialPort.GetPortNames();

            if (ports.Length == 1)
            {
                serial.PortName = ports[0];
            }

            if (ports.Length > 0)
            {
                StartSerialThread();
            }

            UpdateContextMenu();
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;

            StopSerialThread();

            Application.Exit();
        }
    }

    static class Program
    {
        //[STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new ArduinoSystemDisplayContext());
        }
    }
}
