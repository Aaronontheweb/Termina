#!/usr/bin/env bash
#
# capture.sh - Regenerate Termina demo screenshots/GIFs with VHS.
#
# Builds every demo project once in Release, then runs each VHS tape.
# Tapes live in screenshots/tapes/*.tape and write their Output/Screenshot
# paths directly into docs/public/gallery/ (committed + auto-deployed by the
# existing docs.yml workflow).
#
# Usage:
#   ./screenshots/capture.sh                 # build + run every tape
#   ./screenshots/capture.sh counter         # build + run only counter.tape
#   ./screenshots/capture.sh counter.tape    # same (extension optional)
#
# Requirements: .NET 10 SDK, vhs, ttyd, ffmpeg (see screenshots/README.md).

set -eu

# --- Resolve repo root regardless of where the script is invoked from -------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
TAPES_DIR="${SCRIPT_DIR}/tapes"
OUTPUT_DIR="${REPO_ROOT}/docs/public/gallery"

cd "${REPO_ROOT}"

# --- Pre-flight: verify required tools are present --------------------------
for tool in dotnet vhs ttyd ffmpeg; do
  if ! command -v "${tool}" >/dev/null 2>&1; then
    echo "ERROR: required tool '${tool}' not found on PATH." >&2
    echo "       See screenshots/README.md for installation instructions." >&2
    exit 1
  fi
done

mkdir -p "${OUTPUT_DIR}"

# --- Build every demo once in Release (so tapes can use --no-build) ---------
echo "==> Building demo projects in Release configuration..."
for proj in demos/Termina.Demo \
            demos/Termina.Demo.Gallery \
            demos/Termina.Demo.Streaming \
            demos/Termina.Demo.Wizard \
            demos/Termina.Demo.RegionBased \
            demos/Termina.Demo.Grid; do
  echo "    building ${proj}"
  dotnet build "${proj}" -c Release --nologo -v quiet
done
echo "==> Build complete."

# --- Select tapes to run ----------------------------------------------------
if [ "$#" -ge 1 ]; then
  name="${1%.tape}"
  tape="${TAPES_DIR}/${name}.tape"
  if [ ! -f "${tape}" ]; then
    echo "ERROR: tape not found: ${tape}" >&2
    echo "Available tapes:" >&2
    ls -1 "${TAPES_DIR}"/*.tape 2>/dev/null | sed 's#.*/#  #' >&2
    exit 1
  fi
  tapes=("${tape}")
else
  tapes=("${TAPES_DIR}"/*.tape)
fi

# --- Run VHS over each tape -------------------------------------------------
echo "==> Recording ${#tapes[@]} tape(s) -> ${OUTPUT_DIR}"
for tape in "${tapes[@]}"; do
  case "$(basename "${tape}")" in
    _*) continue ;;   # skip helper tapes prefixed with underscore
  esac
  echo ""
  echo "--> vhs $(basename "${tape}")"
  vhs "${tape}"
done

echo ""
echo "==> Done. Generated assets in docs/public/gallery/:"
ls -lh "${OUTPUT_DIR}"
