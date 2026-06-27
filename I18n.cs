using System.Runtime.InteropServices;

namespace sing_box_tray
{
    public static class I18n
    {
        [DllImport("kernel32.dll")]
        private static extern ushort GetUserDefaultUILanguage();

        private static bool IsZh() => (GetUserDefaultUILanguage() & 0x00FF) == 0x04;

        // Tray Tips
        public static string TrayRunning => IsZh() ? "sing-box-tray (运行中)" : "sing-box-tray (Running)";
        public static string TrayTunMode => IsZh() ? "sing-box-tray (TUN 模式)" : "sing-box-tray (TUN Mode)";
        public static string TrayProxyMode => IsZh() ? "sing-box-tray (系统代理)" : "sing-box-tray (System Proxy)";
        
        // Menu Items
        public static string MenuSystemProxy => IsZh() ? "系统代理" : "System Proxy";
        public static string MenuTunMode => IsZh() ? "TUN 模式" : "TUN Mode";
        public static string MenuDashboard => IsZh() ? "控制面板" : "Dashboard";
        public static string MenuOpenConfigDir => IsZh() ? "打开配置文件夹" : "Open Config Directory";
        public static string MenuReloadConfig => IsZh() ? "重载配置" : "Reload Config";
        public static string MenuStartup => IsZh() ? "开机自启动" : "Run at Startup";
        public static string MenuExit => IsZh() ? "退出" : "Exit";

        // Notifications
        public static string MsgKernelErrorTitle => IsZh() ? "内核发生错误" : "Kernel Error";
        public static string MsgReloadSuccessTitle => IsZh() ? "重载配置成功" : "Reload Successful";
        public static string MsgReloadSuccess => IsZh() ? "已重新加载配置并重启内核。" : "Configuration reloaded and kernel restarted.";
        public static string MsgReloadFailedTitle => IsZh() ? "重载配置失败" : "Reload Failed";
        
        // Dialogs
        public static string MsgAlreadyRunning => IsZh() ? "sing-box-tray 已经在运行中！" : "sing-box-tray is already running!";
        public static string MsgNoticeTitle => IsZh() ? "提示" : "Notice";
        public static string MsgErrorTitle => IsZh() ? "错误" : "Error";
        public static string MsgFatalErrorTitle => IsZh() ? "致命错误" : "Fatal Error";
        public static string MsgKernelNotFoundTitle => IsZh() ? "未发现内核" : "Kernel Not Found";
        public static string MsgKernelNotFound(string dir) => IsZh() 
            ? $"未在 [{dir}] 目录中发现内核文件 sing-box.exe，请将 sing-box 内核放入该目录后重试。" 
            : $"Kernel file sing-box.exe not found in [{dir}]. Please place it there and try again.";
        public static string MsgCreateDataDirFailed(string msg) => IsZh()
            ? $"创建 data 目录失败: {msg}"
            : $"Failed to create data directory: {msg}";
        public static string MsgLoadConfigFailed(string msg) => IsZh()
            ? $"加载配置文件失败: {msg}"
            : $"Failed to load config: {msg}";
        public static string MsgUnhandledException(string msg) => IsZh()
            ? $"程序发生未处理异常: {msg}"
            : $"Unhandled exception: {msg}";

        // Process Supervisor
        public static string ExeNotFound(string path) => IsZh() ? $"未找到 sing-box 核心文件: {path}" : $"sing-box executable not found: {path}";
        public static string FailedToStart(string msg) => IsZh() ? $"启动 sing-box 失败: {msg}" : $"Failed to start sing-box: {msg}";
        public static string ExitedUnexpectedly => IsZh() ? "sing-box 异常退出。正在重启..." : "sing-box exited unexpectedly. Restarting...";
        public static string LogExitedUnexpectedly => IsZh() ? "sing-box 进程异常退出。3秒后重启..." : "sing-box process exited unexpectedly. Restarting in 3 seconds...";
    }
}
