// filepath: tests/AFG.Generators.Tests/PackageVersionsTests.cs
using AFG.Generators.Constants;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證集中套件版本管理常數之有效性。
/// </summary>
public sealed class PackageVersionsTests
{
    [Fact]
    public void PackageVersions_ShouldContainValidVersionStrings()
    {
        PackageVersions.Avalonia.Should().NotBeNullOrWhiteSpace();
        PackageVersions.CommunityToolkitMvvm.Should().NotBeNullOrWhiteSpace();
        PackageVersions.MicrosoftExtensionsDependencyInjection.Should().NotBeNullOrWhiteSpace();

        Version.TryParse(PackageVersions.Avalonia, out _).Should().BeTrue();
        Version.TryParse(PackageVersions.CommunityToolkitMvvm, out _).Should().BeTrue();
        Version.TryParse(PackageVersions.MicrosoftExtensionsDependencyInjection, out _).Should().BeTrue();
    }
}
