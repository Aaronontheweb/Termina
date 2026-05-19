#!/usr/bin/env bash
#
# capture.sh - Regenerate Termina demo screenshots/GIFs with VHS.
#
# Builds the solution in Release, then runs each VHS tape. Tapes live in
# screenshots/tapes/*.tape and write their Output/Screenshot paths directly
# into docs/public/gallery/ (committed + auto-deployed by the existing
# docs.yml workflow).
#
# Usage:
#   ./screenshots/capture.sh                # build + run every tape
#   ./screenshots/capture.sh wizard         # build + run only wizard.tape
#   ./screenshots/capture.sh wizard.tape    # same (extension optional)
#
# Requirements: .NET 10 SDK, vhs, ttyd, ffmpeg (see screenshots/README.md).

set -eu

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
TAPES_DIR="${SCRIPT_DIR}/tapes"
OUTPUT_DIR="${REPO_ROOT}/docs/public/gallery"

cd "${REPO_ROOT}"

for tool in dotnet vhs ttyd ffmpeg; do
  if ! command -v "${tool}" >/dev/null 2>&1; then
    echo "ERROR: required tool '${tool}' not found on PATH." >&2
    echo "       See screenshots/README.md for installation instructions." >&2
    exit 1
  fi
done

mkdir -p "${OUTPUT_DIR}"

# One solution build so every tape can launch with --no-build.
echo "==> Building solution (Release)..."
dotnet build Termina.slnx -c Release --nologo -v quiet

if [ "$#" -ge 1 ]; then
  name="${1%.tape}"
  tape="${TAPES_DIR}/${name}.tape"
  if [ ! -f "${tape}" ]; then
    echo "ERROR: tape not found: ${tape}" >&2
    echo "Available tapes:" >&2
    ls -1 "${TAPES_DIR}"/*.tape 2>/dev/null | grep -v '/_' | sed 's#.*/#  #' >&2
    exit 1
  fi
  tapes=("${tape}")
else
  tapes=("${TAPES_DIR}"/*.tape)
fi

echo "==> Recording ${#tapes[@]} tape(s) -> ${OUTPUT_DIR}"
for tape in "${tapes[@]}"; do
  # Files prefixed with "_" are shared headers Source'd by real tapes.
  case "$(basename "${tape}")" in _*) continue ;; esac
  echo ""
  echo "--> vhs $(basename "${tape}")"
  vhs "${tape}"
done

echo ""
echo "==> Done. Generated assets in docs/public/gallery/:"
ls -lh "${OUTPUT_DIR}"
