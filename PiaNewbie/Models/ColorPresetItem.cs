using CommunityToolkit.Mvvm.ComponentModel;

namespace PiaNewbie.Models;

public partial class ColorPresetItem : ObservableObject
{
    public required string Name { get; init; }
    public required string Hex { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}
