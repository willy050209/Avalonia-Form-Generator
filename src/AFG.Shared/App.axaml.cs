// filepath: src/AFG.Shared/App.axaml.cs
using Avalonia.Markup.Xaml;

namespace AFG.Shared;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
