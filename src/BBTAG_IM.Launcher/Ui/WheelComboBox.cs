using Avalonia.Controls;
using Avalonia.Input;
using BBTAG_IM.Core.Selection;

namespace BBTAG_IM.Launcher.Ui;

public sealed class WheelComboBox : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (IsDropDownOpen || e.Handled)
        {
            base.OnPointerWheelChanged(e);
            return;
        }

        SelectedIndex = WheelChoice.Next(SelectedIndex, ItemCount, WheelChoice.Steps((float)e.Delta.Y));
        e.Handled = true;
    }
}
