"""Draws the app icon: a registration crosshair on a plate of the stock (see DESIGN.md).

Every size is drawn separately on whole pixels so the 1px crosshair at 16px stays sharp,
then all sizes go into one .ico. The icon carries no letters, so it survives a rename.

    python tools/make_icon.py [preview.png]
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent.parent / "src" / "LookUp" / "Assets" / "AppIcon.ico"
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
SUPERSAMPLE = 8

STOCK = [(0.0, "#FAF6F5"), (0.55, "#F5F4F6"), (1.0, "#EEF3EF")]  # ground-warm, ground, ground-cool
EDGE = "#B5B3B0"  # field-border: the plate edge has to show on a light taskbar
INK = "#16181A"


def rgb(hex_color):
    return tuple(int(hex_color[i:i + 2], 16) for i in (1, 3, 5))


def stock_at(t):
    for (t0, c0), (t1, c1) in zip(STOCK, STOCK[1:]):
        if t <= t1:
            f = (t - t0) / (t1 - t0)
            return tuple(round(a + (b - a) * f) for a, b in zip(rgb(c0), rgb(c1)))
    return rgb(STOCK[-1][1])


def stroke_for(size):
    if size <= 24:
        return 1
    if size <= 64:
        return 2
    return round(size / 32)


def draw(size):
    w = stroke_for(size)
    margin = max(1, round(size / 16))
    plate = size - 2 * margin
    if (plate - w) % 2:
        plate += 1  # an odd remainder would put the crosshair between pixels
    edge = 1 if size <= 64 else round(size / 96)

    # The stock runs warm (top left) to cool (bottom right), as on every window.
    ground = Image.new("RGB", (plate, plate))
    for y in range(plate):
        for x in range(plate):
            ground.putpixel((x, y), stock_at((x + y) / max(1, 2 * (plate - 1))))

    k = SUPERSAMPLE
    img = Image.new("RGBA", (size * k, size * k), (0, 0, 0, 0))
    img.paste(ground.resize((plate * k, plate * k), Image.BILINEAR), (margin * k, margin * k))
    d = ImageDraw.Draw(img)
    x0, x1 = margin * k, (margin + plate) * k - 1
    d.rectangle([x0, x0, x1, x1], outline=EDGE, width=edge * k)

    # Crosshair: two strokes on whole pixels and a ring half the cross wide, as in the plates' corners.
    line = margin + (plate - w) // 2
    inset = round(plate * 0.14)
    a, b = margin + inset, margin + plate - inset
    d.rectangle([line * k, a * k, (line + w) * k - 1, b * k - 1], fill=INK)
    d.rectangle([a * k, line * k, b * k - 1, (line + w) * k - 1], fill=INK)
    if size not in PIXEL_RINGS:
        centre = (line + w / 2) * k
        r = ((b - a) / 4 + w / 2) * k
        d.ellipse([centre - r, centre - r, centre + r, centre + r], outline=INK, width=w * k)

    img = img.resize((size, size), Image.BOX)
    for dx, dy in PIXEL_RINGS.get(size, []):
        img.putpixel((line + dx, line + dy), rgb(INK) + (255,))
    return img


# At 16px an anti-aliased ring turns to grey mush; this one is placed by hand
# (a 7px circle round the centre pixel, the cross supplies its four ends).
PIXEL_RINGS = {
    16: [(dx, dy) for dy, row in enumerate([
        "..###..",
        ".#...#.",
        "#.....#",
        "#.....#",
        "#.....#",
        ".#...#.",
        "..###..",
    ], start=-3) for dx, c in enumerate(row, start=-3) if c == "#"],
}


def main():
    images = [draw(s) for s in SIZES]
    largest = images[-1]
    largest.save(OUT, format="ICO", sizes=[(s, s) for s in SIZES], append_images=images[:-1])
    print(f"wrote {OUT}")

    if len(sys.argv) > 1:
        # Each size at 1:1 on a light and a dark taskbar, plus 16 and 32 enlarged 8x.
        pad, row = 12, max(SIZES) + 24
        sheet = Image.new("RGB", (sum(SIZES) + pad * (len(SIZES) + 1), row * 2 + 32 * 8 + 16 * 8 + pad * 3), "#F3F3F3")
        dark = Image.new("RGB", (sheet.width, row), "#202020")
        sheet.paste(dark, (0, row))
        x = pad
        for s, im in zip(SIZES, images):
            for top in (0, row):
                sheet.paste(im, (x, top + (row - s) // 2), im)
            x += s + pad
        y = row * 2 + pad
        for s in (16, 32):
            big = images[SIZES.index(s)].resize((s * 8, s * 8), Image.NEAREST)
            sheet.paste(big, (pad, y), big)
            sheet.paste(Image.new("RGB", (s * 8, s * 8), "#202020"), (pad * 2 + s * 8, y))
            sheet.paste(big, (pad * 2 + s * 8, y), big)
            y += s * 8 + pad
        sheet.save(sys.argv[1])
        print(f"wrote {sys.argv[1]}")


if __name__ == "__main__":
    main()
