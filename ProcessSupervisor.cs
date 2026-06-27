using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sing_box_tray
{
    public class ProcessSupervisor : IDisposable
    {
        private readonly string _exePath;
        private readonly string _logFile;
        private Process? _process;
        private string _currentConfig = "";
        private bool _shouldRun;
        private bool _isRestarting;
        private readonly object _lock = new();

        public event Action<string, string>? OnStateChanged; // state, message

        public bool IsRunning => _process != null && !_process.HasExited;

        public ProcessSupervisor(string exePath, string logFile)
        {
            _exePath = exePath;
            _logFile = logFile;
        }

        public void Start(string configJson)
        {
            lock (_lock)
            {
                _shouldRun = true;
                _currentConfig = configJson;
                StopInternal();
                StartInternal();
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _shouldRun = false;
                StopInternal();
            }
        }

        private void StartInternal()
        {
            if (!File.Exists(_exePath))
            {
                OnStateChanged?.Invoke("failed", I18n.ExeNotFound(_exePath));
                return;
            }

            try
            {
                _process = new Process();
                _process.StartInfo.FileName = _exePath;
                _process.StartInfo.Arguments = "run -c stdin";
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.CreateNoWindow = true;
                _process.StartInfo.RedirectStandardInput = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                _process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                _process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_exePath) ?? "";

                _process.EnableRaisingEvents = true;
                _process.Exited += Process_Exited;

                _process.Start();

                // 写入 JSON 配置文件 (使用无 BOM 的 UTF-8 编码，防止垃圾回收或双重释放导致的已关闭文件访问错误)
                var bytes = new UTF8Encoding(false).GetBytes(_currentConfig);
                _process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                _process.StandardInput.BaseStream.Flush();
                _process.StandardInput.Close();

                // 异步读取 stdout 和 stderr 并记录日志
                Task.Run(() => ReadStreamAsync(_process.StandardOutput));
                Task.Run(() => ReadStreamAsync(_process.StandardError));

                OnStateChanged?.Invoke("running", "");
            }
            catch (Exception ex)
            {
                OnStateChanged?.Invoke("failed", I18n.FailedToStart(ex.Message));
            }
        }

        private void StopInternal()
        {
            if (_process != null)
            {
                _process.Exited -= Process_Exited;
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process.WaitForExit(3000);
                    }
                }
                catch {}
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            lock (_lock)
            {
                if (!_shouldRun)
                {
                    OnStateChanged?.Invoke("stopped", "");
                    return;
                }

                if (_isRestarting) return;
                _isRestarting = true;

                Log(I18n.LogExitedUnexpectedly);
                OnStateChanged?.Invoke("failed", I18n.ExitedUnexpectedly);

                // 延时 3 秒后重新启动
                Task.Delay(3000).ContinueWith(_ =>
                {
                    lock (_lock)
                    {
                        _isRestarting = false;
                        if (_shouldRun)
                        {
                            StopInternal();
                            StartInternal();
                        }
                    }
                });
            }
        }

        private async Task ReadStreamAsync(StreamReader reader)
        {
            try
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line != null)
                    {
                        Log(line);
                    }
                }
            }
            catch {}
        }

        private void Log(string message)
        {
            if (string.IsNullOrEmpty(_logFile)) return;
            try
            {
                var dir = Path.GetDirectoryName(_logFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"{timestamp} [sing-box] {message}{Environment.NewLine}";
                
                // 循环重试写入日志（防止文件占用）
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        File.AppendAllText(_logFile, logLine);
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(50);
                    }
                }
            }
            catch {}
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
