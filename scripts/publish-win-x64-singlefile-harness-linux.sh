#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
project_path="$repo_root/DesktopShell/DesktopShell.csproj"

configuration="${CONFIGURATION:-Release}"
runtime="${RUNTIME:-win-x64}"
self_contained="${SELF_CONTAINED:-true}"
include_native="${INCLUDE_NATIVE_LIBRARIES_FOR_SELF_EXTRACT:-true}"

tmp_root="${DESKTOPSHELL_HARNESS_TMP:-/tmp/desktopshell-harness}"
mkdir -p "$tmp_root" "$tmp_root/obj" "$tmp_root/bin" "$tmp_root/publish" "$tmp_root/nuget/packages" "$tmp_root/.dotnet"

export DOTNET_CLI_HOME="$tmp_root/.dotnet"
export NUGET_PACKAGES="$tmp_root/nuget/packages"
export NUGET_HTTP_CACHE_PATH="$tmp_root/nuget/http-cache"
export NUGET_SCRATCH="$tmp_root/nuget/scratch"
mkdir -p "$NUGET_HTTP_CACHE_PATH" "$NUGET_SCRATCH"

fallback_packages=""
for d in /mnt/c/Users/*/.nuget/packages; do
  if [[ -d "$d" ]]; then
    fallback_packages="$d"
    break
  fi
done

common_props=(
  "-p:EnableWindowsTargeting=true"
  "-p:BaseIntermediateOutputPath=$tmp_root/obj/"
  "-p:BaseOutputPath=$tmp_root/bin/"
  "-p:RestorePackagesPath=$NUGET_PACKAGES"
)

if [[ -n "$fallback_packages" ]]; then
  common_props+=("-p:RestoreFallbackFolders=$fallback_packages")
else
  common_props+=("-p:RestoreFallbackFolders=")
fi

common_msbuild_flags=(
  "--disable-build-servers"
  "-m:1"
  "/nodeReuse:false"
)

cd "$repo_root"

echo "[harness] restoring DesktopShell"
dotnet restore "$project_path" \
  "${common_props[@]}" \
  "${common_msbuild_flags[@]}" \
  --ignore-failed-sources

echo "[harness] publishing single-file win-x64"
dotnet publish "$project_path" \
  -c "$configuration" \
  -r "$runtime" \
  --self-contained "$self_contained" \
  -p:PublishSingleFile=true \
  "-p:IncludeNativeLibrariesForSelfExtract=$include_native" \
  "-p:PublishDir=$tmp_root/publish/" \
  --no-restore \
  "${common_props[@]}" \
  "${common_msbuild_flags[@]}" \
  "$@"

echo "[harness] publish output: $tmp_root/publish"
