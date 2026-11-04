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
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }

    public static IntPtr FindWindowByPid(int targetPid)
    {
        IntPtr foundHwnd = IntPtr.Zero;
        EnumWindowsProc proc = delegate(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd)) return true;
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == (uint)targetPid)
            {
                StringBuilder sb = new StringBuilder(256);
                GetClassName(hWnd, sb, 256);
                string className = sb.ToString();
                RECT r;
                GetWindowRect(hWnd, out r);
                if (r.Width > 150 && r.Height > 150)
                {
                    foundHwnd = hWnd;
                    return false;
                }
            }
            return true;
        };
        EnumWindows(proc, IntPtr.Zero);
        GC.KeepAlive(proc);
        return foundHwnd;
    }

    public static bool CaptureWindow(IntPtr hWnd, string outputPath)
    {
        if (hWnd == IntPtr.Zero) return false;
        ShowWindow(hWnd, 9);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(500);

        RECT rect;
        GetWindowRect(hWnd, out rect);
        if (rect.Width <= 0 || rect.Height <= 0) return false;

        using (Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                try
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(rect.Width, rect.Height), CopyPixelOperation.SourceCopy);
                }
                catch
                {
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        PrintWindow(hWnd, hdc, 2);
                    }
                    finally
                    {
                        g.ReleaseHdc(hdc);
                    }
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
}
"@ -ReferencedAssemblies "System.Drawing.dll"

$rootDir = "D:\P\CS\Avalonia Form Generator"
$afgExe = "$rootDir\src\AFG.Desktop\bin\Debug\net10.0\AFG.Desktop.exe"

$examples = @(
    "BasicControlsShowcase",
    "DialogsShowcase",
    "PictureBoxShowcase",
    "VectorGraphicsShowcase",
    "HighPerformanceDrawingShowcase",
    "MediaPlayerShowcase",
    "MultiLanguageInteropShowcase"
)

foreach ($name in $examples) {
    Write-Host "=== Processing $name ==="
    $jsonPath = "$rootDir\example\$name\$name.afg.json"
    $runtimeExe = "$rootDir\example\$name\src\$name.Desktop\bin\Debug\net10.0\$name.Desktop.exe"
    $editorPng = "$rootDir\example\$name\screenshots\editor_design_view.png"
    $runtimePng = "$rootDir\example\$name\screenshots\runtime_result.png"

    # 1. Capture Editor Design View
    Write-Host "Launching AFG Editor with $name.afg.json..."
    $pEditor = Start-Process -FilePath $afgExe -ArgumentList "`"$jsonPath`"" -PassThru
    Start-Sleep -Seconds 5

    $hWndEditor = [WinCaptureEngine]::FindWindowByPid($pEditor.Id)
    if ($hWndEditor -eq [IntPtr]::Zero) {
        Start-Sleep -Seconds 2
        $hWndEditor = [WinCaptureEngine]::FindWindowByPid($pEditor.Id)
    }

    if ($hWndEditor -ne [IntPtr]::Zero) {
        $ok = [WinCaptureEngine]::CaptureWindow($hWndEditor, $editorPng)
        Write-Host "Editor screenshot captured: $ok -> $editorPng"
    } else {
        Write-Warning "Could not find editor window for $name (PID $($pEditor.Id))"
    }
    Stop-Process -Id $pEditor.Id -Force
    Start-Sleep -Seconds 1

    # 2. Capture Runtime View
    Write-Host "Launching Runtime $name.Desktop.exe..."
    $pRuntime = Start-Process -FilePath $runtimeExe -PassThru
    Start-Sleep -Seconds 4

    $hWndRuntime = [WinCaptureEngine]::FindWindowByPid($pRuntime.Id)
    if ($hWndRuntime -eq [IntPtr]::Zero) {
        Start-Sleep -Seconds 2
        $hWndRuntime = [WinCaptureEngine]::FindWindowByPid($pRuntime.Id)
    }

    if ($hWndRuntime -ne [IntPtr]::Zero) {
        $ok = [WinCaptureEngine]::CaptureWindow($hWndRuntime, $runtimePng)
        Write-Host "Runtime screenshot captured: $ok -> $runtimePng"
    } else {
        Write-Warning "Could not find runtime window for $name (PID $($pRuntime.Id))"
    }
    Stop-Process -Id $pRuntime.Id -Force
    Start-Sleep -Seconds 1
}

Write-Host "All screenshots capture routine completed!"
