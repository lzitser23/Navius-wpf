namespace Navius.Wpf.Ui.Carousel;

/// <summary>
/// Pure index math, zero WPF dependency (same testing precedent as AutocompleteEngine/
/// PaginationEngine/SidebarNavigation). Unlike those (which default to no wrap), a carousel is
/// conventionally cyclic, so <paramref name="loop"/> defaults to true here.
/// </summary>
public static class CarouselEngine
{
    public static int MoveIndex(int current, int count, int delta, bool loop = true)
    {
        if (count <= 0)
        {
            return -1;
        }

        var next = current + delta;

        if (loop)
        {
            return ((next % count) + count) % count;
        }

        if (next < 0)
        {
            return 0;
        }

        return next > count - 1 ? count - 1 : next;
    }
}
