#!/usr/bin/env python3
"""Analyze debug_capture.png to find gray horizontal lines"""

from PIL import Image
import os

# Load the captured image
capture_path = os.path.join(os.environ['LOCALAPPDATA'], 'Promptveil', 'shape.png')
if not os.path.exists(capture_path):
    capture_path = os.path.join(os.environ['LOCALAPPDATA'], 'Promptveil', 'debug_capture.png')
print(f"Loading: {capture_path}")

img = Image.open(capture_path)
width, height = img.size
print(f"Image size: {width}x{height}")

pixels = img.load()

# Analyze each row for "grayness" - look for rows where most pixels are similar gray
def analyze_row(y):
    """Analyze a row and return stats about gray pixels"""
    gray_count = 0
    total_gray_value = 0
    sample_pixels = []

    for x in range(0, width, max(1, width // 100)):
        r, g, b = pixels[x, y][:3]
        avg = (r + g + b) // 3
        max_diff = max(abs(r - avg), abs(g - avg), abs(b - avg))

        # Check if pixel is gray (R, G, B are similar)
        if max_diff < 20:
            gray_count += 1
            total_gray_value += avg
            if len(sample_pixels) < 5:
                sample_pixels.append((r, g, b))

    sample_count = width // max(1, width // 100)
    gray_ratio = gray_count / sample_count if sample_count > 0 else 0
    avg_gray = total_gray_value // gray_count if gray_count > 0 else 0

    return gray_ratio, avg_gray, sample_pixels

# Find rows with high gray ratio (potential separator lines)
print("\nScanning for horizontal gray lines...")
print("-" * 60)

candidates = []
for y in range(height):
    gray_ratio, avg_gray, samples = analyze_row(y)

    # Look for rows where >70% is gray and gray value is in mid-range (not too dark, not too light)
    if gray_ratio > 0.7 and 30 <= avg_gray <= 120:
        candidates.append((y, gray_ratio, avg_gray, samples))

# Group consecutive rows (same line spans multiple pixels)
print(f"\nFound {len(candidates)} candidate rows")

if candidates:
    groups = []
    current_group = [candidates[0]]

    for i in range(1, len(candidates)):
        if candidates[i][0] - candidates[i-1][0] <= 3:  # Within 3 pixels
            current_group.append(candidates[i])
        else:
            groups.append(current_group)
            current_group = [candidates[i]]
    groups.append(current_group)

    print(f"\nGrouped into {len(groups)} distinct lines:")
    print("-" * 60)

    for i, group in enumerate(groups):
        y_start = group[0][0]
        y_end = group[-1][0]
        avg_gray = sum(c[2] for c in group) // len(group)
        sample = group[len(group)//2][3]

        print(f"Line {i+1}: Y={y_start}-{y_end} (thickness={y_end-y_start+1}px)")
        print(f"         Avg gray value: {avg_gray}")
        print(f"         Sample RGB: {sample[:3] if sample else 'N/A'}")
        print()

# Also sample specific Y positions to see RGB values
print("\nSampling RGB values at various Y positions:")
print("-" * 60)

# Sample bottom 40% of image (where input area typically is)
for y in range(int(height * 0.5), height, height // 20):
    samples = []
    for x in [width//4, width//2, width*3//4]:
        r, g, b = pixels[x, y][:3]
        samples.append(f"({r},{g},{b})")
    print(f"Y={y}: {', '.join(samples)}")
