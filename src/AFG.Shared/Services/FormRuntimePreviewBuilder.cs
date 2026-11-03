// filepath: src/AFG.Shared/Services/FormRuntimePreviewBuilder.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Shared.Controls;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace AFG.Shared.Services;

/// <summary>
/// 動態表單控制項生成器：將 FormDocument / AstNode 轉換為真實、具互動性之 Avalonia 控制項樹。
/// </summary>
public static class FormRuntimePreviewBuilder
{
    /// <summary>
    /// 依據 FormDocument 建立執行期預覽控制項樹根節點。
    /// </summary>
    /// <param name="doc">表單設計文件。</param>
    /// <returns>已具備完整幾何與互動能力的 Avalonia 根控制項。</returns>
    public static Control BuildRuntimeControlTree(FormDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var rootControl = CreateRuntimeControl(doc.RootNode) ?? new Canvas();

        if (rootControl is Canvas canvas)
        {
            if (double.IsNaN(canvas.Width) || canvas.Width <= 0)
            {
                canvas.Width = doc.CanvasWidth;
            }
            if (double.IsNaN(canvas.Height) || canvas.Height <= 0)
            {
                canvas.Height = doc.CanvasHeight;
            }

            if (canvas.Background is null && !string.IsNullOrWhiteSpace(doc.BackgroundColor) && Color.TryParse(doc.BackgroundColor, out var parsedBg))
            {
                canvas.Background = new SolidColorBrush(parsedBg);
            }

            canvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            canvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        }

        return rootControl;
    }

    /// <summary>
    /// 依據 AST 節點建立單一 Avalonia 控制項及其子階層，若為非可視服務元件則傳回 null。
    /// </summary>
    /// <param name="node">AST 節點。</param>
    /// <returns>具備互動能力的 Avalonia 控制項；非可視元件則為 null。</returns>
    public static Control? CreateRuntimeControl(AstNode node)
    {
        if (node is null) return null;

        // 自動過濾非可視元件，不產生視覺佔位符，確保 100% 還原真實運行外觀
        if (IsNonVisualComponent(node.Type))
        {
            return null;
        }

        Control control;

        switch (node.Type)
        {
            case ControlType.Button:
                control = new Button
                {
                    Content = node.Content ?? node.Text ?? "Button"
                };
                break;

            case ControlType.TextBox:
                control = new TextBox
                {
                    Text = node.Text ?? string.Empty,
                    PlaceholderText = node.Watermark
                };
                break;

            case ControlType.TextBlock:
                control = new TextBlock
                {
                    Text = node.Text ?? node.Content ?? "TextBlock"
                };
                break;

            case ControlType.CheckBox:
                control = new CheckBox
                {
                    Content = node.Content ?? node.Text ?? "CheckBox",
                    IsChecked = node.IsChecked
                };
                break;

            case ControlType.RadioButton:
                control = new RadioButton
                {
                    Content = node.Content ?? node.Text ?? "RadioButton",
                    IsChecked = node.IsChecked
                };
                break;

            case ControlType.ComboBox:
                control = new ComboBox
                {
                    PlaceholderText = string.IsNullOrWhiteSpace(node.Watermark) ? "請選擇項目..." : node.Watermark,
                    ItemsSource = new[] { "選項 A", "選項 B", "選項 C" },
                    SelectedIndex = 0
                };
                break;

            case ControlType.ListBox:
                control = new ListBox
                {
                    ItemsSource = new[] { "清單項目 1", "清單項目 2", "清單項目 3" }
                };
                break;

            case ControlType.DatePicker:
                control = new DatePicker();
                break;

            case ControlType.TimePicker:
                control = new TimePicker();
                break;

            case ControlType.Slider:
                control = new Slider
                {
                    Minimum = node.Minimum ?? 0,
                    Maximum = node.Maximum ?? 100,
                    Value = node.Value ?? 50
                };
                break;

            case ControlType.ProgressBar:
                control = new ProgressBar
                {
                    Minimum = node.Minimum ?? 0,
                    Maximum = node.Maximum ?? 100,
                    Value = node.Value ?? 60
                };
                break;

            case ControlType.PictureBox or ControlType.Image:
                var picBgColor = !string.IsNullOrWhiteSpace(node.Background) && Color.TryParse(node.Background, out var parsedBg)
                    ? parsedBg
                    : (node.InitBitmap
                        ? (Color.TryParse(node.BitmapBackgroundColor ?? "#F0F0F0", out var bBg) ? bBg : Color.Parse("#F0F0F0"))
                        : Colors.Transparent);

                var picBorder = new Border
                {
                    Background = new SolidColorBrush(picBgColor),
                    ClipToBounds = true
                };

                bool imageLoaded = false;
                if (!string.IsNullOrWhiteSpace(node.Source))
                {
                    try
                    {
                        var bitmap = BitmapHelper.LoadBitmap(node.Source);
                        if (bitmap is not null)
                        {
                            var avaloniaStretch = node.Stretch switch
                            {
                                Core.Enums.Stretch.None => Avalonia.Media.Stretch.None,
                                Core.Enums.Stretch.Fill => Avalonia.Media.Stretch.Fill,
                                Core.Enums.Stretch.UniformToFill => Avalonia.Media.Stretch.UniformToFill,
                                _ => Avalonia.Media.Stretch.Uniform
                            };
                            var imgControl = new Image
                            {
                                Source = bitmap,
                                Stretch = avaloniaStretch
                            };
                            picBorder.Child = imgControl;
                            imageLoaded = true;
                        }
                    }
                    catch
                    {
                        imageLoaded = false;
                    }
                }

                if (!imageLoaded)
                {
                    var picPanel = new StackPanel
                    {
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 4
                    };

                    var picTitleText = node.InitBitmap ? "PictureBox [Bitmap Init]" : "PictureBox";
                    var picTitleColor = node.InitBitmap ? Color.Parse("#A78BFA") : Color.Parse("#A1A1AA");

                    picPanel.Children.Add(new TextBlock
                    {
                        Text = picTitleText,
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(picTitleColor),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    });

                    var sourceText = node.InitBitmap
                        ? $"Size: {(node.Width ?? 200)}x{(node.Height ?? 150)} | Bg: {node.BitmapBackgroundColor ?? "#F0F0F0"}"
                        : (!string.IsNullOrWhiteSpace(node.Source)
                            ? node.Source
                            : (!string.IsNullOrWhiteSpace(node.Text) ? node.Text : (!string.IsNullOrWhiteSpace(node.Content) ? node.Content : "(無影像來源)")));

                    picPanel.Children.Add(new TextBlock
                    {
                        Text = sourceText,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#71717A")),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 180
                    });

                    picBorder.Child = picPanel;
                }

                control = picBorder;
                break;

            case ControlType.MediaPlayer:
                var player = new MediaPlayerControl
                {
                    AutoPlay = node.AutoPlay ?? false,
                    IsLooping = node.IsLooping ?? false,
                    Volume = node.Volume ?? 1.0,
                    Stretch = (Avalonia.Media.Stretch)(node.Stretch ?? AFG.Core.Enums.Stretch.Uniform)
                };
                if (!string.IsNullOrWhiteSpace(node.Source))
                {
                    player.Load(node.Source);
                }
                control = player;
                break;

            case ControlType.Border:
                var border = new Border();
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        border.Child = childControl;
                        break;
                    }
                }
                control = border;
                break;

            case ControlType.StackPanel:
                var sp = new StackPanel
                {
                    Orientation = (node.Orientation ?? Core.Enums.Orientation.Vertical) == Core.Enums.Orientation.Horizontal
                        ? Avalonia.Layout.Orientation.Horizontal
                        : Avalonia.Layout.Orientation.Vertical
                };
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        sp.Children.Add(childControl);
                    }
                }
                control = sp;
                break;

            case ControlType.Grid:
                var grid = new Grid();
                if (node.RowDefinitions.Count > 0)
                {
                    grid.RowDefinitions = new RowDefinitions(string.Join(",", node.RowDefinitions.Select(r => r.ToString())));
                }
                if (node.ColumnDefinitions.Count > 0)
                {
                    grid.ColumnDefinitions = new ColumnDefinitions(string.Join(",", node.ColumnDefinitions.Select(c => c.ToString())));
                }
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        if (child.GridRow > 0) Grid.SetRow(childControl, child.GridRow);
                        if (child.GridColumn > 0) Grid.SetColumn(childControl, child.GridColumn);
                        if (child.GridRowSpan > 1) Grid.SetRowSpan(childControl, child.GridRowSpan);
                        if (child.GridColumnSpan > 1) Grid.SetColumnSpan(childControl, child.GridColumnSpan);
                        grid.Children.Add(childControl);
                    }
                }
                control = grid;
                break;

            case ControlType.Canvas:
                var innerCanvas = new Canvas();
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        innerCanvas.Children.Add(childControl);
                    }
                }
                control = innerCanvas;
                break;

            case ControlType.DockPanel:
                var dockPanel = new DockPanel();
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        if (child.Dock.HasValue)
                        {
                            DockPanel.SetDock(childControl, ToAvaloniaDock(child.Dock.Value));
                        }
                        dockPanel.Children.Add(childControl);
                    }
                }
                control = dockPanel;
                break;

            case ControlType.WrapPanel:
                var wrapPanel = new WrapPanel
                {
                    Orientation = (node.Orientation ?? Core.Enums.Orientation.Horizontal) == Core.Enums.Orientation.Vertical
                        ? Avalonia.Layout.Orientation.Vertical
                        : Avalonia.Layout.Orientation.Horizontal
                };
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        wrapPanel.Children.Add(childControl);
                    }
                }
                control = wrapPanel;
                break;

            case ControlType.ScrollViewer:
                var scrollViewer = new ScrollViewer();
                foreach (var child in node.Children)
                {
                    var childControl = CreateRuntimeControl(child);
                    if (childControl is not null)
                    {
                        scrollViewer.Content = childControl;
                        break;
                    }
                }
                control = scrollViewer;
                break;

            case ControlType.DebugConsole:
                var consoleBorder = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#09090B")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#27272A")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6)
                };
                var consoleGrid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto, *")
                };
                var headerPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                var titleText = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(node.Text) ? "Debug Console" : node.Text,
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E4E4E7")),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                DockPanel.SetDock(titleText, Avalonia.Controls.Dock.Left);

                var logListBox = new ListBox
                {
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.Parse("#A1A1AA")),
                    FontSize = 10,
                    ItemsSource = new System.Collections.Generic.List<string>
                    {
                        "[INF] 應用程式預覽已初始化。",
                        "[INF] 獨立表單執行期模式已就緒。",
                        "[DBG] 互動式除錯主控台正在監聽..."
                    }
                };

                var clearBtn = new Button
                {
                    Content = "Clear",
                    FontSize = 10,
                    Padding = new Thickness(8, 2),
                    Background = new SolidColorBrush(Color.Parse("#27272A")),
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                };
                clearBtn.Click += (_, _) =>
                {
                    if (logListBox.ItemsSource is System.Collections.IList list)
                    {
                        list.Clear();
                    }
                };

                headerPanel.Children.Add(titleText);
                headerPanel.Children.Add(clearBtn);
                Grid.SetRow(headerPanel, 0);
                Grid.SetRow(logListBox, 1);

                consoleGrid.Children.Add(headerPanel);
                consoleGrid.Children.Add(logListBox);
                consoleBorder.Child = consoleGrid;
                control = consoleBorder;
                break;

            default:
                control = new ContentControl { Content = $"{node.Type}" };
                break;
        }

        // --- 套用通用幾何、外觀與可互動性屬性 ---
        ApplyCommonProperties(control, node);

        return control;
    }

    /// <summary>
    /// 檢查是否為非可視服務元件。
    /// </summary>
    public static bool IsNonVisualComponent(ControlType type) => type switch
    {
        ControlType.DispatcherTimer or
        ControlType.BackgroundWorker or
        ControlType.BluetoothClient or
        ControlType.SerialPortService or
        ControlType.OpenFileDialog or
        ControlType.SaveFileDialog or
        ControlType.MessageBox or
        ControlType.LogicFunction => true,
        _ => false
    };

    /// <summary>
    /// 將 AFG 停靠方向映射為 Avalonia Dock 列舉。
    /// </summary>
    public static Avalonia.Controls.Dock ToAvaloniaDock(DockPosition dock) => dock switch
    {
        DockPosition.Left => Avalonia.Controls.Dock.Left,
        DockPosition.Top => Avalonia.Controls.Dock.Top,
        DockPosition.Right => Avalonia.Controls.Dock.Right,
        DockPosition.Bottom => Avalonia.Controls.Dock.Bottom,
        _ => Avalonia.Controls.Dock.Left
    };

    private static void ApplyCommonProperties(Control control, AstNode node)
    {
        control.Tag = node.Id;
        if (!string.IsNullOrWhiteSpace(node.Name))
        {
            control.Name = node.Name;
        }

        // 確保控制項具備完整互動性，支援使用者正常打字、點選、滑動與展開下拉選單
        control.IsHitTestVisible = true;
        control.Opacity = node.Opacity;
        control.IsVisible = node.IsVisible;
        control.IsEnabled = node.IsEnabled;
        control.ZIndex = node.ZIndex;

        // 排版對齊
        control.HorizontalAlignment = (Avalonia.Layout.HorizontalAlignment)node.HorizontalAlignment;
        control.VerticalAlignment = (Avalonia.Layout.VerticalAlignment)node.VerticalAlignment;

        // 邊距 (Margin / Padding)
        control.Margin = new Thickness(node.Margin.Left, node.Margin.Top, node.Margin.Right, node.Margin.Bottom);

        if (node.Padding != AFG.Core.Models.Common.ThicknessModel.Zero)
        {
            var padding = new Thickness(node.Padding.Left, node.Padding.Top, node.Padding.Right, node.Padding.Bottom);
            if (control is Border bPadding) bPadding.Padding = padding;
            else if (control is TemplatedControl tcPadding) tcPadding.Padding = padding;
        }

        // 尺寸幾何 (Width / Height / AutoSize)
        if (node.AutoSize)
        {
            control.Width = double.NaN;
            control.Height = double.NaN;
        }
        else
        {
            if (node.Width.HasValue)
            {
                control.Width = node.Width.Value;
            }
            else if (!node.IsContainer)
            {
                control.Width = 120;
            }

            if (node.Height.HasValue)
            {
                control.Height = node.Height.Value;
            }
            else if (!node.IsContainer)
            {
                control.Height = 35;
            }
        }

        if (node.MinWidth.HasValue) control.MinWidth = node.MinWidth.Value;
        if (node.MinHeight.HasValue) control.MinHeight = node.MinHeight.Value;
        if (node.MaxWidth.HasValue) control.MaxWidth = node.MaxWidth.Value;
        if (node.MaxHeight.HasValue) control.MaxHeight = node.MaxHeight.Value;

        // 附屬位置排版屬性 (Canvas, Grid, DockPanel)
        var left = node.CanvasLeft ?? 0;
        var top = node.CanvasTop ?? 0;
        Canvas.SetLeft(control, left);
        Canvas.SetTop(control, top);

        if (node.GridRow > 0) Grid.SetRow(control, node.GridRow);
        if (node.GridColumn > 0) Grid.SetColumn(control, node.GridColumn);
        if (node.GridRowSpan > 1) Grid.SetRowSpan(control, node.GridRowSpan);
        if (node.GridColumnSpan > 1) Grid.SetColumnSpan(control, node.GridColumnSpan);

        if (node.Dock.HasValue)
        {
            DockPanel.SetDock(control, ToAvaloniaDock(node.Dock.Value));
        }

        // 背景色 (Background)
        if (!string.IsNullOrWhiteSpace(node.Background) && Color.TryParse(node.Background, out var bgColor))
        {
            var brush = new SolidColorBrush(bgColor);
            if (control is Panel p) p.Background = brush;
            else if (control is Border b) b.Background = brush;
            else if (control is TemplatedControl tc) tc.Background = brush;
        }

        // 前景色 (Foreground)
        if (!string.IsNullOrWhiteSpace(node.Foreground) && Color.TryParse(node.Foreground, out var fgColor))
        {
            var brush = new SolidColorBrush(fgColor);
            if (control is TextBlock tb) tb.Foreground = brush;
            else if (control is TemplatedControl tc) tc.Foreground = brush;
        }

        // 字型大小 (FontSize)
        if (node.FontSize.HasValue && node.FontSize.Value > 0)
        {
            if (control is TextBlock tb) tb.FontSize = node.FontSize.Value;
            else if (control is TemplatedControl tc) tc.FontSize = node.FontSize.Value;
        }

        // 字型粗細 (FontWeight)
        if (!string.IsNullOrWhiteSpace(node.FontWeight) && Enum.TryParse<FontWeight>(node.FontWeight, true, out var fw))
        {
            if (control is TextBlock tb) tb.FontWeight = fw;
            else if (control is TemplatedControl tc) tc.FontWeight = fw;
        }

        // 文字對齊 (TextAlignment)
        if (control is TextBlock textBlock)
        {
            if (node.CustomProperties.TryGetValue("TextAlignment", out var alignStr) &&
                Enum.TryParse<Avalonia.Media.TextAlignment>(alignStr, true, out var ta))
            {
                textBlock.TextAlignment = ta;
            }
            else
            {
                textBlock.TextAlignment = node.HorizontalAlignment switch
                {
                    Core.Enums.HorizontalAlignment.Center => Avalonia.Media.TextAlignment.Center,
                    Core.Enums.HorizontalAlignment.Right => Avalonia.Media.TextAlignment.Right,
                    _ => Avalonia.Media.TextAlignment.Left
                };
            }
        }
        else if (control is TextBox textBox)
        {
            if (node.CustomProperties.TryGetValue("TextAlignment", out var alignStr) &&
                Enum.TryParse<Avalonia.Media.TextAlignment>(alignStr, true, out var ta))
            {
                textBox.TextAlignment = ta;
            }
            else
            {
                textBox.TextAlignment = node.HorizontalAlignment switch
                {
                    Core.Enums.HorizontalAlignment.Center => Avalonia.Media.TextAlignment.Center,
                    Core.Enums.HorizontalAlignment.Right => Avalonia.Media.TextAlignment.Right,
                    _ => Avalonia.Media.TextAlignment.Left
                };
            }
        }

        // 邊框樣式 (BorderBrush & BorderThickness)
        if (!string.IsNullOrWhiteSpace(node.BorderBrush) && Color.TryParse(node.BorderBrush, out var bColor))
        {
            var brush = new SolidColorBrush(bColor);
            if (control is Border b) b.BorderBrush = brush;
            else if (control is TemplatedControl tc) tc.BorderBrush = brush;
        }

        if (node.BorderThickness.HasValue)
        {
            var bt = new Thickness(node.BorderThickness.Value.Left, node.BorderThickness.Value.Top, node.BorderThickness.Value.Right, node.BorderThickness.Value.Bottom);
            if (control is Border b) b.BorderThickness = bt;
            else if (control is TemplatedControl tc) tc.BorderThickness = bt;
        }

        // 圓角半徑 (CornerRadius)
        if (node.CornerRadius.HasValue)
        {
            var cr = new CornerRadius(node.CornerRadius.Value.TopLeft, node.CornerRadius.Value.TopRight, node.CornerRadius.Value.BottomRight, node.CornerRadius.Value.BottomLeft);
            if (control is Border b) b.CornerRadius = cr;
            else if (control is TemplatedControl tc) tc.CornerRadius = cr;
            else if (control is Button btn) btn.CornerRadius = cr;
        }

        // 陰影裝飾 (BoxShadow)
        if (node.BoxShadow.HasValue)
        {
            var bs = node.BoxShadow.Value;
            if (control is Border b)
            {
                try
                {
                    b.BoxShadow = BoxShadows.Parse(bs.ToString());
                }
                catch { }
            }
            else if (Color.TryParse(bs.Color, out var sColor))
            {
                control.Effect = new DropShadowEffect
                {
                    OffsetX = bs.OffsetX,
                    OffsetY = bs.OffsetY,
                    BlurRadius = bs.Blur,
                    Color = sColor,
                    Opacity = sColor.A / 255.0
                };
            }
        }
    }
}
