// filepath: tests/AFG.Generators.Tests/VisualBasicGeneratorTests.cs
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Generators.VisualBasic;
using FluentAssertions;
using Xunit;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 Visual Basic (.NET) View 與 ViewModel 程式碼生成器正確性。
/// </summary>
public sealed class VisualBasicGeneratorTests
{
    private readonly VisualBasicViewGenerator _viewGen = new();
    private readonly VisualBasicViewModelGenerator _vmGen = new();

    [Fact]
    public void Generate_VisualBasicView_ShouldProduceValidClassAndWithEventsFields()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "InvoiceView",
            ViewModelClassName = "InvoiceViewModel",
            Title = "Invoice System",
            CanvasWidth = 1024,
            CanvasHeight = 768,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Name = "BtnPrint",
                        Type = ControlType.Button,
                        Content = "Print Invoice",
                        CanvasLeft = 50,
                        CanvasTop = 60,
                        Width = 140,
                        Height = 45,
                        Bindings = [new BindingDefinition { TargetProperty = "Content", ViewModelProperty = "PrintButtonText" }],
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "PrintCommand" }]
                    }
                ]
            }
        };

        // Act
        var result = _viewGen.Generate(doc);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("InvoiceView.vb");
        result.Content.Should().Contain("Public Class InvoiceView");
        result.Content.Should().Contain("Inherits UserControl");
        result.Content.Should().Contain("Private WithEvents _btnPrint As Button");
        result.Content.Should().Contain("Me.Width = 1024");
        result.Content.Should().Contain("Me.Height = 768");
        result.Content.Should().Contain("Dim rootControl As New Canvas()");
        result.Content.Should().Contain("rootControl.Children.Add(rootControl_child1)");
        result.Content.Should().Contain("rootControl_child1.Bind(Button.ContentProperty, New Binding(\"PrintButtonText\"))");
        result.Content.Should().Contain("rootControl_child1.Bind(Button.CommandProperty, New Binding(\"PrintCommand\"))");
    }

    [Fact]
    public void Generate_VisualBasicViewModel_ShouldProduceObservablePropertiesAndCommands()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "CustomerView",
            ViewModelClassName = "CustomerViewModel",
            InjectedServices = [new ServiceDependencyDefinition("ICustomerService")],
            RootNode = new AstNode
            {
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Type = ControlType.TextBox,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "CustomerName" }]
                    },
                    new AstNode
                    {
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SearchCommand", IsAsync = false }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _vmGen.Generate(doc);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("CustomerViewModel.vb");
        result.Content.Should().Contain("Public Class CustomerViewModel");
        result.Content.Should().Contain("Inherits ObservableObject");
        result.Content.Should().Contain("Private ReadOnly _customerService As ICustomerService");
        result.Content.Should().Contain("Private _customerName As String = \"\"");
        result.Content.Should().Contain("Public Property CustomerName As String");
        result.Content.Should().Contain("SetProperty(_customerName, value)");
        result.Content.Should().Contain("Public ReadOnly Property SearchCommand As IRelayCommand = New RelayCommand(AddressOf Search)");
        result.Content.Should().Contain("Private Sub Search()");
    }
}
