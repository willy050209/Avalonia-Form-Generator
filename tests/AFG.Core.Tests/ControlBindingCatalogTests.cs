// filepath: tests/AFG.Core.Tests/ControlBindingCatalogTests.cs
using System.Linq;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using FluentAssertions;
using Xunit;

namespace AFG.Core.Tests;

public sealed class ControlBindingCatalogTests
{
    [Fact]
    public void MediaPlayer_ShouldIncludeAllDedicatedProperties_AndExcludeIrrelevantProperties()
    {
        // Act
        var props = ControlBindingCatalog.GetSupportedProperties(ControlType.MediaPlayer);

        // Assert: 必須包含 MediaPlayer 專屬屬性
        props.Should().Contain("Source");
        props.Should().Contain("AutoPlay");
        props.Should().Contain("IsLooping");
        props.Should().Contain("Volume");
        props.Should().Contain("Position");
        props.Should().Contain("Duration");
        props.Should().Contain("State");
        props.Should().Contain("CurrentFrame");
        props.Should().Contain("Stretch");
        props.Should().Contain("SpeedRatio");
        props.Should().Contain("IsEnabled");
        props.Should().Contain("IsVisible");

        // 必須屏蔽不存在的屬性
        props.Should().NotContain("Text");
        props.Should().NotContain("Content");
        props.Should().NotContain("Watermark");
        props.Should().NotContain("Header");
        props.Should().NotContain("IsChecked");
        props.Should().NotContain("ItemsSource");
    }

    [Fact]
    public void TextBox_ShouldIncludeWatermarkAndText_AndExcludeMediaPlayerProperties()
    {
        // Act
        var props = ControlBindingCatalog.GetSupportedProperties(ControlType.TextBox);

        // Assert
        props.Should().Contain("Text");
        props.Should().Contain("Watermark");
        props.Should().Contain("FontSize");
        props.Should().NotContain("AutoPlay");
        props.Should().NotContain("CurrentFrame");
        props.Should().NotContain("IsChecked");
    }

    [Fact]
    public void Button_ShouldIncludeTextAndContent_AndExcludeImageAndTimerProperties()
    {
        // Act
        var props = ControlBindingCatalog.GetSupportedProperties(ControlType.Button);

        // Assert
        props.Should().Contain("Text");
        props.Should().Contain("Content");
        props.Should().NotContain("Source");
        props.Should().NotContain("CurrentFrame");
        props.Should().NotContain("Interval");
    }

    [Fact]
    public void DispatcherTimer_ShouldOnlyIncludeIntervalAndIsEnabled()
    {
        // Act
        var props = ControlBindingCatalog.GetSupportedProperties(ControlType.DispatcherTimer);

        // Assert
        props.Should().Equal(["Interval", "IsEnabled"]);
    }

    [Fact]
    public void IsPropertySupported_ShouldValidateCorrectly()
    {
        ControlBindingCatalog.IsPropertySupported(ControlType.MediaPlayer, "AutoPlay").Should().BeTrue();
        ControlBindingCatalog.IsPropertySupported(ControlType.MediaPlayer, "CurrentFrame").Should().BeTrue();
        ControlBindingCatalog.IsPropertySupported(ControlType.MediaPlayer, "Text").Should().BeFalse();

        ControlBindingCatalog.IsPropertySupported(ControlType.TextBox, "Watermark").Should().BeTrue();
        ControlBindingCatalog.IsPropertySupported(ControlType.TextBox, "AutoPlay").Should().BeFalse();

        ControlBindingCatalog.IsPropertySupported(ControlType.CheckBox, "IsChecked").Should().BeTrue();
        ControlBindingCatalog.IsPropertySupported(ControlType.CheckBox, "Value").Should().BeFalse();
    }

    [Fact]
    public void CurrentFrame_BindingToBoolOrInt_ShouldBeIncompatible()
    {
        // CurrentFrame 不能綁定 bool 或 int 或 string
        ControlBindingCatalog.IsDataTypeCompatible("CurrentFrame", "bool", ControlType.MediaPlayer).Should().BeFalse();
        ControlBindingCatalog.IsDataTypeCompatible("CurrentFrame", "int", ControlType.MediaPlayer).Should().BeFalse();
        ControlBindingCatalog.IsDataTypeCompatible("CurrentFrame", "string", ControlType.MediaPlayer).Should().BeFalse();

        // CurrentFrame 可以綁定 Bitmap 或 IImage
        ControlBindingCatalog.IsDataTypeCompatible("CurrentFrame", "Avalonia.Media.Imaging.Bitmap", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("CurrentFrame", "Avalonia.Media.IImage", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("CurrentFrame", "Bitmap?", ControlType.MediaPlayer).Should().BeTrue();
    }

    [Fact]
    public void IsChecked_BindingToImageOrString_ShouldBeIncompatible()
    {
        // IsChecked 只能綁定 bool
        ControlBindingCatalog.IsDataTypeCompatible("IsChecked", "Avalonia.Media.IImage", ControlType.CheckBox).Should().BeFalse();
        ControlBindingCatalog.IsDataTypeCompatible("IsChecked", "string", ControlType.CheckBox).Should().BeFalse();
        ControlBindingCatalog.IsDataTypeCompatible("IsChecked", "bool", ControlType.CheckBox).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("IsChecked", "bool?", ControlType.CheckBox).Should().BeTrue();
    }

    [Fact]
    public void AutoPlay_And_IsLooping_ShouldOnlyAcceptBool()
    {
        ControlBindingCatalog.IsDataTypeCompatible("AutoPlay", "bool", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("AutoPlay", "int", ControlType.MediaPlayer).Should().BeFalse();
        ControlBindingCatalog.IsDataTypeCompatible("IsLooping", "bool", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("IsLooping", "string", ControlType.MediaPlayer).Should().BeFalse();
    }

    [Fact]
    public void Position_And_Duration_ShouldOnlyAcceptTimeSpan()
    {
        ControlBindingCatalog.IsDataTypeCompatible("Position", "TimeSpan", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("Position", "bool", ControlType.MediaPlayer).Should().BeFalse();
        ControlBindingCatalog.IsDataTypeCompatible("Duration", "TimeSpan", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("Duration", "double", ControlType.MediaPlayer).Should().BeFalse();
    }

    [Fact]
    public void State_ShouldAcceptMediaStateOrString()
    {
        ControlBindingCatalog.IsDataTypeCompatible("State", "AFG.Core.Enums.MediaState", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("State", "MediaState", ControlType.MediaPlayer).Should().BeTrue();
        ControlBindingCatalog.IsDataTypeCompatible("State", "bool", ControlType.MediaPlayer).Should().BeFalse();
    }
}
