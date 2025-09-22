#!/bin/bash

# Resize all PNGs in the current folder (8x8 -> 32x32) and center them in 40x40 canvas

output_dir="padded"
mkdir -p "$output_dir"

for f in *.png; do
  [ -e "$f" ] || continue  # Skip if no .png files exist

  echo "Processing $f..."
  magick "$f" \
    -scale 400% \
    -background none -gravity center -extent 40x40 \
    "$output_dir/$f"
done

echo "✅ Done! Saved padded sprites to ./$output_dir/"
