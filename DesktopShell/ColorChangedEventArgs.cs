namespace DesktopShell;

public class ColorChangedEventArgs : EventArgs
{
    public required ColorHandler.RGB RGB { get; init; }
    public required ColorHandler.HSV HSV { get; init; }
}