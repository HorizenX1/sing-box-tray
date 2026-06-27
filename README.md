# sing-box-tray

`sing-box-tray` is a lightweight, zero-dependency, and high-performance Windows system tray manager and process supervisor for the [`sing-box`](https://github.com/SagerNet/sing-box) universal proxy platform.

Compiled using **.NET 8 Native AOT**, it runs as a single self-contained binary with an extremely small memory footprint (~10-20MB) and does not require the .NET Runtime to be preinstalled on the system.

## Features

- 🚀 **Native AOT Compiled**: Ultra-fast startup, ~10MB memory footprint, zero external dependencies.
- 🔄 **Process Supervision**: Automatically runs and restarts the `sing-box.exe` core.
- 🛡️ **No Config Leakage**: Feeds configuration via `stdin` to prevent disk exposure.
- 🌐 **Proxy & TUN Toggle**: Instantly switch Windows system proxy or TUN mode (with auto UAC elevation).
- 🖱️ **Mouse Shortcuts**: Left Click for Dashboard, Shift + Left Click to toggle proxy modes.
- 🎨 **DPI Aware Icons**: Uses Windows API to scale the tray icon sharply across resolutions.
- ⚙️ **Automatic Startup**: One-click toggle for Windows startup.

## File Structure

When running, the application organizes files in a local `data` directory next to the executable:

```text
├── sing-box-tray.exe           # The main tray executable
└── data/                       # Config and runtime directory
    ├── sing-box.exe            # Place the sing-box core here
    ├── config.json             # Your sing-box JSON configuration
    ├── tray-config.ini         # Automatically generated tray configuration
    └── sing-box-tray.log       # Log file for tray output and core standard streams
```

## Configuration Options

Upon first run, `data/tray-config.ini` is generated with the following options:

```ini
[tray-config]
# Path to the directory containing sing-box.exe (defaults to the data directory)
sb-dir = 
# Path to your sing-box JSON config (defaults to config.json)
sb-config-file = config.json
# Initial TUN mode setting on startup ("on" or "off")
tun-start-mode = off
# Auto-enable Windows system proxy on startup (1 to enable, 0 to disable)
system-proxy-auto = 0
# Bypass addresses for the system proxy (separated by semicolons, e.g., "github.com;google.com")
system-proxy-bypass = 
# Bypass local/intranet addresses for the system proxy (1 to enable, 0 to disable)
system-proxy-bypass-local = 0
# Path to log file
log-file = sing-box-tray.log
```

> [!NOTE]
> The tray automatically parses `config.json` to extract the listening port from the `mixed` type inbound to configure the system proxy. If TUN mode is enabled, it automatically enables the `tun` inbound; if TUN mode is disabled, it dynamically strips the `tun` inbound from the configuration when starting the core.

## Prerequisites & Installation

1. Download the latest compiled release from the Releases page.
2. Download the official `sing-box` Windows core (e.g., `sing-box.exe`) from [SagerNet/sing-box](https://github.com/SagerNet/sing-box/releases).
3. Create a `data` folder in the same directory as `sing-box-tray.exe` and place `sing-box.exe` and your `config.json` inside it.
4. Run `sing-box-tray.exe`.
5. Access configurations, toggle proxy/TUN modes, or restart the core by right-clicking the tray icon.

## Building from Source

To compile the Native AOT binary yourself, you will need the [.NET 8 SDK](https://dotnet.microsoft.com/download) installed:

```bash
# Clone the repository
git clone https://github.com/HorizenX1/sing-box-tray.git
cd sing-box-tray

# Publish the Native AOT executable for Windows x64
dotnet publish -c Release -r win-x64
```

The compiled binary will be located in `bin/Release/net8.0-windows/win-x64/publish/`.

## License

This project is open-source and licensed under the MIT License.
