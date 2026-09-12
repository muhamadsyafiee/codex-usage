namespace CodexUsage;
public static class DockGeometry
{
    public static (double Left, double Top) Place(string edge, double x, double y, double width, double height, double widgetWidth, double widgetHeight, double offset = 0.5)
    {
        offset = double.IsFinite(offset) ? Math.Clamp(offset, 0, 1) : 0.5;
        var left = x + Math.Max(0, width - widgetWidth) * offset;
        var top = y + Math.Max(0, height - widgetHeight) * offset;
        if (edge == "Kiri") left = x;
        if (edge == "Kanan") left = x + Math.Max(0, width - widgetWidth);
        if (edge == "Atas") top = y;
        if (edge == "Bawah") top = y + Math.Max(0, height - widgetHeight);
        return (left, top);
    }
}
