// filepath: src/AFG.Core/Enums/TargetLanguage.cs
namespace AFG.Core.Enums;

/// <summary>
/// 表示程式碼生成與專案匯出支援的目標程式語言。
/// </summary>
public enum TargetLanguage
{
    /// <summary>
    /// C# (.cs, .csproj)
    /// </summary>
    CSharp = 0,

    /// <summary>
    /// F# (.fs, .fsproj)
    /// </summary>
    FSharp = 1,

    /// <summary>
    /// Visual Basic (.vb, .vbproj)
    /// </summary>
    VisualBasic = 2,

    /// <summary>
    /// C++ (.h, .cpp, CMake)
    /// </summary>
    Cpp = 3
}
