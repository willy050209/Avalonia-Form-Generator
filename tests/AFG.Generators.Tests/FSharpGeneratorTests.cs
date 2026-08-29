// filepath: tests/AFG.Generators.Tests/FSharpGeneratorTests.cs
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Generators.FSharp;
using FluentAssertions;
using Xunit;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 F# View 與 ViewModel 程式碼生成器正確性。
/// </summary>
public sealed class FSharpGeneratorTests
{
    private readonly FSharpViewGenerator _viewGen = new();
    private readonly FSharpViewModelGenerator _vmGen = new();

    [Fact]
    public void Generate_FSharpView_ShouldProduceValidTypeDefinitionAndControlTree()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "OrderView",
            ViewModelClassName = "OrderViewModel",
            Title = "Order System",
            CanvasWidth = 800,
            CanvasHeight = 600,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Name = "BtnSubmit",
                        Type = ControlType.Button,
                        Content = "Submit Order",
                        CanvasLeft = 100,
                        CanvasTop = 150,
                        Width = 120,
                        Height = 40,
                        Bindings = [new BindingDefinition { TargetProperty = "Content", ViewModelProperty = "SubmitButtonText" }],
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
                    }
                ]
            }
        };

        // Act
        var result = _viewGen.Generate(doc);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("OrderView.fs");
        result.Content.Should().Contain("type OrderView() as this =");
        result.Content.Should().Contain("inherit UserControl()");
        result.Content.Should().Contain("let mutable _btnSubmit : Button = null");
        result.Content.Should().Contain("this.Width <- 800");
        result.Content.Should().Contain("this.Height <- 600");
        result.Content.Should().Contain("let rootControl = Canvas()");
        result.Content.Should().Contain("rootControl.Children.Add(rootControl_child1) |> ignore");
        result.Content.Should().Contain("rootControl_child1.Bind(Button.ContentProperty, Binding(\"SubmitButtonText\")) |> ignore");
        result.Content.Should().Contain("rootControl_child1.Bind(Button.CommandProperty, Binding(\"SubmitCommand\")) |> ignore");
    }

    [Fact]
    public void Generate_FSharpViewModel_ShouldProduceObservablePropertiesAndCommands()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "UserView",
            ViewModelClassName = "UserViewModel",
            InjectedServices = [new ServiceDependencyDefinition("IGreetingService")],
            RootNode = new AstNode
            {
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Type = ControlType.TextBox,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "UserName" }]
                    },
                    new AstNode
                    {
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveCommand", IsAsync = true }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _vmGen.Generate(doc);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("UserViewModel.fs");
        result.Content.Should().Contain("type UserViewModel(greetingService: IGreetingService) as this =");
        result.Content.Should().Contain("inherit ObservableObject()");
        result.Content.Should().Contain("let mutable _userName : string = \"\"");
        result.Content.Should().Contain("member this.UserName");
        result.Content.Should().Contain("this.SetProperty(&_userName, value) |> ignore");
        result.Content.Should().Contain("member this.SaveCommand = AsyncRelayCommand(fun () -> this.SaveAsync())");
        result.Content.Should().Contain("member private this.SaveAsync() =");
    }
}
