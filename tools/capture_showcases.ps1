# capture_showcases.ps1
Add-Type -TypeDefinition @"
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public class WinCaptureEngine
{
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }

    public static IntPtr FindAppWindow(int targetPid)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindowsProc proc = delegate(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd)) return true;
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == (uint)targetPid)
            {
                RECT r;
                GetWindowRect(hWnd, out r);
                if (r.Width > 200 && r.Height > 200)
                {
                    found = hWnd;
                    return false;
                }
            }
            return true;
        };
        EnumWindows(proc, IntPtr.Zero);
        GC.KeepAlive(proc);
        return found;
    }

    public static bool CaptureWindow(IntPtr hWnd, string outputPath, bool maximize = false)
    {
        if (hWnd == IntPtr.Zero) return false;
        
        if (maximize)
        {
            ShowWindow(hWnd, 3); // SW_MAXIMIZE
        }
        else
        {
            ShowWindow(hWnd, 9); // SW_RESTORE
            RECT cur;
            GetWindowRect(hWnd, out cur);
            if (cur.Top < 0 || cur.Left < 0 || cur.Bottom > 1200)
            {
                int newTop = Math.Max(10, Math.Min(cur.Top, 1200 - cur.Height));
                int newLeft = Math.Max(10, Math.Min(cur.Left, 1920 - cur.Width));
                SetWindowPos(hWnd, IntPtr.Zero, newLeft, newTop, 0, 0, 0x0001 | 0x0004); // SWP_NOSIZE | SWP_NOZORDER
            }
        }
        
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(3000); // 確保 Avalonia / Skia / DirectX 完整首幀渲染

        RECT rect;
        GetWindowRect(hWnd, out rect);
        if (rect.Width <= 0 || rect.Height <= 0) return false;
        Console.WriteLine("[CaptureWindow] hWnd=" + hWnd + ", max=" + maximize + ", rect=" + rect.Left + "," + rect.Top + " - " + rect.Right + "," + rect.Bottom + " (" + rect.Width + "x" + rect.Height + ")");

        using (Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                try
                {
                    // 優先採用 PW_RENDERFULLCONTENT (0x2)，能 100% 準確抓取硬體加速與非互動視窗之後台緩衝區
                    bool ok = PrintWindow(hWnd, hdc, 2);
                    if (!ok)
                    {
                        PrintWindow(hWnd, hdc, 0);
                    }
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
            string dir = System.IO.Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            bmp.Save(outputPath, ImageFormat.Png);
        }
        return true;
    }
    public static void EnablePerMonitorDpi()
    {
        try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
    }
}
"@ -ReferencedAssemblies "System.Drawing.dll"

# Enable Per-Monitor V2 DPI awareness (-4)
[WinCaptureEngine]::EnablePerMonitorDpi()

$rootDir = "D:\P\CS\Avalonia Form Generator"
$afgExe = "$rootDir\src\AFG.Desktop\bin\Debug\net10.0\AFG.Desktop.exe"

$targetExample = $args[0]
if ($targetExample) {
    $examples = @($targetExample)
} else {
    $examples = @(
        "BasicControlsShowcase",
        "DialogsShowcase",
        "PictureBoxShowcase",
        "VectorGraphicsShowcase",
        "HighPerformanceDrawingShowcase",
        "MediaPlayerShowcase",
        "MultiLanguageInteropShowcase"
    )
}

foreach ($name in $examples) {
    Write-Host "=== Processing $name ==="
    $jsonPath = "$rootDir\example\$name\$name.afg.json"
    $runtimeExe = "$rootDir\example\$name\src\$name.Desktop\bin\Debug\net10.0\$name.Desktop.exe"
    $editorPng = "$rootDir\example\$name\screenshots\editor_design_view.png"
    $runtimePng = "$rootDir\example\$name\screenshots\runtime_result.png"

    # 1. Capture Editor Design View (Maximized)
    Write-Host "Launching AFG Editor with $name.afg.json..."
    $pEditor = Start-Process -FilePath $afgExe -ArgumentList "`"$jsonPath`"" -PassThru
    $hWndEditor = [IntPtr]::Zero
    for ($i = 0; $i -lt 25; $i++) {
        Start-Sleep -Milliseconds 500
        $hWndEditor = [WinCaptureEngine]::FindAppWindow($pEditor.Id)
        if ($hWndEditor -ne [IntPtr]::Zero) { break }
    }

    if ($hWndEditor -ne [IntPtr]::Zero) {
        $ok = [WinCaptureEngine]::CaptureWindow($hWndEditor, $editorPng, $true)
        Write-Host "Editor screenshot captured: $ok -> $editorPng"
    } else {
        Write-Warning "Could not find editor window for $name (PID $($pEditor.Id))"
    }
    Stop-Process -Id $pEditor.Id -Force
    Start-Sleep -Seconds 1

    # 2. Capture Runtime View (Restored / Native)
    Write-Host "Launching Runtime $name.Desktop.exe..."
    $pRuntime = Start-Process -FilePath $runtimeExe -PassThru
    $hWndRuntime = [IntPtr]::Zero
    for ($i = 0; $i -lt 25; $i++) {
        Start-Sleep -Milliseconds 500
        $hWndRuntime = [WinCaptureEngine]::FindAppWindow($pRuntime.Id)
        if ($hWndRuntime -ne [IntPtr]::Zero) { break }
    }

    if ($hWndRuntime -ne [IntPtr]::Zero) {
        $ok = [WinCaptureEngine]::CaptureWindow($hWndRuntime, $runtimePng, $false)
        Write-Host "Runtime screenshot captured: $ok -> $runtimePng"
    } else {
        Write-Warning "Could not find runtime window for $name (PID $($pRuntime.Id))"
    }
    Stop-Process -Id $pRuntime.Id -Force
    Start-Sleep -Seconds 1
}

Write-Host "Screenshot capture routine completed!"
