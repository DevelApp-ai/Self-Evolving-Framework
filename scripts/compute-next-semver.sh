#!/usr/bin/env bash
set -euo pipefail

default_version="${DEFAULT_VERSION:-1.0.0}"
bump_part="${SEMVER_BUMP:-patch}"

if [[ "${TAG_LIST+x}" == "x" ]]; then
  tags="${TAG_LIST}"
else
  tags="$(git tag --list 'v[0-9]*.[0-9]*.[0-9]*' | sed 's/^v//')"
fi

latest_version="$(
  printf '%s\n' "${tags}" \
    | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' \
    | sort -V \
    | tail -n1 || true
)"

if [[ -z "${latest_version}" ]]; then
  echo "${default_version}"
  exit 0
fi

IFS='.' read -r major minor patch <<< "${latest_version}"

case "${bump_part}" in
  patch)
    echo "${major}.${minor}.$((patch + 1))"
    ;;
  minor)
    echo "${major}.$((minor + 1)).0"
    ;;
  major)
    echo "$((major + 1)).0.0"
    ;;
  *)
    echo "Unsupported SEMVER_BUMP value: ${bump_part}. Expected one of: patch, minor, major." >&2
    exit 1
    ;;
esac
