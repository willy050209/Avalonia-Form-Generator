// filepath: src/AFG.Core/Enums/ArchitectureMode.cs
namespace AFG.Core.Enums;

/// <summary>
/// 表示應用程式與表單程式碼生成之架構模式。
/// </summary>
public enum ArchitectureMode
{
    /// <summary>
    /// 混合模式（預設模式）：產出 View + ViewModel，事件轉化為 RelayCommand 與 ObservableProperty，同時在 View 生成強型別私有欄位與 NameScope 註冊。
    /// </summary>
    Hybrid = 0,

    /// <summary>
    /// Code-Behind / Event-Driven 模式（快速驗證 / WinForms 習慣）：僅產出 View，直接綁定事件處理器至 View 內部方法，不產出 ViewModel。
    /// </summary>
    CodeBehind = 1,

    /// <summary>
    /// Pure MVVM 模式（標準架構 / 正式開發）：產出 View + ViewModel，View 內維持純 Inline 宣告（不宣告強型別私有欄位）。
    /// </summary>
    PureMvvm = 2
}
