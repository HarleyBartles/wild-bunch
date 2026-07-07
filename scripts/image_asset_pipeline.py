#!/usr/bin/env python3
"""Cut, normalize, and promote generated image assets onto fixed canvases.

Primary backend: Python 3.11+ with Pillow installed in the active environment.
This script is intentionally generic so it can be reused for any future asset
family that needs background cutout and footprint normalization.
"""

from __future__ import annotations

import argparse
from collections import Counter, deque
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

try:
    from PIL import Image
except ImportError as exc:  # pragma: no cover - import guard
    raise SystemExit(
        "Pillow is required for the primary image pipeline backend. "
        "Install it in your active Python environment."
    ) from exc


@dataclass(frozen=True)
class PipelineConfig:
    canvas_width: int = 80
    canvas_height: int = 50
    padding: int = 1
    sample_radius: int = 6
    color_tolerance: int = 42
    sheet_padding: int = 0
    sheet_background_tolerance: int = 20


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        prog="image_asset_pipeline.py",
        description="Cut generated art away from a flat background and normalize it onto a canvas.",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    normalize = subparsers.add_parser("normalize", help="Cut and normalize one image")
    normalize.add_argument("--input", required=True, type=Path, help="Source image")
    normalize.add_argument("--out", required=True, type=Path, help="Output PNG")
    normalize.add_argument("--canvas-width", type=int, default=60)
    normalize.add_argument("--canvas-height", type=int, default=50)
    normalize.add_argument("--padding", type=int, default=1)
    normalize.add_argument("--sample-radius", type=int, default=6)
    normalize.add_argument("--color-tolerance", type=int, default=42)

    sheet = subparsers.add_parser("slice-sheet", help="Split a turnaround sheet into separate crops")
    sheet.add_argument("--input", required=True, type=Path, help="Source sheet image")
    sheet.add_argument("--out-dir", required=True, type=Path, help="Directory for the cropped outputs")
    sheet.add_argument(
        "--names",
        default="",
        help="Comma-separated output names in reading order, without extensions",
    )
    sheet.add_argument("--sample-radius", type=int, default=12)
    sheet.add_argument("--background-tolerance", type=int, default=20)
    sheet.add_argument("--padding", type=int, default=12)

    promote = subparsers.add_parser(
        "promote-sprites",
        help="Normalize a pipeline tree into the matching sprites tree",
    )
    promote.add_argument("--input-root", required=True, type=Path, help="Pipeline root to promote from")
    promote.add_argument("--out-root", required=True, type=Path, help="Sprite root to promote into")
    promote.add_argument("--canvas-width", type=int, default=60)
    promote.add_argument("--canvas-height", type=int, default=50)
    promote.add_argument("--padding", type=int, default=1)
    promote.add_argument("--sample-radius", type=int, default=6)
    promote.add_argument("--color-tolerance", type=int, default=42)

    return parser.parse_args()


def _sample_background_color(image: Image.Image, radius: int) -> tuple[int, int, int]:
    width, height = image.size
    xs = sorted({0, min(radius, width - 1), max(width - 1 - radius, 0), width - 1})
    ys = sorted({0, min(radius, height - 1), max(height - 1 - radius, 0), height - 1})
    samples = [image.getpixel((x, y))[:3] for x in xs for y in ys]
    return Counter(samples).most_common(1)[0][0]


def _is_background(
    pixel: tuple[int, int, int, int],
    background: tuple[int, int, int],
    tolerance: int,
    *,
    require_green_dominance: bool = True,
) -> bool:
    if pixel[3] == 0:
        return True
    r, g, b, _ = pixel
    br, bg, bb = background
    distance = abs(r - br) + abs(g - bg) + abs(b - bb)
    green_dominance = g >= max(r, b) + 8
    return distance <= tolerance * 3 and (green_dominance if require_green_dominance else True)


def _edge_connected_background_mask(
    image: Image.Image,
    background: tuple[int, int, int],
    tolerance: int,
    *,
    require_green_dominance: bool = True,
) -> Image.Image:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()
    mask = [[False] * width for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()

    def enqueue_if_background(x: int, y: int) -> None:
        if mask[y][x]:
            return
        if _is_background(pixels[x, y], background, tolerance, require_green_dominance=require_green_dominance):
            mask[y][x] = True
            queue.append((x, y))

    for x in range(width):
        enqueue_if_background(x, 0)
        if height > 1:
            enqueue_if_background(x, height - 1)
    for y in range(height):
        enqueue_if_background(0, y)
        if width > 1:
            enqueue_if_background(width - 1, y)

    while queue:
        current_x, current_y = queue.popleft()
        for next_x, next_y in (
            (current_x + 1, current_y),
            (current_x - 1, current_y),
            (current_x, current_y + 1),
            (current_x, current_y - 1),
        ):
            if 0 <= next_x < width and 0 <= next_y < height and not mask[next_y][next_x]:
                if _is_background(
                    pixels[next_x, next_y],
                    background,
                    tolerance,
                    require_green_dominance=require_green_dominance,
                ):
                    mask[next_y][next_x] = True
                    queue.append((next_x, next_y))

    for y in range(height):
        for x in range(width):
            if mask[y][x]:
                pixels[x, y] = (pixels[x, y][0], pixels[x, y][1], pixels[x, y][2], 0)

    return rgba


def _is_near_background(
    pixel: tuple[int, int, int, int],
    background: tuple[int, int, int],
    tolerance: int,
) -> bool:
    if pixel[3] == 0:
        return True
    r, g, b, _ = pixel
    br, bg, bb = background
    distance = abs(r - br) + abs(g - bg) + abs(b - bb)
    return distance <= tolerance * 3


def _connected_components(
    image: Image.Image,
    *,
    background: tuple[int, int, int],
    tolerance: int,
) -> list[tuple[int, int, int, int]]:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()
    mask = [[False] * width for _ in range(height)]
    for y in range(height):
        for x in range(width):
            if not _is_near_background(pixels[x, y], background, tolerance):
                mask[y][x] = True

    seen = [[False] * width for _ in range(height)]
    boxes: list[tuple[int, int, int, int, int]] = []
    for y in range(height):
        for x in range(width):
            if mask[y][x] and not seen[y][x]:
                queue = deque([(x, y)])
                seen[y][x] = True
                min_x = max_x = x
                min_y = max_y = y
                pixel_count = 0
                while queue:
                    current_x, current_y = queue.popleft()
                    pixel_count += 1
                    min_x = min(min_x, current_x)
                    max_x = max(max_x, current_x)
                    min_y = min(min_y, current_y)
                    max_y = max(max_y, current_y)
                    for next_x, next_y in (
                        (current_x + 1, current_y),
                        (current_x - 1, current_y),
                        (current_x, current_y + 1),
                        (current_x, current_y - 1),
                    ):
                        if (
                            0 <= next_x < width
                            and 0 <= next_y < height
                            and mask[next_y][next_x]
                            and not seen[next_y][next_x]
                        ):
                            seen[next_y][next_x] = True
                            queue.append((next_x, next_y))
                boxes.append((pixel_count, min_x, min_y, max_x, max_y))

    return [
        (min_x, min_y, max_x + 1, max_y + 1)
        for pixel_count, min_x, min_y, max_x, max_y in boxes
        if pixel_count > 1000
    ]


def _sort_reading_order(boxes: Iterable[tuple[int, int, int, int]]) -> list[tuple[int, int, int, int]]:
    return sorted(boxes, key=lambda box: (box[1], box[0]))


def _normalize_crop(image: Image.Image, *, canvas_width: int, canvas_height: int, padding: int) -> Image.Image:
    source_width, source_height = image.size
    usable_width = max(canvas_width - padding * 2, 1)
    usable_height = max(canvas_height - padding * 2, 1)
    scale = min(usable_width / source_width, usable_height / source_height)
    scaled_width = max(1, round(source_width * scale))
    scaled_height = max(1, round(source_height * scale))
    resample = getattr(Image, "Resampling", Image).LANCZOS
    resized = image.resize((scaled_width, scaled_height), resample)
    canvas = Image.new("RGBA", (canvas_width, canvas_height), (255, 255, 255, 255))
    x = (canvas_width - scaled_width) // 2
    y = (canvas_height - scaled_height) // 2
    layer = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))
    layer.paste(resized, (x, y))
    canvas.alpha_composite(layer)
    return canvas


def slice_sheet(input_path: Path, output_dir: Path, names: list[str], *, sample_radius: int, tolerance: int, padding: int) -> None:
    with Image.open(input_path) as image:
        background = _sample_background_color(image, sample_radius)
        boxes = _sort_reading_order(_connected_components(image, background=background, tolerance=tolerance))
        if len(boxes) != len(names):
            raise RuntimeError(
                f"Expected {len(names)} views but detected {len(boxes)} components in {input_path.name}"
            )
        crops = [image.crop(box) for box in boxes]
        canvas_width = max(crop.size[0] for crop in crops) + padding * 2
        canvas_height = max(crop.size[1] for crop in crops) + padding * 2
        output_dir.mkdir(parents=True, exist_ok=True)
        for crop, name in zip(crops, names):
            normalized = _normalize_crop(crop, canvas_width=canvas_width, canvas_height=canvas_height, padding=padding)
            normalized.save(output_dir / f"{name}.png")


def _trim_transparency(image: Image.Image) -> Image.Image:
    bbox = image.getbbox()
    if bbox is None:
        raise RuntimeError("Image became fully transparent after background cutout")
    return image.crop(bbox)


def _normalize_to_canvas(image: Image.Image, *, canvas_width: int, canvas_height: int, padding: int) -> Image.Image:
    source_width, source_height = image.size
    if source_width == 0 or source_height == 0:
        raise RuntimeError("Cannot normalize an empty image")

    usable_width = max(canvas_width - padding * 2, 1)
    usable_height = max(canvas_height - padding * 2, 1)
    scale = min(usable_width / source_width, usable_height / source_height)
    scaled_width = max(1, round(source_width * scale))
    scaled_height = max(1, round(source_height * scale))

    resample = getattr(Image, "Resampling", Image).LANCZOS
    resized = image.resize((scaled_width, scaled_height), resample)

    canvas = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))
    x = (canvas_width - scaled_width) // 2
    y = canvas_height - padding - scaled_height
    if y < 0:
        y = 0
    layer = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))
    layer.paste(resized, (x, y))
    canvas.alpha_composite(layer)
    return canvas


def normalize_image(input_path: Path, output_path: Path, config: PipelineConfig) -> None:
    with Image.open(input_path) as image:
        background = _sample_background_color(image, config.sample_radius)
        cut = _edge_connected_background_mask(
            image,
            background,
            config.color_tolerance,
            require_green_dominance=False,
        )
        trimmed = _trim_transparency(cut)
        normalized = _normalize_to_canvas(
            trimmed,
            canvas_width=config.canvas_width,
            canvas_height=config.canvas_height,
            padding=config.padding,
        )
        output_path.parent.mkdir(parents=True, exist_ok=True)
        normalized.save(output_path)


def cut_background_in_place(input_path: Path, config: PipelineConfig) -> None:
    with Image.open(input_path) as image:
        background = _sample_background_color(image, config.sample_radius)
        cut = _edge_connected_background_mask(
            image,
            background,
            config.color_tolerance,
            require_green_dominance=False,
        )
        input_path.parent.mkdir(parents=True, exist_ok=True)
        cut.save(input_path)


def promote_sprites(input_root: Path, output_root: Path, config: PipelineConfig) -> int:
    if not input_root.exists():
        raise SystemExit(f"Input root does not exist: {input_root}")

    promoted = 0
    for source_path in sorted(input_root.rglob("*.png")):
        relative_path = source_path.relative_to(input_root)
        if "normalized" in relative_path.parts:
            continue
        cut_background_in_place(source_path, config)
        destination_path = output_root / relative_path
        normalize_image(source_path, destination_path, config)
        promoted += 1

    if promoted == 0:
        raise SystemExit(f"No PNG files found under {input_root}")

    return promoted


def main() -> int:
    args = _parse_args()
    if args.command == "normalize":
        config = PipelineConfig(
            canvas_width=args.canvas_width,
            canvas_height=args.canvas_height,
            padding=args.padding,
            sample_radius=args.sample_radius,
            color_tolerance=args.color_tolerance,
        )
        normalize_image(args.input, args.out, config)
        return 0
    if args.command == "slice-sheet":
        names = [name.strip() for name in args.names.split(",") if name.strip()]
        if not names:
            raise SystemExit("--names is required for slice-sheet")
        slice_sheet(
            args.input,
            args.out_dir,
            names,
            sample_radius=args.sample_radius,
            tolerance=args.background_tolerance,
            padding=args.padding,
        )
        return 0
    if args.command == "promote-sprites":
        config = PipelineConfig(
            canvas_width=args.canvas_width,
            canvas_height=args.canvas_height,
            padding=args.padding,
            sample_radius=args.sample_radius,
            color_tolerance=args.color_tolerance,
        )
        promoted = promote_sprites(args.input_root, args.out_root, config)
        print(f"Promoted {promoted} PNG files from {args.input_root} to {args.out_root}")
        return 0
    raise SystemExit(f"Unknown command: {args.command}")


if __name__ == "__main__":
    raise SystemExit(main())
