using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace sing_box_tray
{
    public static class SysProxy
    {
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;
        private const int INTERNET_PER_CONN_FLAGS = 1;
        private const int INTERNET_PER_CONN_PROXY_SERVER = 2;
        private const int INTERNET_PER_CONN_PROXY_BYPASS = 3;

        private const int PROXY_TYPE_DIRECT = 0x00000001;
        private const int PROXY_TYPE_PROXY = 0x00000002;
        private const int INTERNET_OPTION_PER_CONNECTION_OPTION = 75;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct INTERNET_PER_CONN_OPTION
        {
            public int dwOption;
            public INTERNET_PER_CONN_OPTION_VALUE Value;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        private struct INTERNET_PER_CONN_OPTION_VALUE
        {
            [FieldOffset(0)]
            public int dwValue;
            [FieldOffset(0)]
            public IntPtr pszValue;
            [FieldOffset(0)]
            public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct INTERNET_PER_CONN_OPTION_LIST
        {
            public int dwSize;
            public IntPtr pszConnection;
            public int dwOptionCount;
            public int dwOptionError;
            public IntPtr pOptions;
        }

        public static bool SetProxy(bool enabled, string host = "127.0.0.1", int port = 0, string bypass = "")
        {
            // 写入注册表
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                {
                    if (key != null)
                    {
                        key.SetValue("ProxyEnable", enabled ? 1 : 0, RegistryValueKind.DWord);
                        if (enabled)
                        {
                            var proxyStr = $"{host}:{port}";
                            key.SetValue("ProxyServer", proxyStr, RegistryValueKind.String);
                            key.SetValue("ProxyOverride", bypass, RegistryValueKind.String);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry update failed: {ex.Message}");
            }

            // 写入 WinInet 缓冲区
            var list = new INTERNET_PER_CONN_OPTION_LIST();
            var size = Marshal.SizeOf<INTERNET_PER_CONN_OPTION_LIST>();
            list.dwSize = size;
            list.pszConnection = IntPtr.Zero;

            INTERNET_PER_CONN_OPTION[] opts;
            if (enabled)
            {
                list.dwOptionCount = 3;
                opts = new INTERNET_PER_CONN_OPTION[3];

                opts[0].dwOption = INTERNET_PER_CONN_FLAGS;
                opts[0].Value.dwValue = PROXY_TYPE_DIRECT | PROXY_TYPE_PROXY;

                opts[1].dwOption = INTERNET_PER_CONN_PROXY_SERVER;
                opts[1].Value.pszValue = Marshal.StringToHGlobalAuto($"{host}:{port}");

                opts[2].dwOption = INTERNET_PER_CONN_PROXY_BYPASS;
                opts[2].Value.pszValue = Marshal.StringToHGlobalAuto(bypass);
            }
            else
            {
                list.dwOptionCount = 1;
                opts = new INTERNET_PER_CONN_OPTION[1];

                opts[0].dwOption = INTERNET_PER_CONN_FLAGS;
                opts[0].Value.dwValue = PROXY_TYPE_DIRECT;
            }

            var optSize = Marshal.SizeOf<INTERNET_PER_CONN_OPTION>();
            var pOpts = Marshal.AllocCoTaskMem(optSize * opts.Length);
            for (int i = 0; i < opts.Length; i++)
            {
                var ptr = new IntPtr(pOpts.ToInt64() + i * optSize);
                Marshal.StructureToPtr(opts[i], ptr, false);
            }
            list.pOptions = pOpts;

            var ipList = Marshal.AllocCoTaskMem(size);
            Marshal.StructureToPtr(list, ipList, false);

            var result = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PER_CONNECTION_OPTION, ipList, size);

            // 释放分配的非托管内存
            if (enabled)
            {
                Marshal.FreeHGlobal(opts[1].Value.pszValue);
                Marshal.FreeHGlobal(opts[2].Value.pszValue);
            }
            Marshal.FreeCoTaskMem(pOpts);
            Marshal.FreeCoTaskMem(ipList);

            if (result)
            {
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            }
            return result;
        }
    }
}
