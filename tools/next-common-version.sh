#!/usr/bin/env bash
# Prints the next LeiMa.Common* version.
#
# Usage: tools/next-common-version.sh <patch|minor|major> [prerelease-suffix]
#
# The base is the latest stable tag `common-vX.Y.Z` (all tags must be fetched). With no such tag
# this is the first release and the version is VersionPrefix from the package Directory.Build.props.
# A suffix such as `rc.1` makes a prerelease: 0.2.0-rc.1.
set -euo pipefail

bump="${1:?usage: next-common-version.sh <patch|minor|major> [prerelease-suffix]}"
suffix="${2:-}"

if [[ -n "$suffix" && ! "$suffix" =~ ^[0-9A-Za-z][0-9A-Za-z.-]*$ ]]; then
  echo "Invalid prerelease suffix '$suffix' (letters, digits, '.' and '-' only)." >&2
  exit 1
fi

last=$(git tag --list 'common-v*' --sort=-v:refname | grep -E '^common-v[0-9]+\.[0-9]+\.[0-9]+$' | head -n1 || true)

if [[ -z "$last" ]]; then
  props="$(dirname "$0")/../libs/dotnet/LeiMa.Common/Directory.Build.props"
  base=$(sed -n 's#.*<VersionPrefix>\(.*\)</VersionPrefix>.*#\1#p' "$props" | head -n1)
  if [[ -z "$base" ]]; then
    echo "No common-v* tag found and no VersionPrefix in $props." >&2
    exit 1
  fi
else
  IFS=. read -r major minor patch <<< "${last#common-v}"
  case "$bump" in
    major) base="$((major + 1)).0.0" ;;
    minor) base="${major}.$((minor + 1)).0" ;;
    patch) base="${major}.${minor}.$((patch + 1))" ;;
    *) echo "Unknown bump '$bump' (use patch, minor or major)." >&2; exit 1 ;;
  esac
fi

echo "${base}${suffix:+-$suffix}"
