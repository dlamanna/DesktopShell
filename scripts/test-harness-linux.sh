#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"

tmp_root="${DESKTOPSHELL_HARNESS_TMP:-/tmp/desktopshell-harness}"
mkdir -p "$tmp_root" "$tmp_root/obj" "$tmp_root/bin" "$tmp_root/TestResults" "$tmp_root/nuget/packages" "$tmp_root/.dotnet"

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

echo "[harness] restoring DesktopShell.sln"
dotnet restore DesktopShell.sln \
  "${common_props[@]}" \
  "${common_msbuild_flags[@]}" \
  --ignore-failed-sources

echo "[harness] running tests"
dotnet test DesktopShell.Tests/DesktopShell.Tests.csproj \
  -c Release \
  --no-restore \
  --results-directory "$tmp_root/TestResults" \
  "${common_props[@]}" \
  "${common_msbuild_flags[@]}" \
  "$@"
