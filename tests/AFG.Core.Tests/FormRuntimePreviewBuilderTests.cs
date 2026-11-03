// filepath: tests/AFG.Core.Tests/FormRuntimePreviewBuilderTests.cs
using System.Collections.Immutable;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Common;
using AFG.Shared.Controls;
using AFG.Shared.Services;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAssertions;
using Xunit;

namespace AFG.Core.Tests;

public sealed class FormRuntimePreviewBuilderTests
{
    static FormRuntimePreviewBuilderTests()
    {
        try
        {
            Avalonia.Skia.SkiaPlatform.Initialize();
        }
        catch
        {
            // Ignore if already initialized
        }
    }

    [Fact]
    public void BuildRuntimeControlTree_WithDefaultDoc_ShouldReturnCanvasWithCorrectDimensions()
    {
        // Arrange
        var doc = FormDocument.CreateDefault("TestFormView") with
        {
            CanvasWidth = 1024,
            CanvasHeight = 768,
            BackgroundColor = "#202025"
        };

        // Act
        var root = FormRuntimePreviewBuilder.BuildRuntimeControlTree(doc);

        // Assert
        root.Should().NotBeNull();
        root.Should().BeOfType<Canvas>();
        var canvas = (Canvas)root;
        canvas.Width.Should().Be(1024);
        canvas.Height.Should().Be(768);
        canvas.Background.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ControlType.DispatcherTimer)]
    [InlineData(ControlType.BackgroundWorker)]
    [InlineData(ControlType.BluetoothClient)]
    [InlineData(ControlType.SerialPortService)]
    [InlineData(ControlType.OpenFileDialog)]
    [InlineData(ControlType.SaveFileDialog)]
    [InlineData(ControlType.MessageBox)]
    [InlineData(ControlType.LogicFunction)]
    public void CreateRuntimeControl_NonVisualServices_ShouldReturnNull(ControlType type)
    {
        // Arrange
        var node = new AstNode
        {
            Name = "ServiceNode",
            Type = type
        };

        // Act
        var control = FormRuntimePreviewBuilder.CreateRuntimeControl(node);

        // Assert
        control.Should().BeNull();
        FormRuntimePreviewBuilder.IsNonVisualComponent(type).Should().BeTrue();
    }

    [Fact]
    public void CreateRuntimeControl_VisualControls_ShouldBeInteractive()
    {
        // Arrange
        var visualTypes = new[]
        {
            ControlType.Button,
            ControlType.TextBox,
            ControlType.TextBlock,
            ControlType.CheckBox,
            ControlType.RadioButton,
            ControlType.ComboBox,
            ControlType.DatePicker,
            ControlType.TimePicker,
            ControlType.Slider,
            ControlType.ProgressBar,
            ControlType.PictureBox,
            ControlType.Image,
            ControlType.MediaPlayer,
            ControlType.DebugConsole,
            ControlType.ListBox
        };

        foreach (var type in visualTypes)
        {
            var node = new AstNode
            {
                Name = $"Node_{type}",
                Type = type,
                Width = 150,
                Height = 40
            };

            // Act
            var ctrl = FormRuntimePreviewBuilder.CreateRuntimeControl(node);

            // Assert
            ctrl.Should().NotBeNull($"Control for {type} should not be null");
            ctrl!.IsHitTestVisible.Should().BeTrue($"Control for {type} must be interactive (IsHitTestVisible = true)");
        }
    }

    [Fact]
    public void CreateRuntimeControl_Containers_ShouldExcludeNonVisualChildren()
    {
        // Arrange: A Canvas container containing 2 visual controls and 2 non-visual services
        var containerNode = new AstNode
        {
            Name = "MainCanvas",
            Type = ControlType.Canvas,
            Children =
            [
                new AstNode { Name = "Btn1", Type = ControlType.Button, Content = "Submit" },
                new AstNode { Name = "Timer1", Type = ControlType.DispatcherTimer },
                new AstNode { Name = "Txt1", Type = ControlType.TextBox, Text = "Hello" },
                new AstNode { Name = "Worker1", Type = ControlType.BackgroundWorker }
            ]
        };

        // Act
        var containerCtrl = FormRuntimePreviewBuilder.CreateRuntimeControl(containerNode);

        // Assert
        containerCtrl.Should().NotBeNull();
        containerCtrl.Should().BeOfType<Canvas>();
        var canvas = (Canvas)containerCtrl!;

        // Only Btn1 and Txt1 should be added as children; Timer1 and Worker1 are automatically filtered
        canvas.Children.Count.Should().Be(2);
        canvas.Children[0].Should().BeOfType<Button>();
        canvas.Children[1].Should().BeOfType<TextBox>();
    }

    [Fact]
    public void CreateRuntimeControl_GridContainer_ShouldConfigureRowsColumnsAndSpans()
    {
        // Arrange
        var gridNode = new AstNode
        {
            Name = "LayoutGrid",
            Type = ControlType.Grid,
            RowDefinitions = [GridLengthModel.Auto, GridLengthModel.Star(1)],
            ColumnDefinitions = [GridLengthModel.Pixel(100), GridLengthModel.Star(1)],
            Children =
            [
                new AstNode
                {
                    Name = "HeaderBtn",
                    Type = ControlType.Button,
                    GridRow = 0,
                    GridColumn = 0,
                    GridRowSpan = 1,
                    GridColumnSpan = 2
                }
            ]
        };

        // Act
        var gridCtrl = FormRuntimePreviewBuilder.CreateRuntimeControl(gridNode) as Grid;

        // Assert
        gridCtrl.Should().NotBeNull();
        gridCtrl!.RowDefinitions.Count.Should().Be(2);
        gridCtrl.ColumnDefinitions.Count.Should().Be(2);
        gridCtrl.Children.Count.Should().Be(1);

        var child = gridCtrl.Children[0];
        Grid.GetRow(child).Should().Be(0);
        Grid.GetColumn(child).Should().Be(0);
        Grid.GetRowSpan(child).Should().Be(1);
        Grid.GetColumnSpan(child).Should().Be(2);
    }

    [Fact]
    public void CreateRuntimeControl_DockPanel_ShouldConfigureChildDock()
    {
        // Arrange
        var dockNode = new AstNode
        {
            Name = "MainDock",
            Type = ControlType.DockPanel,
            Children =
            [
                new AstNode
                {
                    Name = "TopNav",
                    Type = ControlType.Button,
                    Dock = DockPosition.Top
                }
            ]
        };

        // Act
        var dockCtrl = FormRuntimePreviewBuilder.CreateRuntimeControl(dockNode) as DockPanel;

        // Assert
        dockCtrl.Should().NotBeNull();
        dockCtrl!.Children.Count.Should().Be(1);
        DockPanel.GetDock(dockCtrl.Children[0]).Should().Be(Avalonia.Controls.Dock.Top);
    }

    [Fact]
    public void CreateRuntimeControl_PropertiesApplication_ShouldApplyCorrectStyles()
    {
        // Arrange
        var node = new AstNode
        {
            Name = "CustomBtn",
            Type = ControlType.Button,
            Content = "Click Me",
            Width = 200,
            Height = 50,
            Margin = new ThicknessModel(10, 15, 10, 15),
            Background = "#FF0000",
            Foreground = "#FFFFFF",
            FontSize = 16,
            FontWeight = "Bold",
            Opacity = 0.9,
            IsEnabled = true,
            IsVisible = true,
            CanvasLeft = 45,
            CanvasTop = 80,
            BorderBrush = "#00FF00",
            BorderThickness = new ThicknessModel(2, 2, 2, 2),
            CornerRadius = new CornerRadiusModel(8, 8, 8, 8)
        };

        // Act
        var ctrl = FormRuntimePreviewBuilder.CreateRuntimeControl(node);

        // Assert
        ctrl.Should().NotBeNull();
        var btn = ctrl as Button;
        btn.Should().NotBeNull();

        btn!.Width.Should().Be(200);
        btn.Height.Should().Be(50);
        btn.Margin.Should().Be(new Avalonia.Thickness(10, 15, 10, 15));
        btn.Opacity.Should().Be(0.9);
        btn.IsHitTestVisible.Should().BeTrue();
        Canvas.GetLeft(btn).Should().Be(45);
        Canvas.GetTop(btn).Should().Be(80);
        btn.CornerRadius.Should().Be(new Avalonia.CornerRadius(8));
        btn.BorderThickness.Should().Be(new Avalonia.Thickness(2));
        btn.FontSize.Should().Be(16);
        btn.FontWeight.Should().Be(FontWeight.Bold);
    }

    [Fact]
    public void CreateRuntimeControl_AutoSize_ShouldSetWidthAndHeightToNaN()
    {
        // Arrange
        var node = new AstNode
        {
            Name = "AutoLabel",
            Type = ControlType.TextBlock,
            Text = "Auto-sized text",
            AutoSize = true
        };

        // Act
        var ctrl = FormRuntimePreviewBuilder.CreateRuntimeControl(node);

        // Assert
        ctrl.Should().NotBeNull();
        double.IsNaN(ctrl!.Width).Should().BeTrue();
        double.IsNaN(ctrl.Height).Should().BeTrue();
    }
}
