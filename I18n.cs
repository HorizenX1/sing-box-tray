using System.Runtime.InteropServices;

namespace sing_box_tray
{
    public static class I18n
    {
        [DllImport("kernel32.dll")]
        private static extern ushort GetUserDefaultUILanguage();

        private static readonly bool _isZh;

        // Tray Tips
        public static string TrayRunning { get; }
        public static string TrayTunMode { get; }
        public static string TrayProxyMode { get; }
        
        // Menu Items
        public static string MenuSystemProxy { get; }
        public static string MenuTunMode { get; }
        public static string MenuDashboard { get; }
        public static string MenuOpenConfigDir { get; }
        public static string MenuReloadConfig { get; }
        public static string MenuStartup { get; }
        public static string MenuExit { get; }

        // Notifications
        public static string MsgKernelErrorTitle { get; }
        public static string MsgReloadSuccessTitle { get; }
        public static string MsgReloadSuccess { get; }
        public static string MsgReloadFailedTitle { get; }
        
        // Dialogs
        public static string MsgAlreadyRunning { get; }
        public static string MsgNoticeTitle { get; }
        public static string MsgErrorTitle { get; }
        public static string MsgFatalErrorTitle { get; }
        public static string MsgKernelNotFoundTitle { get; }
        
        // Process Supervisor
        public static string ExitedUnexpectedly { get; }
        public static string LogExitedUnexpectedly { get; }

        static I18n()
        {
            _isZh = (GetUserDefaultUILanguage() & 0x00FF) == 0x04;

            if (_isZh)
            {
                TrayRunning = "sing-box-tray (运行中)";
                TrayTunMode = "sing-box-tray (TUN 模式)";
                TrayProxyMode = "sing-box-tray (系统代理)";
                
                MenuSystemProxy = "系统代理";
                MenuTunMode = "TUN 模式";
                MenuDashboard = "控制面板";
                MenuOpenConfigDir = "打开配置文件夹";
                MenuReloadConfig = "重载配置";
                MenuStartup = "开机自启动";
                MenuExit = "退出";

                MsgKernelErrorTitle = "内核发生错误";
                MsgReloadSuccessTitle = "重载配置成功";
                MsgReloadSuccess = "已重新加载配置并重启内核。";
                MsgReloadFailedTitle = "重载配置失败";
                
                MsgAlreadyRunning = "sing-box-tray 已经在运行中！";
                MsgNoticeTitle = "提示";
                MsgErrorTitle = "错误";
                MsgFatalErrorTitle = "致命错误";
                MsgKernelNotFoundTitle = "未发现内核";
                
                ExitedUnexpectedly = "sing-box 异常退出。正在重启...";
                LogExitedUnexpectedly = "sing-box 进程异常退出。3秒后重启...";
            }
            else
            {
                TrayRunning = "sing-box-tray (Running)";
                TrayTunMode = "sing-box-tray (TUN Mode)";
                TrayProxyMode = "sing-box-tray (System Proxy)";
                
                MenuSystemProxy = "System Proxy";
                MenuTunMode = "TUN Mode";
                MenuDashboard = "Dashboard";
                MenuOpenConfigDir = "Open Config Directory";
                MenuReloadConfig = "Reload Config";
                MenuStartup = "Run at Startup";
                MenuExit = "Exit";

                MsgKernelErrorTitle = "Kernel Error";
                MsgReloadSuccessTitle = "Reload Successful";
                MsgReloadSuccess = "Configuration reloaded and kernel restarted.";
                MsgReloadFailedTitle = "Reload Failed";
                
                MsgAlreadyRunning = "sing-box-tray is already running!";
                MsgNoticeTitle = "Notice";
                MsgErrorTitle = "Error";
                MsgFatalErrorTitle = "Fatal Error";
                MsgKernelNotFoundTitle = "Kernel Not Found";
                
                ExitedUnexpectedly = "sing-box exited unexpectedly. Restarting...";
                LogExitedUnexpectedly = "sing-box process exited unexpectedly. Restarting in 3 seconds...";
            }
        }

        // Methods for dynamic strings
        public static string MsgKernelNotFound(string dir) => _isZh 
            ? $"未在 [{dir}] 目录中发现内核文件 sing-box.exe，请将 sing-box 内核放入该目录后重试。" 
            : $"Kernel file sing-box.exe not found in [{dir}]. Please place it there and try again.";
        public static string MsgCreateDataDirFailed(string msg) => _isZh
            ? $"创建 data 目录失败: {msg}"
            : $"Failed to create data directory: {msg}";
        public static string MsgLoadConfigFailed(string msg) => _isZh
            ? $"加载配置文件失败: {msg}"
            : $"Failed to load config: {msg}";
        public static string MsgUnhandledException(string msg) => _isZh
            ? $"程序发生未处理异常: {msg}"
            : $"Unhandled exception: {msg}";
        public static string ExeNotFound(string path) => _isZh 
            ? $"未找到 sing-box 核心文件: {path}" 
            : $"sing-box executable not found: {path}";
        public static string FailedToStart(string msg) => _isZh 
            ? $"启动 sing-box 失败: {msg}" 
            : $"Failed to start sing-box: {msg}";
    }
}
