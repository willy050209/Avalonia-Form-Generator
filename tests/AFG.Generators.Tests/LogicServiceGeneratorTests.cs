// filepath: tests/AFG.Generators.Tests/LogicServiceGeneratorTests.cs
using System.Collections.Immutable;
using AFG.Core.Enums;
using AFG.Core.Models.Logic;
using AFG.Generators.Logic;
using FluentAssertions;
using Xunit;

namespace AFG.Generators.Tests;

public class LogicServiceGeneratorTests
{
    [Fact]
    public void CSharpLogicGenerator_WhenGeneratingSyncAndAsyncMethods_ShouldGenerateValidInterfaceAndClass()
    {
        // Arrange
        var service = new LogicServiceDefinition
        {
            ServiceName = "OrderCalculationService",
            Namespace = "ECommerce.Services",
            Description = "處理訂單金額與折扣計算之業務邏輯服務",
            Functions =
            [
                new LogicFunctionDefinition
                {
                    Name = "CalculateTotal",
                    ReturnType = "decimal",
                    IsAsync = false,
                    Description = "計算訂單總金額",
                    Parameters =
                    [
                        new FunctionParameter { Name = "unitPrice", Type = "decimal", Description = "單價" },
                        new FunctionParameter { Name = "quantity", Type = "int", Description = "數量" },
                        new FunctionParameter { Name = "discountRate", Type = "decimal", DefaultValue = "0m", Description = "折扣率" }
                    ],
                    CustomImplementation = "return (unitPrice * quantity) * (1m - discountRate);"
                },
                new LogicFunctionDefinition
                {
                    Name = "ProcessPayment",
                    ReturnType = "bool",
                    IsAsync = true,
                    Description = "執行非同步付款交易",
                    Parameters =
                    [
                        new FunctionParameter { Name = "orderId", Type = "string", Description = "訂單編號" },
                        new FunctionParameter { Name = "amount", Type = "decimal", Description = "扣款金額" }
                    ]
                }
            ]
        };

        // Act
        var (iface, impl) = CSharpLogicGenerator.Generate(service);

        // Assert
        iface.FileName.Should().Be("IOrderCalculationService.cs");
        iface.Content.Should().Contain("namespace ECommerce.Services;");
        iface.Content.Should().Contain("public interface IOrderCalculationService");
        iface.Content.Should().Contain("decimal CalculateTotal(decimal unitPrice, int quantity, decimal discountRate = 0m);");
        iface.Content.Should().Contain("Task<bool> ProcessPaymentAsync(string orderId, decimal amount, CancellationToken cancellationToken = default);");

        impl.FileName.Should().Be("OrderCalculationService.cs");
        impl.Content.Should().Contain("public class OrderCalculationService : IOrderCalculationService");
        impl.Content.Should().Contain("return (unitPrice * quantity) * (1m - discountRate);");
        impl.Content.Should().Contain("public async Task<bool> ProcessPaymentAsync(");
    }

    [Fact]
    public void FSharpLogicGenerator_WhenGeneratingService_ShouldGenerateValidFSharpCode()
    {
        // Arrange
        var service = new LogicServiceDefinition
        {
            ServiceName = "FinanceCalculator",
            Namespace = "Finance.Core",
            Functions =
            [
                new LogicFunctionDefinition
                {
                    Name = "AddNumbers",
                    ReturnType = "int",
                    IsAsync = false,
                    Parameters =
                    [
                        new FunctionParameter { Name = "a", Type = "int" },
                        new FunctionParameter { Name = "b", Type = "int" }
                    ],
                    CustomImplementation = "a + b"
                },
                new LogicFunctionDefinition
                {
                    Name = "FetchRates",
                    ReturnType = "string",
                    IsAsync = true,
                    Parameters =
                    [
                        new FunctionParameter { Name = "currency", Type = "string" }
                    ]
                }
            ]
        };

        // Act
        var file = FSharpLogicGenerator.Generate(service);

        // Assert
        file.FileName.Should().Be("FinanceCalculator.fs");
        file.Content.Should().Contain("namespace Finance.Core");
        file.Content.Should().Contain("type IFinanceCalculator =");
        file.Content.Should().Contain("abstract member AddNumbers : a: int * b: int -> int");
        file.Content.Should().Contain("abstract member FetchRatesAsync : currency: string * ?cancellationToken: CancellationToken -> Task<string>");
        file.Content.Should().Contain("type FinanceCalculator() =");
        file.Content.Should().Contain("interface IFinanceCalculator with");
        file.Content.Should().Contain("a + b");
        file.Content.Should().Contain("task {");
    }

    [Fact]
    public void VisualBasicLogicGenerator_WhenGeneratingService_ShouldGenerateValidVBCode()
    {
        // Arrange
        var service = new LogicServiceDefinition
        {
            ServiceName = "InventoryService",
            Namespace = "Warehouse.Logic",
            Functions =
            [
                new LogicFunctionDefinition
                {
                    Name = "CheckStock",
                    ReturnType = "int",
                    IsAsync = false,
                    Parameters =
                    [
                        new FunctionParameter { Name = "sku", Type = "string" }
                    ]
                },
                new LogicFunctionDefinition
                {
                    Name = "ReserveStock",
                    ReturnType = "void",
                    IsAsync = true,
                    Parameters =
                    [
                        new FunctionParameter { Name = "sku", Type = "string" },
                        new FunctionParameter { Name = "count", Type = "int" }
                    ]
                }
            ]
        };

        // Act
        var (iface, impl) = VisualBasicLogicGenerator.Generate(service);

        // Assert
        iface.FileName.Should().Be("IInventoryService.vb");
        iface.Content.Should().Contain("Namespace Warehouse.Logic");
        iface.Content.Should().Contain("Public Interface IInventoryService");
        iface.Content.Should().Contain("Function CheckStock(ByVal sku As String) As Integer");
        iface.Content.Should().Contain("Function ReserveStockAsync(ByVal sku As String, ByVal count As Integer, Optional ByVal cancellationToken As CancellationToken = Nothing) As Task");

        impl.FileName.Should().Be("InventoryService.vb");
        impl.Content.Should().Contain("Public Class InventoryService");
        impl.Content.Should().Contain("Implements IInventoryService");
        impl.Content.Should().Contain("Public Async Function ReserveStockAsync");
    }

    [Fact]
    public void CppLogicGenerator_WhenGeneratingNativeAndBridges_ShouldGenerateHeadersAndDllImports()
    {
        // Arrange
        var service = new LogicServiceDefinition
        {
            ServiceName = "NativeCrypto",
            Namespace = "Security.Native",
            Functions =
            [
                new LogicFunctionDefinition
                {
                    Name = "EncryptData",
                    ReturnType = "int",
                    IsAsync = false,
                    Parameters =
                    [
                        new FunctionParameter { Name = "key", Type = "int" },
                        new FunctionParameter { Name = "data", Type = "int" }
                    ],
                    CustomImplementation = "return key ^ data;"
                },
                new LogicFunctionDefinition
                {
                    Name = "HashAsync",
                    ReturnType = "int",
                    IsAsync = true,
                    Parameters =
                    [
                        new FunctionParameter { Name = "input", Type = "int" }
                    ]
                }
            ]
        };

        // Act
        var (header, cpp, cmake) = CppLogicGenerator.GenerateNative(service, "SecurityNativeLib");
        var csBridge = CppLogicGenerator.GenerateCSharpBridge(service, "SecurityNativeLib");
        var fsBridge = CppLogicGenerator.GenerateFSharpBridge(service, "SecurityNativeLib");
        var vbBridge = CppLogicGenerator.GenerateVisualBasicBridge(service, "SecurityNativeLib");

        // Assert
        header.FileName.Should().Be("NativeCrypto.h");
        header.Content.Should().Contain("AFG_API int32_t NativeCrypto_EncryptData(int32_t key, int32_t data);");
        cpp.FileName.Should().Be("NativeCrypto.cpp");
        cpp.Content.Should().Contain("return key ^ data;");
        cmake.FileName.Should().Be("CMakeLists.txt");
        cmake.Content.Should().Contain("add_library(SecurityNativeLib SHARED NativeCrypto.cpp)");

        csBridge.FileName.Should().Be("NativeCryptoNativeBridge.cs");
        csBridge.Content.Should().Contain("[DllImport(LibName, EntryPoint = \"NativeCrypto_EncryptData\", CallingConvention = CallingConvention.Cdecl)]");
        csBridge.Content.Should().Contain("public class NativeCryptoNativeBridge : INativeCrypto");

        fsBridge.FileName.Should().Be("NativeCryptoNativeBridge.fs");
        fsBridge.Content.Should().Contain("[<DllImport(LibName, EntryPoint = \"NativeCrypto_EncryptData\", CallingConvention = CallingConvention.Cdecl)>]");

        vbBridge.FileName.Should().Be("NativeCryptoNativeBridge.vb");
        vbBridge.Content.Should().Contain("<DllImport(LibName, EntryPoint:=\"NativeCrypto_EncryptData\", CallingConvention:=CallingConvention.Cdecl)>");
    }
}
