using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace sing_box_tray
{
    internal static class Program
    {
        #region Win32 API Definitions

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
            public uint lPrivate;
        }

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        private const uint MB_OK = 0x00000000;
        private const uint MB_ICONERROR = 0x00000010;
        private const uint MB_ICONWARNING = 0x00000030;
        private const uint MB_ICONINFORMATION = 0x00000040;

        #endregion

        private static Mutex? _mutex;
        private const string AppGuid = "Global\\sing-box-tray-single-instance-guid";

        public static bool AcquireMutex()
        {
            if (_mutex != null) return true;
            _mutex = new Mutex(true, AppGuid, out bool isNewInstance);
            return isNewInstance;
        }

        public static void ReleaseMutex()
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
                _mutex = null;
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                // 0. 开启高 DPI 适配，防止字体模糊和菜单变形
                EnableDpiAwareness();

                // 1. 单实例防护，防止多次启动
                if (!AcquireMutex())
                {
                    MessageBox(IntPtr.Zero, I18n.MsgAlreadyRunning, I18n.MsgNoticeTitle, MB_OK | MB_ICONINFORMATION);
                    return;
                }

                // 2. 确定与创建 data 目录
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataDir = Path.Combine(exeDir, "data");
                if (!Directory.Exists(dataDir))
                {
                    try
                    {
                        Directory.CreateDirectory(dataDir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox(IntPtr.Zero, I18n.MsgCreateDataDirFailed(ex.Message), I18n.MsgErrorTitle, MB_OK | MB_ICONERROR);
                        return;
                    }
                }

                // 3. 读取和解析配置
                ConfigManager configManager;
                try
                {
                    configManager = new ConfigManager(dataDir);
                    // 解析启动参数
                    foreach (var arg in args)
                    {
                        if (arg.Equals("--enable-tun", StringComparison.OrdinalIgnoreCase))
                        {
                            configManager.Options.TunStartMode = "on";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox(IntPtr.Zero, I18n.MsgLoadConfigFailed(ex.Message), I18n.MsgErrorTitle, MB_OK | MB_ICONERROR);
                    return;
                }

                // 4. 检查 sing-box.exe 是否在指定目录（默认在 data 目录下）
                string sbPath = Path.Combine(configManager.Options.SbDir, "sing-box.exe");
                if (!File.Exists(sbPath))
                {
                    MessageBox(IntPtr.Zero, I18n.MsgKernelNotFound(configManager.Options.SbDir), I18n.MsgKernelNotFoundTitle, MB_OK | MB_ICONWARNING);
                    return;
                }

                // 5. 启动守护进程与托盘
                using var supervisor = new ProcessSupervisor(sbPath, configManager.Options.LogFile);
                using var tray = new TrayController(configManager, supervisor);

                // 6. 原生 Win32 消息循环
                MSG msg;
                while (GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                // 消息循环退出时，垃圾回收会自动释放 supervisor 和 tray，执行各自 of Dispose()
            }
            catch (Exception ex)
            {
                MessageBox(IntPtr.Zero, I18n.MsgUnhandledException(ex.ToString()), I18n.MsgFatalErrorTitle, MB_OK | MB_ICONERROR);
            }
        }

        private static void EnableDpiAwareness()
        {
            try
            {
                // Windows 10 Creators Update (1703) 及以上：Per-Monitor V2 DPI Awareness
                if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    SetProcessDPIAware();
                }
            }
            catch
            {
                try
                {
                    SetProcessDPIAware();
                }
                catch {}
            }
        }
    }
}
