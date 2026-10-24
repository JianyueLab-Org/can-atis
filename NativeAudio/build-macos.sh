#!/usr/bin/env bash
# Cross-compiles the ARM64 macOS libNativeAudio.dylib inside the osxcross Docker image.
set -euo pipefail
cd "$(dirname "$0")"

IMAGE="ghcr.io/shepherdjerred/macos-cross-compiler:latest"

docker run --rm \
  --user "$(id -u):$(id -g)" \
  -e TMPDIR=/src/build-macos-tmp \
  -v "$(pwd)":/src \
  -w /src \
  "$IMAGE" \
  bash -c '
    set -euo pipefail
    mkdir -p "$TMPDIR"
    SDK_PATH="/osxcross/SDK/MacOSX15.0.sdk"

    echo "==> Building arm64..."
    cmake -S . -B build-macos-arm64 \
      -DCMAKE_TOOLCHAIN_FILE=toolchains/macos-arm64.cmake \
      -DCMAKE_BUILD_TYPE=Release \
      -DCMAKE_OSX_ARCHITECTURES=arm64 \
      -DCMAKE_FRAMEWORK_PATH="$SDK_PATH/System/Library/Frameworks" \
      -DCMAKE_LIBRARY_PATH="$SDK_PATH/usr/lib" \
      -DNATIVE_DLL_OUTPUT_DIR=/src/build-macos-arm64/out
    cmake --build build-macos-arm64 -- -j$(nproc)
  '

mkdir -p ../src/CanAtis.Desktop/Voice/Audio/Native/macos
cp build-macos-arm64/libNativeAudio.dylib ../src/CanAtis.Desktop/Voice/Audio/Native/macos/
rm -rf build-macos-tmp
echo "==> Copied to src/CanAtis.Desktop/Voice/Audio/Native/macos/libNativeAudio.dylib"
