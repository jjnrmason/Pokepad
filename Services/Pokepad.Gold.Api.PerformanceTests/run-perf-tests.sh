#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${BASE_URL:-}" ]]; then
  echo "BASE_URL is required" >&2
  exit 1
fi

if [[ -z "${TOKEN:-}" ]]; then
  echo "TOKEN is required" >&2
  exit 1
fi

if ! command -v k6 >/dev/null 2>&1; then
  echo "k6 is required. Install it from https://grafana.com/docs/k6/latest/set-up/install-k6/" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="${SCRIPT_DIR}/results"

mkdir -p "${RESULTS_DIR}"

run_script() {
  local name="$1"
  local script="$2"

  echo "Running ${name}..."
  k6 run \
    --summary-export "${RESULTS_DIR}/${name}.json" \
    "${SCRIPT_DIR}/scripts/${script}"
}

run_script "baseline" "baseline.js"
run_script "async-flow" "async-flow.js"
run_script "rate-limit-probe" "rate-limit-probe.js"

echo "Cold start is intentionally manual: wait 10 minutes, then run:"
echo "BASE_URL=\"${BASE_URL}\" TOKEN=\"***\" k6 run --summary-export \"${RESULTS_DIR}/cold-start.json\" \"${SCRIPT_DIR}/scripts/cold-start.js\""
