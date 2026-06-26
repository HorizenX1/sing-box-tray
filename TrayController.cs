using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace sing_box_tray
{
    public class TrayController : IDisposable
    {
        #region Win32 API Definitions

        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 1;
        private const int WM_COMMAND = 0x0111;
        private const int WM_DESTROY = 0x0002;
        private const int WM_NULL = 0x0000;

        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_INFO = 0x00000010;

        private const int NIIF_NONE = 0x00000000;
        private const int NIIF_INFO = 0x00000001;
        private const int NIIF_WARNING = 0x00000002;
        private const int NIIF_ERROR = 0x00000003;

        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_LBUTTONUP = 0x0202;

        private const int MF_STRING = 0x00000000;
        private const int MF_SEPARATOR = 0x00000800;
        private const int MF_POPUP = 0x00000010;
        private const int MF_UNCHECKED = 0x00000000;
        private const int MF_CHECKED = 0x00000008;
        private const int MF_GRAYED = 0x00000001;
        private const int MF_ENABLED = 0x00000000;
        private const int MF_BYCOMMAND = 0x00000000;

        private const int TPM_LEFTALIGN = 0x0000;
        private const int TPM_RIGHTBUTTON = 0x0002;

        private const int ID_PROXY = 1001;
        private const int ID_TUN = 1002;
        private const int ID_STARTUP = 1003;
        private const int ID_OPEN_DIR = 1004;
        private const int ID_EXIT = 1005;
        private const int ID_DASHBOARD = 1006;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public int cbSize;
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        private static extern int CheckMenuItem(IntPtr hMenu, int uIDCheckItem, int uCheck);

        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(IntPtr hMenu, int uIDEnableItem, int uEnable);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconFromResourceEx(IntPtr pbIconBits, uint cbIconBits, bool fIcon, uint dwVer, int cxDesired, int cyDesired, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ho);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

        [DllImport("gdi32.dll")]
        private static extern bool Ellipse(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        private static extern bool Polygon(IntPtr hdc, POINT[] lpPoints, int nCount);

        [DllImport("user32.dll")]
        private static extern bool FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;
        private const int VK_SHIFT = 0x10;

        #endregion

        private readonly ConfigManager _configManager;
        private readonly ProcessSupervisor _supervisor;

        private IntPtr _hWnd;
        private static TrayController? _instance;

        // 三色 HICON 句柄
        private readonly IntPtr _hIconNormal;
        private readonly IntPtr _hIconProxy;
        private readonly IntPtr _hIconTun;

        private bool _isTunMode;
        private bool _isProxyMode;

        public TrayController(ConfigManager configManager, ProcessSupervisor supervisor)
        {
            _instance = this;
            _configManager = configManager;
            _supervisor = supervisor;

            // 1. 从 Base64 加载三种状态的图标（原版、系统代理、TUN模式）
            int iconWidth = GetSystemMetrics(SM_CXSMICON);
            int iconHeight = GetSystemMetrics(SM_CYSMICON);
            _hIconNormal = CreateIconFromBytes(Convert.FromBase64String(IconData.Normal), iconWidth, iconHeight);
            _hIconProxy = CreateIconFromBytes(Convert.FromBase64String(IconData.Proxy), iconWidth, iconHeight);
            _hIconTun = CreateIconFromBytes(Convert.FromBase64String(IconData.Tun), iconWidth, iconHeight);

            // 2. 建立隐藏接收窗口
            CreateMessageWindow();

            // 3. 注册托盘图标
            AddTrayIcon();

            _supervisor.OnStateChanged += Supervisor_OnStateChanged;

            // 自动开启系统代理
            if (_configManager.Options.SystemProxyAuto)
            {
                EnableSystemProxy(true);
            }

            // 根据默认的启动模式决定开启 TUN
            bool wantTun = string.Equals(_configManager.Options.TunStartMode, "on", StringComparison.OrdinalIgnoreCase);
            if (wantTun && CanUseTun())
            {
                StartCoreWithTun(true);
            }
            else
            {
                StartCoreWithTun(false);
            }
        }

        private unsafe void CreateMessageWindow()
        {
            var className = "sing-box-tray-msg-window";

            var wndClass = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc,
                hInstance = GetModuleHandle(null),
                lpszClassName = className
            };

            ushort classAtom = RegisterClassEx(ref wndClass);
            if (classAtom == 0)
            {
                int err = Marshal.GetLastWin32Error();
                // 1410 is ERROR_CLASS_ALREADY_EXISTS, which can happen if re-instantiated, but we shouldn't fail
                if (err != 1410)
                {
                    throw new Exception($"RegisterClassEx failed with error code: {err}");
                }
            }

            _hWnd = CreateWindowEx(
                0, className, "sing-box-tray-msg-window", 0,
                0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);

            if (_hWnd == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                throw new Exception($"CreateWindowEx failed with error code: {err}");
            }
        }

        private void AddTrayIcon()
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIconNormal,
                szTip = "sing-box-tray (运行中)"
            };

            bool success = Shell_NotifyIcon(NIM_ADD, ref nid);
            if (!success)
            {
                throw new Exception("Shell_NotifyIcon (NIM_ADD) failed.");
            }
        }

        private void UpdateTrayIcon(IntPtr hIcon, string tip)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1,
                uFlags = NIF_ICON | NIF_TIP,
                hIcon = hIcon,
                szTip = tip.Length >= 128 ? tip[..127] : tip
            };

            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }

        private bool CanUseTun()
        {
            return _configManager.SbConfig.HasTunInbound;
        }

        private bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void EnableSystemProxy(bool enable)
        {
            if (enable)
            {
                _isProxyMode = true;

                string bypassStr = _configManager.Options.SystemProxyBypass;
                if (_configManager.Options.SystemProxyBypassLocal)
                {
                    bypassStr = string.IsNullOrEmpty(bypassStr) ? "<local>" : $"<local>;{bypassStr}";
                }

                SysProxy.SetProxy(true, "127.0.0.1", _configManager.SbConfig.ProxyPort, bypassStr);
                
                if (_isTunMode)
                {
                    UpdateTrayIcon(_hIconTun, "sing-box-tray (TUN 模式)");
                }
                else
                {
                    UpdateTrayIcon(_hIconProxy, "sing-box-tray (系统代理)");
                }
            }
            else
            {
                _isProxyMode = false;
                SysProxy.SetProxy(false);
                if (_isTunMode)
                {
                    UpdateTrayIcon(_hIconTun, "sing-box-tray (TUN 模式)");
                }
                else
                {
                    UpdateTrayIcon(_hIconNormal, "sing-box-tray (运行中)");
                }
            }
        }

        private void StartCoreWithTun(bool useTun)
        {
            _isTunMode = useTun;
            if (useTun)
            {
                EnableSystemProxy(false);
                _supervisor.Start(_configManager.SbConfig.JsonWithTun);
                UpdateTrayIcon(_hIconTun, "sing-box-tray (TUN 模式激活)");
            }
            else
            {
                _supervisor.Start(_configManager.SbConfig.JsonWithoutTun);
                if (_isProxyMode)
                {
                    UpdateTrayIcon(_hIconProxy, "sing-box-tray (系统代理)");
                }
                else
                {
                    UpdateTrayIcon(_hIconNormal, "sing-box-tray (运行中)");
                }
            }
        }

        private void Supervisor_OnStateChanged(string state, string msg)
        {
            if (state == "failed" && !string.IsNullOrEmpty(msg))
            {
                ShowNotification("内核发生错误", msg, NIIF_ERROR);
            }
        }

        private void ShowNotification(string title, string text, int iconFlag)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1,
                uFlags = NIF_INFO,
                szInfo = text.Length >= 256 ? text[..255] : text,
                szInfoTitle = title.Length >= 64 ? title[..63] : title,
                dwInfoFlags = iconFlag
            };

            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }

        private void ShowPopupMenu()
        {
            IntPtr hMenu = CreatePopupMenu();

            // 1. 系统代理
            AppendMenu(hMenu, MF_STRING | (_isProxyMode ? MF_CHECKED : MF_UNCHECKED), (IntPtr)ID_PROXY, "系统代理");

            // 2. TUN 模式
            int tunFlags = MF_STRING;
            if (!CanUseTun())
            {
                tunFlags |= MF_GRAYED;
                AppendMenu(hMenu, tunFlags, (IntPtr)ID_TUN, "TUN 模式");
            }
            else
            {
                AppendMenu(hMenu, MF_STRING | (_isTunMode ? MF_CHECKED : MF_UNCHECKED), (IntPtr)ID_TUN, "TUN 模式");
            }

            // 3. 控制面板
            AppendMenu(hMenu, MF_STRING, (IntPtr)ID_DASHBOARD, "控制面板");

            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, "");

            // 4. 打开配置文件夹
            AppendMenu(hMenu, MF_STRING, (IntPtr)ID_OPEN_DIR, "打开配置文件夹");

            // 5. 开机自启动
            AppendMenu(hMenu, MF_STRING | (IsStartupEnabled() ? MF_CHECKED : MF_UNCHECKED), (IntPtr)ID_STARTUP, "开机自启动");

            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, "");

            // 6. 退出
            AppendMenu(hMenu, MF_STRING, (IntPtr)ID_EXIT, "退出");

            POINT pt;
            GetCursorPos(out pt);
            SetForegroundWindow(_hWnd);
            TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, _hWnd, IntPtr.Zero);
            PostMessage(_hWnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);
            
            // 销毁菜单释放资源
            DestroyMenu(hMenu);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private static IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (_instance != null)
                {
                    switch (message)
                    {
                        case WM_TRAYICON:
                            {
                                var mouseMsg = (int)lParam;
                                if (mouseMsg == WM_LBUTTONUP)
                                {
                                    short shiftState = GetAsyncKeyState(VK_SHIFT);
                                    bool isShiftPressed = (shiftState & 0x8000) != 0;

                                    if (isShiftPressed)
                                    {
                                        if (_instance._isTunMode)
                                        {
                                            // 切换为系统代理
                                            _instance.StartCoreWithTun(false);
                                            if (!_instance._isProxyMode)
                                            {
                                                _instance.EnableSystemProxy(true);
                                            }
                                        }
                                        else
                                        {
                                            // 切换为 TUN 模式
                                            if (!_instance.IsAdministrator())
                                            {
                                                _instance.ElevateAndEnableTun();
                                            }
                                            else
                                            {
                                                _instance.EnableSystemProxy(false);
                                                _instance.StartCoreWithTun(true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _instance.HandleMenuCommand(ID_DASHBOARD);
                                    }
                                }
                                else if (mouseMsg == WM_RBUTTONUP)
                                {
                                    _instance.ShowPopupMenu();
                                }
                                break;
                            }
                        case WM_COMMAND:
                            {
                                var id = (int)wParam;
                                _instance.HandleMenuCommand(id);
                                break;
                            }
                        case WM_DESTROY:
                            {
                                PostQuitMessage(0);
                                break;
                            }
                    }
                }
            }
            catch
            {
                // Prevent exception propagation to native code
            }
            return DefWindowProc(hWnd, message, wParam, lParam);
        }

        private void HandleMenuCommand(int id)
        {
            switch (id)
            {
                case ID_PROXY:
                    EnableSystemProxy(!_isProxyMode);
                    break;
                case ID_TUN:
                    if (!IsAdministrator() && !_isTunMode)
                    {
                        ElevateAndEnableTun();
                        return;
                    }
                    if (!_isTunMode)
                    {
                        EnableSystemProxy(false); // 启动 TUN 模式时自动关闭系统代理
                        StartCoreWithTun(true);
                    }
                    else
                    {
                        StartCoreWithTun(false);
                    }
                    break;
                case ID_DASHBOARD:
                    OpenDashboard();
                    break;
                case ID_STARTUP:
                    SetStartup(!IsStartupEnabled());
                    break;
                case ID_OPEN_DIR:
                    OpenConfigDir();
                    break;
                case ID_EXIT:
                    PostMessage(_hWnd, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
                    break;
                default:
                    break;
            }
        }

        private void ElevateAndEnableTun()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (exePath != null)
                {
                    Program.ReleaseMutex();
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "--enable-tun",
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    // Explicitly stop the child process before forcefully exiting
                    _supervisor.Stop();
                    Environment.Exit(0);
                }
            }
            catch
            {
                // User cancelled UAC prompt
                Program.AcquireMutex();
            }
        }

        private void OpenDashboard()
        {
            try
            {
                string host = "127.0.0.1";
                string port = "9090"; // default
                if (!string.IsNullOrEmpty(_configManager.SbConfig.ExternalController))
                {
                    var parts = _configManager.SbConfig.ExternalController.Split(':');
                    if (parts.Length == 2)
                    {
                        var h = parts[0].Trim();
                        if (h != "0.0.0.0" && !string.IsNullOrEmpty(h))
                        {
                            host = h;
                        }
                        port = parts[1].Trim();
                    }
                    else if (parts.Length == 1)
                    {
                        port = parts[0].Trim();
                    }
                }
                string dashboardUrl = $"http://{host}:{port}/ui/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = dashboardUrl,
                    UseShellExecute = true
                });
            }
            catch {}
        }

        private void OpenConfigDir()
        {
            try
            {
                var dir = _configManager.Options.SbConfigFile != "" ? Path.GetDirectoryName(_configManager.Options.SbConfigFile)! : AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch {}
        }

        private void SetStartup(bool run)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (run)
                    {
                        key.SetValue("sing-box-tray", $"\"{Environment.ProcessPath}\"");
                    }
                    else
                    {
                        key.DeleteValue("sing-box-tray", false);
                    }
                }
            }
            catch {}
        }

        private bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("sing-box-tray") != null;
        }

        private static uint ToColorRef(Color c)
        {
            return (uint)(c.R | (c.G << 8) | (c.B << 16));
        }

        private static IntPtr CreateIconFromBytes(byte[] icoBytes, int width, int height)
        {
            if (icoBytes.Length < 6) throw new Exception("Invalid ICO file: too small.");
            ushort count = BitConverter.ToUInt16(icoBytes, 4);
            
            int bestIndex = 0;
            int bestWidth = 0;
            
            for (int i = 0; i < count; i++)
            {
                int entryOffset = 6 + i * 16;
                if (entryOffset + 16 > icoBytes.Length) break;
                
                byte w = icoBytes[entryOffset];
                int wVal = w == 0 ? 256 : w;
                
                if (wVal == width)
                {
                    bestIndex = i;
                    break;
                }
                if (wVal > bestWidth)
                {
                    bestWidth = wVal;
                    bestIndex = i;
                }
            }
            
            int targetEntryOffset = 6 + bestIndex * 16;
            uint bytesInRes = BitConverter.ToUInt32(icoBytes, targetEntryOffset + 8);
            uint imageOffset = BitConverter.ToUInt32(icoBytes, targetEntryOffset + 12);
            
            if (imageOffset + bytesInRes > icoBytes.Length) throw new Exception("Invalid ICO file: out of bounds.");
            
            IntPtr hGlobal = Marshal.AllocHGlobal((int)bytesInRes);
            try
            {
                Marshal.Copy(icoBytes, (int)imageOffset, hGlobal, (int)bytesInRes);
                IntPtr hIcon = CreateIconFromResourceEx(hGlobal, bytesInRes, true, 0x00030000, width, height, 0);
                if (hIcon == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new Exception($"CreateIconFromResourceEx failed with error code: {err}");
                }
                return hIcon;
            }
            finally
            {
                Marshal.FreeHGlobal(hGlobal);
            }
        }

        public void Dispose()
        {
            _instance = null;

            if (_isProxyMode)
            {
                SysProxy.SetProxy(false);
            }

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);

            DestroyWindow(_hWnd);

            DestroyIcon(_hIconNormal);
            DestroyIcon(_hIconProxy);
            DestroyIcon(_hIconTun);
        }
    }
}
