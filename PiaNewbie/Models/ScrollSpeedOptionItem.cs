using CommunityToolkit.Mvvm.ComponentModel;

namespace PiaNewbie.Models;

public partial class ScrollSpeedOptionItem : ObservableObject
{
    public required double Value { get; init; }
    public required string Label { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}
