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
        vm.GeneratedViewCode.Should().Contain(".Text(\"Click Me\")");
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
        vm.GeneratedViewCode.Should().Contain(".Text((MainFormViewModel vm) => vm.UserName, BindingMode.TwoWay)");
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

    [Fact]
    public void MainViewModel_CustomResolutionDialog_ShouldApplyNewCanvasDimensions()
    {
        // Arrange
        var vm = new MainViewModel();
        vm.Canvas.CanvasWidth.Should().Be(800);
        vm.Canvas.CanvasHeight.Should().Be(600);

        // Act: 開啟對話框並輸入自訂寬高 (1440x900)
        vm.OpenCustomResolutionDialogCommand.Execute(null);
        vm.IsCustomResolutionDialogVisible.Should().BeTrue();
        vm.CustomResolutionWidth.Should().Be(800);
        vm.CustomResolutionHeight.Should().Be(600);

        vm.CustomResolutionWidth = 1440;
        vm.CustomResolutionHeight = 900;
        vm.ApplyCustomResolutionCommand.Execute(null);

        // Assert: 對話框關閉且畫布尺寸成功套用
        vm.IsCustomResolutionDialogVisible.Should().BeFalse();
        vm.Canvas.CanvasWidth.Should().Be(1440);
        vm.Canvas.CanvasHeight.Should().Be(900);
        vm.Canvas.Document.CanvasWidth.Should().Be(1440);
        vm.Canvas.Document.CanvasHeight.Should().Be(900);
    }

    [Fact]
    public void MainViewModel_ProjectNameDialog_ShouldApplyCustomProjectName()
    {
        // Arrange
        var vm = new MainViewModel();
        vm.Canvas.ExportProjectName.Should().Be("MainFormApp");

        // Act: 開啟專案名稱對話框並輸入新名稱 "PosSystem"
        vm.OpenProjectNameDialogCommand.Execute(null);
        vm.IsProjectNameDialogVisible.Should().BeTrue();
        vm.CustomProjectNameInput.Should().Be("MainFormApp");

        vm.CustomProjectNameInput = "PosSystem";
        vm.ApplyProjectNameCommand.Execute(null);

        // Assert: 對話框關閉且專案名稱成功套用
        vm.IsProjectNameDialogVisible.Should().BeFalse();
        vm.Canvas.ExportProjectName.Should().Be("PosSystem");
    }

    [Fact]
    public void CanvasResizeNode_ShouldSynchronouslyUpdateInspectorCoordinatesAndDimensions()
    {
        // Arrange
        var vm = new MainViewModel();
        var node = new AstNode
        {
            Id = "testBtn",
            Name = "MyButton",
            Type = ControlType.Button,
            Width = 120,
            Height = 35,
            CanvasLeft = 100,
            CanvasTop = 150
        };

        var rootWithNode = AstTreeOperations.AddChild(vm.Canvas.Document.RootNode, vm.Canvas.Document.RootNode.Id, node);
        vm.Canvas.Document = vm.Canvas.Document with { RootNode = rootWithNode };
        vm.Canvas.SelectNode("testBtn");

        // 驗證初始檢查器屬性
        vm.Inspector.Width.Should().Be(120);
        vm.Inspector.Height.Should().Be(35);
        vm.Inspector.CanvasLeft.Should().Be(100);
        vm.Inspector.CanvasTop.Should().Be(150);

        // Act: 模擬使用者使用 8 點縮放控制項變更尺寸與座標（自動吸附至 8px 網格）
        vm.Canvas.ResizeNode("testBtn", newWidth: 264, newHeight: 96, newLeft: 80, newTop: 120);

        // Assert: 屬性檢查器面板同步即時更新
        vm.Inspector.Width.Should().Be(264);
        vm.Inspector.Height.Should().Be(96);
        vm.Inspector.CanvasLeft.Should().Be(80);
        vm.Inspector.CanvasTop.Should().Be(120);
    }

    [Fact]
    public void ModifyingBindingInInspector_ShouldSynchronouslyUpdateGeneratedPreviewCode()
    {
        // Arrange
        var vm = new MainViewModel();
        var tbItem = new ToolboxItem("文字方塊", "常用", ControlType.TextBox, "txt", 150, 30, "");
        vm.Canvas.AddControlFromToolbox(tbItem, 50, 50);

        // Select the newly added TextBox
        var selected = vm.Canvas.SelectedNode;
        selected.Should().NotBeNull();
        vm.Inspector.HasSelectedNode.Should().BeTrue();

        // Act: Add and modify a binding via Inspector
        vm.Inspector.AddBindingCommand.Execute(null);
        vm.Inspector.Bindings.Should().HaveCount(1);
        vm.Inspector.Bindings[0].ViewModelProperty = "UserEmailAddress";

        // Assert: Code preview must immediately reflect the new binding
        vm.GeneratedViewCode.Should().Contain(".Text((MainFormViewModel vm) => vm.UserEmailAddress, BindingMode.TwoWay)");
        vm.GeneratedVmCode.Should().Contain("private string _userEmailAddress = string.Empty;");
    }

    [Fact]
    public void ModifyingEventInInspector_ShouldSynchronouslyUpdateGeneratedPreviewCode()
    {
        // Arrange
        var vm = new MainViewModel();
        var btnItem = new ToolboxItem("按鈕", "常用", ControlType.Button, "btn", 120, 35, "Submit");
        vm.Canvas.AddControlFromToolbox(btnItem, 50, 50);

        var selected = vm.Canvas.SelectedNode;
        selected.Should().NotBeNull();
        vm.Inspector.HasSelectedNode.Should().BeTrue();

        // Act: Add and modify an event via Inspector
        vm.Inspector.AddEventCommand.Execute(null);
        vm.Inspector.Events.Should().HaveCount(1);
        vm.Inspector.Events[0].CommandProperty = "PerformSubmitCommand";

        // Assert: Code preview must immediately reflect the new event command
        vm.GeneratedViewCode.Should().Contain(".OnClick((MainFormViewModel vm) => vm.PerformSubmitCommand)");
        vm.GeneratedVmCode.Should().Contain("private async Task PerformSubmitAsync(");
    }

    [Fact]
    public void ModifyingFormEventInInspector_ShouldSynchronouslyUpdateGeneratedPreviewCode()
    {
        // Arrange
        var vm = new MainViewModel();
        vm.Inspector.SelectFormCommand.Execute(null);
        vm.Inspector.IsFormSelected.Should().BeTrue();

        // Act: Add and modify a form event via Inspector
        vm.Inspector.AddFormEventCommand.Execute(null);
        vm.Inspector.FormEvents.Should().HaveCount(1);
        vm.Inspector.FormEvents[0].EventName = "Loaded";
        vm.Inspector.FormEvents[0].CommandProperty = "InitializeWindowCommand";

        // Assert: Code preview must immediately reflect the form event in View and ViewModel
        vm.GeneratedViewCode.Should().Contain("Loaded += (sender, e) => (DataContext as MainFormViewModel)?.InitializeWindowCommand.Execute(e);");
        vm.GeneratedVmCode.Should().Contain("private async Task InitializeWindowAsync(");
    }

    [Fact]
    public void TogglingFormGenerateCodeBehindFields_InInspector_ShouldSynchronouslyUpdateGeneratedPreviewCode()
    {
        // Arrange
        var vm = new MainViewModel();
        var tbItem = new ToolboxItem("文字方塊", "常用", ControlType.TextBox, "txtUsername", 150, 30, "");
        vm.Canvas.AddControlFromToolbox(tbItem, 50, 50);
        vm.Inspector.NodeName = "txtUsername";

        // 預設開啟 Code-Behind Friendly 模式，應包含欄位宣告與 Name 註冊
        vm.GeneratedViewCode.Should().Contain("private TextBox _txtUsername;");
        vm.GeneratedViewCode.Should().Contain("_txtUsername = new TextBox()");
        vm.GeneratedViewCode.Should().Contain(".Name(\"txtUsername\")");

        // Act: 在 Inspector 中切換開關關閉 Code-Behind 欄位
        vm.Inspector.FormGenerateCodeBehindFields = false;

        // Assert: 預覽程式碼必須立即更新為純 MVVM 模式（不包含欄位宣告，維持 Inline）
        vm.GeneratedViewCode.Should().NotContain("private TextBox _txtUsername;");
        vm.GeneratedViewCode.Should().NotContain("_txtUsername = new TextBox()");
        vm.GeneratedViewCode.Should().Contain("new TextBox()");

        // Act 2: 再次切換開啟
        vm.Inspector.FormGenerateCodeBehindFields = true;

        // Assert 2: 預覽程式碼必須立即恢復欄位宣告
        vm.GeneratedViewCode.Should().Contain("private TextBox _txtUsername;");
        vm.GeneratedViewCode.Should().Contain("_txtUsername = new TextBox()");
        vm.GeneratedViewCode.Should().Contain(".Name(\"txtUsername\")");
    }

    [Fact]
    public void Inspector_ChangingArchitectureMode_ShouldUpdatePreviewCodeImmediately()
    {
        // Arrange
        var vm = new MainViewModel();
        var tbItem = new ToolboxItem("按鈕", "常用", ControlType.Button, "btnSubmit", 100, 35, "送出");
        vm.Canvas.AddControlFromToolbox(tbItem, 50, 50);
        vm.Inspector.NodeName = "btnSubmit";
        vm.Inspector.AddEventCommand.Execute(null);
        var evtItem = vm.Inspector.Events[0];
        evtItem.EventName = "Click";
        evtItem.CommandProperty = "SubmitCommand";

        // Hybrid 模式預設
        vm.GeneratedViewCode.Should().Contain("private Button _btnSubmit;");
        vm.GeneratedViewCode.Should().Contain(".OnClick((MainFormViewModel vm) => vm.SubmitCommand)");
        vm.GeneratedVmCode.Should().Contain("[RelayCommand]");

        // Act 1: 切換至 Code-Behind 模式
        vm.Inspector.FormArchitectureMode = ArchitectureMode.CodeBehind;

        // Assert 1: View 應切換為直接事件處理常式與 Stub，Vm 代碼提示不使用 ViewModel
        vm.GeneratedViewCode.Should().Contain("private Button _btnSubmit;");
        vm.GeneratedViewCode.Should().Contain(".OnClick(BtnSubmit_Click)");
        vm.GeneratedViewCode.Should().Contain("BtnSubmit_Click(object? sender, RoutedEventArgs e)");
        vm.GeneratedVmCode.Should().Contain("Code-Behind / Event-Driven 模式不使用 ViewModel");

        // Act 2: 切換至 Pure MVVM 模式
        vm.Inspector.FormArchitectureMode = ArchitectureMode.PureMvvm;

        // Assert 2: View 應不含欄位宣告，維持 Inline
        vm.GeneratedViewCode.Should().NotContain("private Button _btnSubmit;");
        vm.GeneratedViewCode.Should().Contain(".OnClick((MainFormViewModel vm) => vm.SubmitCommand)");
        vm.GeneratedVmCode.Should().Contain("[RelayCommand]");
    }
}
