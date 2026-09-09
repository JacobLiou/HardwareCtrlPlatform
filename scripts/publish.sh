#!/bin/bash
# VOA 准直器手动调节工位发布脚本（自包含，Bash）
# 注意：本应用为 WPF，实际运行目标仍是 Windows；本脚本便于在 Windows 的 Git Bash / CI 中调用。

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
APP_PROJECT="$PROJECT_ROOT/src/VoaCollimator.App/VoaCollimator.App.csproj"
CONFIGURATION="${1:-Release}"
RUNTIME_ID="${2:-win-x86}"
OUTPUT_BASE="${3:-$PROJECT_ROOT/publish}"
OUTPUT_PATH="$OUTPUT_BASE/$RUNTIME_ID/$CONFIGURATION"

case "$RUNTIME_ID" in
  win-x86) PLATFORM_TARGET=x86 ;;
  win-arm64) PLATFORM_TARGET=ARM64 ;;
  win-x64) PLATFORM_TARGET=x64 ;;
  *) PLATFORM_TARGET=x86 ;;
esac

if [ ! -f "$APP_PROJECT" ]; then
  echo "错误：找不到项目文件 $APP_PROJECT"
  exit 1
fi

echo ""
echo "========================================"
echo "VOA 准直器手动调节工位 — 自包含发布"
echo "========================================"
echo "项目：$APP_PROJECT"
echo "配置：$CONFIGURATION"
echo "运行时：$RUNTIME_ID"
echo "输出目录：$OUTPUT_PATH"
echo "========================================"
echo ""

dotnet publish "$APP_PROJECT" \
  -c "$CONFIGURATION" \
  -r "$RUNTIME_ID" \
  -o "$OUTPUT_PATH" \
  --self-contained true \
  /p:DebugType=embedded \
  /p:DebugSymbols=true \
  /p:PublishReadyToRun=true \
  /p:PlatformTarget="$PLATFORM_TARGET"

if [ -d "$PROJECT_ROOT/config" ]; then
  rm -rf "$OUTPUT_PATH/config"
  cp -R "$PROJECT_ROOT/config" "$OUTPUT_PATH/config"
  echo "已复制配置目录：$OUTPUT_PATH/config"
fi

echo ""
echo "发布成功"
echo "输出位置：$OUTPUT_PATH"
echo ""
echo "可执行文件：$OUTPUT_PATH/VoaCollimator.App.exe"
echo "说明：目标机无需安装 .NET 运行时；请整目录拷贝部署。"
echo ""
