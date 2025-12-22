"""
Analyze shape.png to find the best algorithm for detecting Claude Code input area
"""
from PIL import Image
import numpy as np

def analyze_image(path):
    img = Image.open(path)
    arr = np.array(img)
    height, width = arr.shape[:2]

    print(f"Image size: {width}x{height}")
    print()

    # Based on previous analysis:
    # - Gray separator lines are at Y=970-973 and Y=1062-1065
    # - The '>' prompt at Y=995-1040 is BETWEEN these two lines
    # - RGB of separator lines is (67,67,67) - brightness 67

    print("=== Analyzing the separator line area ===")

    # Look at Y=970 area (upper separator)
    print("\nAround Y=970 (upper separator):")
    for y in range(965, 980):
        row = arr[y]
        # Sample across width
        samples = []
        brightness_sum = 0
        consistent = 0
        for x in range(100, width - 100, 50):
            r, g, b = row[x][:3]
            brightness = (int(r) + int(g) + int(b)) // 3
            brightness_sum += brightness
            samples.append(brightness)
            if 60 <= brightness <= 75:
                consistent += 1

        avg_brightness = brightness_sum // len(samples) if samples else 0
        consistent_pct = consistent * 100 // len(samples) if samples else 0
        if consistent_pct > 50 or 60 <= avg_brightness <= 75:
            print(f"  Y={y}: avg_brightness={avg_brightness}, consistent={consistent_pct}%")

    # Look at Y=1062 area (lower separator)
    print("\nAround Y=1062 (lower separator):")
    for y in range(1058, 1070):
        row = arr[y]
        samples = []
        brightness_sum = 0
        consistent = 0
        for x in range(100, width - 100, 50):
            r, g, b = row[x][:3]
            brightness = (int(r) + int(g) + int(b)) // 3
            brightness_sum += brightness
            samples.append(brightness)
            if 60 <= brightness <= 75:
                consistent += 1

        avg_brightness = brightness_sum // len(samples) if samples else 0
        consistent_pct = consistent * 100 // len(samples) if samples else 0
        if consistent_pct > 50 or 60 <= avg_brightness <= 75:
            print(f"  Y={y}: avg_brightness={avg_brightness}, consistent={consistent_pct}%")

    print("\n=== Proposed Algorithm ===")
    print("""
1. Scan from bottom up looking for rows where:
   - Average brightness is 60-75 (dark gray)
   - High consistency (>90% of samples have similar brightness)
   - This indicates a horizontal separator line

2. The separator lines are RGB(67,67,67), NOT RGB(100-170)
   - Current code looks for brightness 100-170 (too bright!)
   - Should look for brightness 60-80

3. Input area is BETWEEN two separator lines:
   - Upper line at ~Y=970 (in this 1708px image)
   - Lower line at ~Y=1062
   - The '>' prompt is at Y=995-1040

4. Scale factor: This is a 200% DPI screenshot
   - Actual window would be ~854px tall
   - Lines would be at ~485 and ~531 in 100% scale
""")

    print("\n=== Testing new detection algorithm ===")

    # New algorithm: find dark gray horizontal lines
    separator_lines = []

    for y in range(height - 1, 100, -1):
        row = arr[y]

        # Sample brightness across the row
        brightness_values = []
        for x in range(100, width - 100, 20):
            r, g, b = row[x][:3]
            brightness_values.append((int(r) + int(g) + int(b)) // 3)

        if not brightness_values:
            continue

        avg = sum(brightness_values) // len(brightness_values)

        # Check consistency
        consistent = sum(1 for b in brightness_values if abs(b - avg) < 10)
        consistency_pct = consistent * 100 // len(brightness_values)

        # Dark gray separator detection
        if 55 <= avg <= 80 and consistency_pct >= 95:
            # Check if this is part of an existing line group
            if separator_lines and abs(separator_lines[-1][0] - y) <= 5:
                continue  # Skip, too close to previous

            separator_lines.append((y, avg, consistency_pct))
            print(f"Separator line found: Y={y}, brightness={avg}, consistency={consistency_pct}%")

    print(f"\nTotal separator lines found: {len(separator_lines)}")

    if len(separator_lines) >= 2:
        bottom_line = separator_lines[0][0]
        top_line = separator_lines[1][0]
        print(f"\nInput area bounds:")
        print(f"  Top: Y={top_line}")
        print(f"  Bottom: Y={bottom_line}")
        print(f"  Height: {bottom_line - top_line}px")

if __name__ == "__main__":
    analyze_image(r"c:\Promptveil\shape.png")
