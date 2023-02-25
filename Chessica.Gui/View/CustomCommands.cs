using System.Windows.Input;

namespace Chessica.Gui.View;

public static class CustomCommands
{
    public static readonly RoutedUICommand Exit = new(
        "Exit",
        "Exit",
        typeof(CustomCommands),
        new InputGestureCollection()
        {
            new KeyGesture(Key.F4, ModifierKeys.Alt)
        }
    );

    public static readonly RoutedUICommand Undo = new(
        "Undo Last Full Move",
        "Undo",
        typeof(CustomCommands),
        new InputGestureCollection
        {
            new KeyGesture(Key.Z, ModifierKeys.Control)
        });
}