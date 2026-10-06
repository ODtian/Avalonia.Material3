namespace Avalonia.Material3.Controls;

/// <summary>Outlined segmented options supporting single or multiple selection.</summary>
public class MaterialSegmentedButtonGroup : MaterialButtonGroup
{
    public MaterialSegmentedButtonGroup()
    {
        SelectionMode = MaterialGroupSelectionMode.Single;
        AllowEmptySelection = false;
    }
}
