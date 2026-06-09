#!/usr/bin/env bash
set -euo pipefail

default_version="${DEFAULT_VERSION:-1.0.0}"

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
echo "${major}.${minor}.$((patch + 1))"
