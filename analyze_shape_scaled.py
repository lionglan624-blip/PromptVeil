"""
Analyze shape.png at 100% scale equivalent
"""
from PIL import Image
import numpy as np

def analyze_image(path):
    img = Image.open(path)

    # 4K = 200% scaling, resize to 100%
    original_size = img.size
    scaled_size = (original_size[0] // 2, original_size[1] // 2)
    img_scaled = img.resize(scaled_size, Image.Resampling.LANCZOS)

    arr = np.array(img_scaled)
    height, width = arr.shape[:2]

    print(f"Original size: {original_size}")
    print(f"Scaled size (100%): {width}x{height}")
    print()

    print("=== Scanning for separator lines at 100% scale ===")
    print()

    for y in range(height - 1, max(0, height - 200), -1):
        row = arr[y]

        brightness_values = []
        margin = min(50, width // 20)
        step = max(1, width // 100)

        for x in range(margin, width - margin, step):
            r, g, b = row[x][:3]
            brightness = (int(r) + int(g) + int(b)) // 3
            brightness_values.append(brightness)

        if not brightness_values:
            continue

        avg = sum(brightness_values) // len(brightness_values)
        consistent = sum(1 for b in brightness_values if abs(b - avg) < 15)
        consistency_pct = consistent * 100 // len(brightness_values)

        if 50 <= avg <= 85 and consistency_pct > 50:
            print(f"Y={y}: brightness={avg}, consistency={consistency_pct}%")

    print()
    print("=== High consistency lines at 100% scale ===")

    separator_lines = []
    for y in range(height - 1, 0, -1):
        row = arr[y]

        brightness_values = []
        for x in range(50, width - 50, max(1, width // 100)):
            r, g, b = row[x][:3]
            brightness_values.append((int(r) + int(g) + int(b)) // 3)

        if not brightness_values:
            continue

        avg = sum(brightness_values) // len(brightness_values)
        consistent = sum(1 for b in brightness_values if abs(b - avg) < 10)
        consistency_pct = consistent * 100 // len(brightness_values)

        if consistency_pct >= 95 and 55 <= avg <= 80:
            if not separator_lines or abs(separator_lines[-1][0] - y) > 5:
                separator_lines.append((y, avg, consistency_pct))
                print(f"SEPARATOR at Y={y}: brightness={avg}, consistency={consistency_pct}%")

    print()
    if len(separator_lines) >= 2:
        print(f"Input area: Y={separator_lines[1][0]} to Y={separator_lines[0][0]}")
        print(f"Height: {separator_lines[0][0] - separator_lines[1][0]}px")

if __name__ == "__main__":
    analyze_image(r"c:\Promptveil\shape.png")
