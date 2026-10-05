using System.Windows;
using LookUp.UI;

namespace LookUp.Tests;

public sealed class ScreenPlacementTests
{
    static readonly ScreenPlacement.Monitor Screen = new(new Rect(0, 0, 1920, 1040), 1.0);
    static readonly Size Popup = new(420, 540);

    [Fact]
    public void Opens_just_below_the_cursor_when_there_is_room()
    {
        var rect = ScreenPlacement.NearCursor(new Point(500, 300), Popup, Screen);
        Assert.Equal(476, rect.X);
        Assert.Equal(322, rect.Y);
    }

    [Fact]
    public void Flips_above_the_cursor_near_the_bottom_so_the_word_stays_visible()
    {
        var rect = ScreenPlacement.NearCursor(new Point(500, 900), Popup, Screen);
        Assert.True(rect.Bottom <= 900 - 20, $"bottom {rect.Bottom} covers the cursor line");
    }

    [Fact]
    public void Flips_left_near_the_right_edge()
    {
        var rect = ScreenPlacement.NearCursor(new Point(1900, 300), Popup, Screen);
        Assert.True(rect.Right <= Screen.WorkArea.Right);
        Assert.True(rect.X < 1900);
    }

    [Fact]
    public void Stays_inside_a_second_monitor_with_negative_coordinates()
    {
        var left = new ScreenPlacement.Monitor(new Rect(-1280, 0, 1280, 984), 1.0);
        var rect = ScreenPlacement.NearCursor(new Point(-1270, 970), Popup, left);
        Assert.True(left.WorkArea.Contains(rect), rect.ToString());
    }

    [Fact]
    public void Offsets_scale_with_the_monitor_DPI()
    {
        var hiDpi = new ScreenPlacement.Monitor(new Rect(0, 0, 3840, 2080), 2.0);
        var rect = ScreenPlacement.NearCursor(new Point(1000, 600), new Size(840, 1080), hiDpi);
        Assert.Equal(952, rect.X);
        Assert.Equal(644, rect.Y);
    }

    [Fact]
    public void A_top_left_anchor_is_pulled_back_inside_the_screen()
    {
        var rect = ScreenPlacement.AtTopLeft(new Point(1800, 900), Popup, Screen);
        Assert.Equal(1920 - 420, rect.X);
        Assert.Equal(1040 - 540, rect.Y);
    }
}
