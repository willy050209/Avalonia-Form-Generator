// filepath: tests/AFG.Core.Tests/TestInitializer.cs
using System.Runtime.CompilerServices;

namespace AFG.Core.Tests;

internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
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
}
