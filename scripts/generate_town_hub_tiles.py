#!/usr/bin/env python3
"""Generate the town-hub road and ground tile families.

This is a small deterministic generator for the BUNCH-142 tile slice. It
creates source-custody art only; the existing image pipeline is then used to
stage and promote the results into `staging/` and `sprites/`.
"""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ROAD_SOURCE = ROOT / "src/WildBunch.Assets/source/town-hub-roads"
GROUND_SOURCE = ROOT / "src/WildBunch.Assets/source/town-hub-ground"

SIZE = (80, 50)

ROAD_DARK = (86, 60, 39, 255)
ROAD_MID = (116, 85, 57, 255)
ROAD_LIGHT = (140, 106, 73, 255)
DIRT_DARK = (102, 74, 42, 255)
DIRT_MID = (137, 98, 56, 255)
DIRT_LIGHT = (166, 120, 70, 255)
STONE = (173, 157, 137, 255)
STONE_DARK = (118, 107, 94, 255)
GREEN = (90, 130, 60, 255)
GREEN_DARK = (59, 94, 44, 255)


def _rng(seed: int) -> random.Random:
    return random.Random(seed)


def _save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)


def _tile_patch(patch: Image.Image, size: tuple[int, int] = SIZE) -> Image.Image:
    width, height = size
    result = Image.new("RGBA", size, (0, 0, 0, 0))
    for y in range(0, height, patch.height):
        for x in range(0, width, patch.width):
            result.alpha_composite(patch, (x, y))
    return result


def _base_dirt_patch(seed: int, width: int = 40, height: int = 25) -> Image.Image:
    rng = _rng(seed)
    image = Image.new("RGBA", (width, height), DIRT_MID)
    px = image.load()
    for y in range(height):
        for x in range(width):
            mix = (x * 31 + y * 17 + rng.randint(0, 255)) % 100
            if mix < 10:
                px[x, y] = DIRT_DARK
            elif mix < 20:
                px[x, y] = DIRT_LIGHT
            elif mix < 24:
                px[x, y] = GREEN_DARK
    draw = ImageDraw.Draw(image)
    for _ in range(18):
        x = rng.randrange(width)
        y = rng.randrange(height)
        r = rng.choice([1, 1, 1, 2])
        color = rng.choice([STONE, STONE_DARK, DIRT_LIGHT, GREEN])
        draw.ellipse((x - r, y - r, x + r, y + r), fill=color)
    for _ in range(8):
        x = rng.randrange(width)
        y = rng.randrange(height)
        draw.line((x, y, x + rng.randint(-4, 4), y + rng.randint(-1, 1)), fill=GREEN, width=1)
    return image


def _major_road_patch(seed: int, variant: str) -> Image.Image:
    rng = _rng(seed)
    image = Image.new("RGBA", (80, 25), DIRT_MID)
    px = image.load()
    road_edge = 53
    shoulder = 6
    for y in range(25):
        for x in range(80):
            if x < road_edge - shoulder:
                band = (x * 5 + y * 11 + rng.randint(0, 31)) % 100
                if band < 18:
                    px[x, y] = ROAD_DARK
                elif band < 38:
                    px[x, y] = ROAD_MID
                else:
                    px[x, y] = ROAD_LIGHT
            elif x < road_edge + 2:
                px[x, y] = STONE_DARK if (x + y) % 2 == 0 else STONE
            else:
                band = (x * 9 + y * 7 + rng.randint(0, 47)) % 100
                if band < 12:
                    px[x, y] = DIRT_DARK
                elif band < 24:
                    px[x, y] = DIRT_LIGHT
                elif band < 30:
                    px[x, y] = GREEN_DARK
    draw = ImageDraw.Draw(image)
    for y in range(25):
        if y % 3 == 0:
            draw.line((2, y, road_edge - 4, y), fill=ROAD_DARK, width=1)
    for y in range(25):
        if y % 4 == 0:
            draw.line((road_edge + 4, y, 79, y), fill=DIRT_LIGHT, width=1)
    for y in range(0, 25, 2):
        draw.point((road_edge - 1, y), fill=STONE)
    if variant == "path":
        for y in range(5, 25):
            if 63 <= y <= 22:
                pass
        draw.line((76, 20, 64, 14), fill=DIRT_LIGHT, width=2)
        draw.line((64, 14, road_edge - 2, 14), fill=DIRT_MID, width=2)
    elif variant == "spur-cross":
        draw.line((70, 0, 70, 24), fill=DIRT_LIGHT, width=2)
        draw.line((70, 12, road_edge - 1, 12), fill=DIRT_MID, width=2)
    elif variant == "flat":
        draw.line((73, 0, 73, 24), fill=DIRT_MID, width=1)
    return image


def _spur_patch(seed: int, variant: str) -> Image.Image:
    rng = _rng(seed)
    image = Image.new("RGBA", (40, 50), DIRT_MID)
    px = image.load()
    road_y = 24
    road_h = 10
    for y in range(50):
        for x in range(40):
            if abs(y - road_y) <= road_h // 2:
                band = (x * 7 + y * 11 + rng.randint(0, 31)) % 100
                px[x, y] = ROAD_DARK if band < 25 else ROAD_MID if band < 65 else ROAD_LIGHT
            else:
                band = (x * 11 + y * 7 + rng.randint(0, 47)) % 100
                px[x, y] = DIRT_DARK if band < 10 else DIRT_MID if band < 26 else DIRT_LIGHT
    draw = ImageDraw.Draw(image)
    for x in range(40):
        if x % 4 == 0:
            draw.point((x, road_y - 4), fill=STONE)
            draw.point((x, road_y + 4), fill=STONE_DARK)
    if variant == "path":
        draw.line((20, 0, 20, road_y - 4), fill=DIRT_LIGHT, width=2)
        draw.line((18, 0, 22, 0), fill=DIRT_DARK, width=1)
    elif variant == "end":
        draw.line((34, road_y - 1, 39, road_y - 1), fill=DIRT_LIGHT, width=2)
        draw.line((31, road_y - 3, 39, road_y - 3), fill=DIRT_DARK, width=1)
    return image


def _dirt_prop_canvas(seed: int) -> Image.Image:
    rng = _rng(seed)
    image = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    for _ in range(24):
        x = rng.randrange(10, 70)
        y = rng.randrange(10, 42)
        r = rng.choice([1, 1, 2, 2, 3])
        color = rng.choice([STONE, STONE_DARK, DIRT_LIGHT, GREEN])
        draw.ellipse((x - r, y - r, x + r, y + r), fill=color)
    return image


def _make_cactus(seed: int) -> Image.Image:
    image = _dirt_prop_canvas(seed)
    draw = ImageDraw.Draw(image)
    trunk = (38, 18, 44, 38)
    arm_l = (33, 22, 38, 28)
    arm_r = (44, 26, 49, 32)
    draw.rounded_rectangle(trunk, radius=2, fill=GREEN, outline=GREEN_DARK)
    draw.rounded_rectangle(arm_l, radius=2, fill=GREEN, outline=GREEN_DARK)
    draw.rounded_rectangle(arm_r, radius=2, fill=GREEN, outline=GREEN_DARK)
    draw.ellipse((36, 13, 46, 21), fill=GREEN, outline=GREEN_DARK)
    return image


def _make_tumbleweed(seed: int) -> Image.Image:
    image = _dirt_prop_canvas(seed)
    draw = ImageDraw.Draw(image)
    center = (40, 27)
    for radius in (10, 8, 6, 4):
        draw.ellipse((center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius), outline=(120, 89, 52, 255))
    for angle in range(0, 180, 20):
        dx = int(math.cos(math.radians(angle)) * 10)
        dy = int(math.sin(math.radians(angle)) * 10)
        draw.line((center[0], center[1], center[0] + dx, center[1] + dy), fill=(139, 106, 64, 255), width=1)
    return image


def _make_scrub(seed: int) -> Image.Image:
    image = _dirt_prop_canvas(seed)
    draw = ImageDraw.Draw(image)
    draw.ellipse((29, 26, 51, 40), fill=GREEN, outline=GREEN_DARK)
    draw.ellipse((34, 21, 46, 34), fill=GREEN, outline=GREEN_DARK)
    draw.line((40, 40, 40, 45), fill=(110, 82, 43, 255), width=1)
    return image


def _make_fence_post(seed: int) -> Image.Image:
    image = _dirt_prop_canvas(seed)
    draw = ImageDraw.Draw(image)
    draw.rectangle((38, 14, 42, 40), fill=(126, 93, 53, 255), outline=(88, 63, 38, 255))
    draw.line((34, 21, 46, 21), fill=(126, 93, 53, 255), width=2)
    draw.line((36, 16, 44, 16), fill=(156, 117, 68, 255), width=1)
    return image


def _make_rocks(seed: int) -> Image.Image:
    image = _dirt_prop_canvas(seed)
    draw = ImageDraw.Draw(image)
    rocks = [(35, 28, 3), (42, 26, 4), (47, 31, 2), (38, 35, 2), (44, 36, 3)]
    for x, y, r in rocks:
        draw.ellipse((x - r, y - r, x + r, y + r), fill=STONE, outline=STONE_DARK)
    return image


def _hill_tile(seed: int, corner: str) -> Image.Image:
    rng = _rng(seed)
    image = _tile_patch(_base_dirt_patch(seed + 17), SIZE)
    draw = ImageDraw.Draw(image)
    if corner == "nw":
        points = [(0, 0), (33, 0), (48, 10), (34, 24), (0, 28)]
        shadow = [(0, 18), (18, 12), (36, 18), (34, 32), (0, 40)]
    elif corner == "ne":
        points = [(47, 0), (79, 0), (79, 28), (44, 24), (30, 8)]
        shadow = [(44, 18), (62, 12), (79, 18), (79, 40), (52, 32)]
    elif corner == "sw":
        points = [(0, 22), (30, 18), (45, 34), (34, 49), (0, 49)]
        shadow = [(0, 28), (18, 22), (34, 30), (30, 49), (0, 49)]
    else:  # se
        points = [(35, 20), (79, 24), (79, 49), (47, 49), (30, 34)]
        shadow = [(45, 28), (62, 22), (79, 30), (79, 49), (52, 49)]
    draw.polygon(points, fill=(136, 99, 55, 255), outline=(90, 63, 33, 255))
    draw.polygon(shadow, fill=(106, 76, 42, 120))
    for _ in range(18):
        x = rng.randrange(0, 50) if corner in {"nw", "sw"} else rng.randrange(30, 80)
        y = rng.randrange(0, 30) if corner in {"nw", "ne"} else rng.randrange(20, 50)
        r = rng.choice([1, 1, 2])
        draw.ellipse((x - r, y - r, x + r, y + r), fill=rng.choice([STONE, STONE_DARK, DIRT_LIGHT]))
    return image


def generate_roads() -> None:
    sources = {
        ROAD_SOURCE / "main-road" / "flat-edge-right.png": _tile_patch(_major_road_patch(101, "flat")),
        ROAD_SOURCE / "main-road" / "path-edge-right.png": _tile_patch(_major_road_patch(102, "path")),
        ROAD_SOURCE / "main-road" / "spur-cross-right.png": _tile_patch(_major_road_patch(103, "spur-cross")),
    }

    for path, image in sources.items():
        _save(image, path)


def generate_ground() -> None:
    base_variants = {
        "dirt-a.png": _tile_patch(_base_dirt_patch(301)),
        "dirt-b.png": _tile_patch(_base_dirt_patch(302)),
        "dirt-c.png": _tile_patch(_base_dirt_patch(303)),
    }
    for name, image in base_variants.items():
        _save(image, GROUND_SOURCE / "base" / name)

    props = {
        "dirt-cactus.png": _make_cactus(401),
        "dirt-tumbleweed.png": _make_tumbleweed(402),
        "dirt-scrub.png": _make_scrub(403),
        "dirt-post.png": _make_fence_post(404),
        "dirt-rocks.png": _make_rocks(405),
    }
    for name, image in props.items():
        _save(image, GROUND_SOURCE / "props" / name)

    hills = {
        "hill-nw.png": _hill_tile(501, "nw"),
        "hill-ne.png": _hill_tile(502, "ne"),
        "hill-sw.png": _hill_tile(503, "sw"),
        "hill-se.png": _hill_tile(504, "se"),
    }
    for name, image in hills.items():
        _save(image, GROUND_SOURCE / "landforms" / name)


def main() -> int:
    generate_roads()
    generate_ground()
    print("Generated town-hub road and ground source tiles.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
