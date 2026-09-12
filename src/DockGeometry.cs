namespace CodexUsage;
public static class DockGeometry
{
    public static (double Left, double Top) Place(string edge, double x, double y, double width, double height, double widgetWidth, double widgetHeight)
    {
        var left = x + Math.Max(0, (width - widgetWidth) / 2);
        var top = y + Math.Max(0, (height - widgetHeight) / 2);
        if (edge == "Kiri") left = x;
        if (edge == "Kanan") left = x + Math.Max(0, width - widgetWidth);
        if (edge == "Atas") top = y;
        if (edge == "Bawah") top = y + Math.Max(0, height - widgetHeight);
        return (left, top);
    }
}
