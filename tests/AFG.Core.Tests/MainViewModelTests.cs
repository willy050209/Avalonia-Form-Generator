// filepath: tests/AFG.Core.Tests/MainViewModelTests.cs
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Shared.Models;
using AFG.Shared.ViewModels;
using FluentAssertions;

namespace AFG.Core.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public void MainViewModel_Initialization_ShouldGeneratePreviewCodeImmediately()
    {
        // Arrange & Act
        var vm = new MainViewModel();

        // Assert
        vm.GeneratedViewCode.Should().NotBeNullOrWhiteSpace();
        vm.GeneratedViewCode.Should().Contain("public partial class MainFormView : UserControl");

        vm.GeneratedVmCode.Should().NotBeNullOrWhiteSpace();
        vm.GeneratedVmCode.Should().Contain("public partial class MainFormViewModel : ObservableObject");
    }

    [Fact]
    public void MainViewModel_WhenControlAdded_ShouldUpdatePreviewCodeImmediately()
    {
        // Arrange
        var vm = new MainViewModel();
        var initialViewCode = vm.GeneratedViewCode;

        // Act: 加入一個 Button 控制項
        var buttonToolboxItem = new ToolboxItem("按鈕", "常用", ControlType.Button, "btn", 120, 35, "Click Me");
        vm.Canvas.AddControlFromToolbox(buttonToolboxItem, 100, 100);

        // Assert
        vm.GeneratedViewCode.Should().NotBeNullOrWhiteSpace();
        vm.GeneratedViewCode.Should().NotBe(initialViewCode);
        vm.GeneratedViewCode.Should().Contain(".Content(\"Click Me\")");
    }

    [Fact]
    public void MainViewModel_WhenBindingAdded_ShouldUpdatePreviewCodeWithBinding()
    {
        // Arrange
        var vm = new MainViewModel();
        var tbItem = new ToolboxItem("文字方塊", "常用", ControlType.TextBox, "txt", 150, 30, "");
        vm.Canvas.AddControlFromToolbox(tbItem, 50, 50);

        var selected = vm.Canvas.SelectedNode;
        selected.Should().NotBeNull();

        // Act: 為控制項配置資料綁定
        var updatedNode = selected! with
        {
            Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "userName", Mode = BindingMode.TwoWay }]
        };
        vm.Canvas.UpdateNodeProperties(updatedNode);

        // Assert
        vm.GeneratedViewCode.Should().Contain(".Text(nameof(MainFormViewModel.UserName), BindingMode.TwoWay)");
        vm.GeneratedVmCode.Should().Contain("private string _userName = string.Empty;");
    }

    [Fact]
    public void MainViewModel_WhenControlDeleted_ShouldUpdatePreviewCodeImmediately()
    {
        // Arrange
        var vm = new MainViewModel();
        var tbItem = new ToolboxItem("文字方塊", "常用", ControlType.TextBox, "txt", 150, 30, "");
        vm.Canvas.AddControlFromToolbox(tbItem, 50, 50);
        var codeWithTextBox = vm.GeneratedViewCode;
        codeWithTextBox.Should().Contain("new TextBox()");

        // Act: 刪除該選取的控制項
        vm.DeleteSelectedNodeCommand.Execute(null);

        // Assert: 預覽程式碼必須立即移除該控制項
        vm.GeneratedViewCode.Should().NotContain("new TextBox()");
    }

    [Fact]
    public void MainViewModel_ToggleCodePanel_ShouldToggleVisibility()
    {
        // Arrange
        var vm = new MainViewModel();
        var initialVisibility = vm.IsCodePanelVisible;

        // Act
        vm.ToggleCodePanelCommand.Execute(null);

        // Assert
        vm.IsCodePanelVisible.Should().Be(!initialVisibility);
    }
}
