#!/usr/bin/env bash
# Installs the Godot .NET editor and export templates matching .github/godot_version.txt
# Exports GODOT (path to the editor binary) to $GITHUB_ENV when running in Actions
set -euo pipefail

VERSION=$(tr -d '[:space:]' < .github/godot_version.txt)
TAG="${VERSION}-stable"
BASE_URL="https://github.com/godotengine/godot/releases/download/${TAG}"
CACHE_DIR="${GODOT_CACHE:-$HOME/.cache/godot-downloads}"

case "$(uname -s)" in
  Linux)
    EDITOR_ZIP="Godot_v${TAG}_mono_linux_x86_64.zip"
    TEMPLATE_DIR="$HOME/.local/share/godot/export_templates/${VERSION}.stable.mono"
    GODOT="$CACHE_DIR/Godot_v${TAG}_mono_linux_x86_64/Godot_v${TAG}_mono_linux.x86_64"
    ;;
  Darwin)
    EDITOR_ZIP="Godot_v${TAG}_mono_macos.universal.zip"
    TEMPLATE_DIR="$HOME/Library/Application Support/Godot/export_templates/${VERSION}.stable.mono"
    GODOT="$CACHE_DIR/Godot_mono.app/Contents/MacOS/Godot"
    ;;
  *)
    echo "Unsupported OS: $(uname -s)" >&2
    exit 1
    ;;
esac

TEMPLATES_TPZ="Godot_v${TAG}_mono_export_templates.tpz"

mkdir -p "$CACHE_DIR"

for file in "$EDITOR_ZIP" "$TEMPLATES_TPZ"; do
  if [ ! -f "$CACHE_DIR/$file" ]; then
    echo "Downloading $file"
    curl -fsSL --retry 3 -o "$CACHE_DIR/$file" "$BASE_URL/$file"
  fi
done

if [ ! -x "$GODOT" ]; then
  unzip -q -o "$CACHE_DIR/$EDITOR_ZIP" -d "$CACHE_DIR"
fi

if [ ! -f "$TEMPLATE_DIR/version.txt" ]; then
  rm -rf "$CACHE_DIR/templates"
  unzip -q -o "$CACHE_DIR/$TEMPLATES_TPZ" -d "$CACHE_DIR"
  mkdir -p "$TEMPLATE_DIR"
  cp -R "$CACHE_DIR/templates/." "$TEMPLATE_DIR/"
  rm -rf "$CACHE_DIR/templates"
fi

"$GODOT" --headless --version

if [ -n "${GITHUB_ENV:-}" ]; then
  echo "GODOT=$GODOT" >> "$GITHUB_ENV"
else
  echo "GODOT=$GODOT"
fi
