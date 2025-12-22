"""
Analyze the actual debug_capture.png from BitBlt capture
"""
from PIL import Image
import numpy as np
import os

def analyze_image(path):
    if not os.path.exists(path):
        print(f"File not found: {path}")
        return

    img = Image.open(path)
    arr = np.array(img)
    height, width = arr.shape[:2]

    print(f"Image size: {width}x{height}")
    print()

    print("=== Scanning for separator lines (brightness 55-80, high consistency) ===")
    print()

    for y in range(height - 1, max(0, height - 300), -1):
        row = arr[y]

        # Sample brightness across the row
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

        # Show rows in the target brightness range
        if 50 <= avg <= 85:
            print(f"Y={y}: brightness={avg}, consistency={consistency_pct}%, samples={len(brightness_values)}")

            # Show sample distribution
            if consistency_pct < 50:
                sample_dist = {}
                for b in brightness_values[:20]:
                    bucket = b // 10 * 10
                    sample_dist[bucket] = sample_dist.get(bucket, 0) + 1
                print(f"       First 20 samples brightness distribution: {sample_dist}")

    print()
    print("=== Looking for any horizontal lines (high consistency) ===")

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

        if consistency_pct >= 90:
            print(f"HIGH CONSISTENCY LINE at Y={y}: brightness={avg}, consistency={consistency_pct}%")

if __name__ == "__main__":
    path = os.path.join(os.environ.get('LOCALAPPDATA', ''), 'Promptveil', 'debug_capture.png')
    analyze_image(path)
