# Writes Themes/Light.xaml and Themes/Dark.xaml from one key list, so both always define the same keys.
import os

THEMES = {
    "Light": dict(
        comment="Light: uncoated near-white stock. The ground drifts warm (top left) to cool (bottom right).",
        warm="#FAF6F5", neutral="#F5F4F6", cool="#EEF3EF", rail="#EEF3EF",
        ink="#16181A", ink_hover="#2E3135", ink2="#5E6166", muted="#686B71",
        hairline="#C9C7C4", field="#B5B3B0", seal="#DCDDF3", seal_ink="#3E4291",
        inverse="#FAF6F5", selection="#D9D6D3", danger="#B3261E", press="#ECE9E7", hover="#F2EFED",
    ),
    "Dark": dict(
        comment="Dark: the same plate at night. Near-black stock, ink reversed, the seal deepened.",
        warm="#1B1A1B", neutral="#18191B", cool="#161A19", rail="#1D2120",
        ink="#ECEBE8", ink_hover="#D4D3D0", ink2="#A9ABAF", muted="#8C8F94",
        hairline="#3A3B3F", field="#55575C", seal="#33365A", seal_ink="#C3C6F5",
        inverse="#17181A", selection="#3A3B3F", danger="#F2B8B5", press="#26272A", hover="#202124",
    ),
}

TEMPLATE = '''<!-- {comment}
     Same keys in Light.xaml and Dark.xaml (generated together); ThemeService swaps them. -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Stock -->
    <Color x:Key="GroundWarmColor">{warm}</Color>
    <LinearGradientBrush x:Key="GroundBrush" StartPoint="0,0" EndPoint="1,1">
        <GradientStop Color="{warm}" Offset="0" />
        <GradientStop Color="{neutral}" Offset="0.55" />
        <GradientStop Color="{cool}" Offset="1" />
    </LinearGradientBrush>
    <SolidColorBrush x:Key="GroundSolidBrush" Color="{neutral}" />
    <SolidColorBrush x:Key="RailBrush" Color="{rail}" />

    <!-- Ink -->
    <SolidColorBrush x:Key="InkBrush" Color="{ink}" />
    <SolidColorBrush x:Key="InkHoverBrush" Color="{ink_hover}" />
    <SolidColorBrush x:Key="InkSecondaryBrush" Color="{ink2}" />
    <SolidColorBrush x:Key="InkMutedBrush" Color="{muted}" />
    <SolidColorBrush x:Key="PlaceholderBrush" Color="{muted}" />
    <SolidColorBrush x:Key="InverseInkBrush" Color="{inverse}" />
    <SolidColorBrush x:Key="DangerBrush" Color="{danger}" />
    <SolidColorBrush x:Key="PressFillBrush" Color="{press}" />
    <SolidColorBrush x:Key="HoverFillBrush" Color="{hover}" />
    <SolidColorBrush x:Key="SelectionBrush" Color="{selection}" />

    <!-- Rules and fields -->
    <SolidColorBrush x:Key="HairlineBrush" Color="{hairline}" />
    <SolidColorBrush x:Key="FieldBorderBrush" Color="{field}" />

    <!-- The one accent: the seal -->
    <SolidColorBrush x:Key="SealBrush" Color="{seal}" />
    <SolidColorBrush x:Key="SealInkBrush" Color="{seal_ink}" />

    <!-- Fluent controls, squared off and inked -->
    <CornerRadius x:Key="ControlCornerRadius">0</CornerRadius>
    <CornerRadius x:Key="OverlayCornerRadius">0</CornerRadius>
    <CornerRadius x:Key="PopupCornerRadius">0</CornerRadius>
    <SolidColorBrush x:Key="TextControlBorderBrush" Color="{field}" />
    <SolidColorBrush x:Key="TextControlBorderBrushPointerOver" Color="{ink2}" />
    <SolidColorBrush x:Key="TextControlBorderBrushFocused" Color="{ink}" />
    <SolidColorBrush x:Key="TextControlFocusedBorderBrush" Color="{ink}" />
    <SolidColorBrush x:Key="TextControlElevationBorderBrush" Color="{field}" />
    <SolidColorBrush x:Key="TextControlElevationBorderFocusedBrush" Color="{ink}" />
    <SolidColorBrush x:Key="TextControlBackground" Color="Transparent" />
    <SolidColorBrush x:Key="TextControlBackgroundPointerOver" Color="Transparent" />
    <SolidColorBrush x:Key="TextControlBackgroundFocused" Color="Transparent" />
    <SolidColorBrush x:Key="TextControlForeground" Color="{ink}" />
    <SolidColorBrush x:Key="TextControlForegroundFocused" Color="{ink}" />
    <SolidColorBrush x:Key="TextControlPlaceholderForeground" Color="{muted}" />
    <SolidColorBrush x:Key="ComboBoxBorderBrush" Color="{field}" />
    <SolidColorBrush x:Key="ComboBoxBorderBrushPointerOver" Color="{ink2}" />
    <SolidColorBrush x:Key="ComboBoxBorderBrushFocused" Color="{ink}" />
    <SolidColorBrush x:Key="ComboBoxBorderBrushPressed" Color="{ink}" />
    <SolidColorBrush x:Key="ComboBoxBackground" Color="Transparent" />
    <SolidColorBrush x:Key="ComboBoxBackgroundPointerOver" Color="Transparent" />
    <SolidColorBrush x:Key="ComboBoxBackgroundPressed" Color="Transparent" />
    <SolidColorBrush x:Key="ControlElevationBorderBrush" Color="{field}" />
    <SolidColorBrush x:Key="AccentFillColorDefaultBrush" Color="{ink}" />
    <SolidColorBrush x:Key="AccentFillColorSecondaryBrush" Color="{ink_hover}" />
    <SolidColorBrush x:Key="AccentFillColorTertiaryBrush" Color="{ink_hover}" />
    <SolidColorBrush x:Key="AccentTextFillColorPrimaryBrush" Color="{ink}" />
    <SolidColorBrush x:Key="AccentFillColorSelectedTextBackgroundBrush" Color="{selection}" />
    <SolidColorBrush x:Key="ListViewItemPillFillBrush" Color="{ink}" />

    <!-- Context menus (tray, notebook, inputs): a square plate with a hairline edge, set in the house face.
         Lives in the palette because Fluent draws shortcut text (InputGestureText) in TextFillColorDisabledBrush,
         which is under 4.5:1; that override must carry this theme's colour, not a dynamic reference. -->
    <Style TargetType="ContextMenu">
        <Style.Resources>
            <SolidColorBrush x:Key="TextFillColorDisabledBrush" Color="{ink2}" />
        </Style.Resources>
        <Setter Property="FontFamily" Value="{{DynamicResource UiFont}}" />
        <Setter Property="FontSize" Value="13" />
        <Setter Property="Foreground" Value="{{DynamicResource InkBrush}}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ContextMenu">
                    <Border Background="{{DynamicResource GroundSolidBrush}}" BorderBrush="{{DynamicResource HairlineBrush}}"
                            BorderThickness="1" Padding="4" SnapsToDevicePixels="True">
                        <ScrollViewer VerticalScrollBarVisibility="Auto">
                            <ItemsPresenter KeyboardNavigation.DirectionalNavigation="Cycle" />
                        </ScrollViewer>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
'''

out = r"D:\Projects\Dictionary\src\LookUp\Themes"
for name, values in THEMES.items():
    with open(os.path.join(out, f"{name}.xaml"), "w", encoding="utf-8") as f:
        f.write(TEMPLATE.format(**values))
print("palettes written")
