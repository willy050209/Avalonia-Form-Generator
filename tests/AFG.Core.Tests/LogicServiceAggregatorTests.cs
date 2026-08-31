// filepath: tests/AFG.Core.Tests/LogicServiceAggregatorTests.cs
using System.Collections.Immutable;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Logic;
using FluentAssertions;

namespace AFG.Core.Tests;

public class LogicServiceAggregatorTests
{
    [Fact]
    public void AggregateFromNodes_WhenSameOutputPathServiceNameAndLanguage_ShouldMergeIntoOneService()
    {
        // Arrange: 兩個具有相同 OutputPath、ServiceName 與 TargetLanguage 的 LogicFunction 節點
        var node1 = new AstNode
        {
            Id = "fn1",
            Name = "OrderService",
            Type = ControlType.LogicFunction,
            OutputPath = "Services",
            TargetNamespace = "MyApp.Services",
            TargetLanguage = TargetLanguage.CSharp,
            LogicFunction = new LogicFunctionDefinition
            {
                Name = "CalculateDiscount",
                ReturnType = "decimal",
                Parameters = [new FunctionParameter { Name = "amount", Type = "decimal" }]
            }
        };

        var node2 = new AstNode
        {
            Id = "fn2",
            Name = "OrderService",
            Type = ControlType.LogicFunction,
            OutputPath = "Services",
            TargetNamespace = "MyApp.Services",
            TargetLanguage = TargetLanguage.CSharp,
            LogicFunction = new LogicFunctionDefinition
            {
                Name = "CalculateTax",
                ReturnType = "decimal",
                Parameters = [new FunctionParameter { Name = "amount", Type = "decimal" }]
            }
        };

        // Act
        var result = LogicServiceAggregator.AggregateFromNodes([node1, node2]);

        // Assert: 應合併為單一 ServiceDefinition，包含兩個函數
        result.Should().HaveCount(1);
        var svc = result[0];
        svc.ServiceName.Should().Be("OrderService");
        svc.Namespace.Should().Be("MyApp.Services");
        svc.Language.Should().Be(TargetLanguage.CSharp);
        svc.Functions.Should().HaveCount(2);
        svc.Functions.Select(f => f.Name).Should().Contain(["CalculateDiscount", "CalculateTax"]);
    }

    [Fact]
    public void AggregateFromNodes_WhenDifferentOutputPathOrLanguage_ShouldGenerateSeparateServices()
    {
        // Arrange: 兩個不同語言或不同路徑的節點
        var node1 = new AstNode
        {
            Id = "fn1",
            Name = "PaymentService",
            Type = ControlType.LogicFunction,
            OutputPath = "Services/Payment",
            TargetNamespace = "MyApp.Payment",
            TargetLanguage = TargetLanguage.CSharp,
            LogicFunction = new LogicFunctionDefinition
            {
                Name = "ProcessPayment",
                ReturnType = "bool",
                Parameters = [new FunctionParameter { Name = "orderId", Type = "string" }]
            }
        };

        var node2 = new AstNode
        {
            Id = "fn2",
            Name = "PaymentService",
            Type = ControlType.LogicFunction,
            OutputPath = "Services/PaymentFSharp",
            TargetNamespace = "MyApp.PaymentFS",
            TargetLanguage = TargetLanguage.FSharp,
            LogicFunction = new LogicFunctionDefinition
            {
                Name = "AuditTransaction",
                ReturnType = "bool",
                Parameters = [new FunctionParameter { Name = "orderId", Type = "string" }]
            }
        };

        // Act
        var result = LogicServiceAggregator.AggregateFromNodes([node1, node2]);

        // Assert: 應產出兩個獨立的服務
        result.Should().HaveCount(2);
        result.Any(s => s.Language == TargetLanguage.CSharp && s.Namespace == "MyApp.Payment").Should().BeTrue();
        result.Any(s => s.Language == TargetLanguage.FSharp && s.Namespace == "MyApp.PaymentFS").Should().BeTrue();
    }

    [Fact]
    public void AggregateWithOutputPath_ShouldPreserveOutputPath()
    {
        // Arrange
        var root = new AstNode
        {
            Type = ControlType.Canvas,
            Children = [
                new AstNode
                {
                    Id = "fn1",
                    Name = "CryptoNative",
                    Type = ControlType.LogicFunction,
                    OutputPath = "Native/Crypto",
                    TargetNamespace = "Security.Native",
                    TargetLanguage = TargetLanguage.Cpp,
                    LogicFunction = new LogicFunctionDefinition
                    {
                        Name = "FastHash",
                        ReturnType = "string",
                        Parameters = [new FunctionParameter { Name = "input", Type = "string" }]
                    }
                }
            ]
        };

        // Act
        var result = LogicServiceAggregator.AggregateWithOutputPath(root);

        // Assert
        result.Should().HaveCount(1);
        result[0].OutputPath.Should().Be("Native/Crypto");
        result[0].Service.ServiceName.Should().Be("CryptoNative");
        result[0].Service.Language.Should().Be(TargetLanguage.Cpp);
        result[0].Service.Functions.Should().HaveCount(1);
        result[0].Service.Functions[0].Name.Should().Be("FastHash");
    }
}
