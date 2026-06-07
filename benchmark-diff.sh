#!/usr/bin/env bash

# Exit immediately if a command exits with a non-zero status
set -e

# Keep track of repository state for cleanup
INITIAL_BRANCH=""
INITIAL_COMMIT=""
CHECKED_OUT_BASELINE=false
STASHED_CHANGES=false
TEMP_CHANGES_CSV=""
TEMP_BASELINE_CSV=""

# Retrieve repository root directory
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

# Ensure we are in a git repository with at least one commit
if ! git rev-parse --verify HEAD >/dev/null 2>&1; then
  echo "Error: Repository has no commits yet."
  exit 1
fi

# Ensure the script itself and benchmark artifacts are locally ignored
# so they are not stashed by git stash -u and do not cause pop conflicts.
ensure_local_ignores() {
  local exclude_file=".git/info/exclude"
  if [ -f "$exclude_file" ]; then
    for pattern in "benchmark-diff.sh" "BenchmarkDotNet.Artifacts/" "artifacts/"; do
      if ! grep -qxF "$pattern" "$exclude_file" 2>/dev/null; then
        echo "$pattern" >> "$exclude_file"
      fi
    done
  fi
}

ensure_local_ignores

# Cleanup function to restore repository state on exit
cleanup() {
  echo ""
  echo "Cleaning up and restoring repository state..."
  
  if [ "$CHECKED_OUT_BASELINE" = true ]; then
    echo "Returning to initial branch/commit: $INITIAL_BRANCH"
    git checkout -q "$INITIAL_BRANCH"
  fi
  
  if [ "$STASHED_CHANGES" = true ]; then
    echo "Unstashing changes..."
    # Delete any newly generated benchmark artifacts to avoid conflicts
    rm -rf "$REPO_ROOT/BenchmarkDotNet.Artifacts"
    # Suppress output unless error
    if ! git stash pop -q; then
      echo "Warning: git stash pop failed. You may need to resolve conflicts manually."
      echo "Stashed changes are saved in the stash list."
    fi
  fi
  
  # Remove temporary CSV files
  if [ -n "$TEMP_CHANGES_CSV" ] && [ -f "$TEMP_CHANGES_CSV" ]; then
    rm -f "$TEMP_CHANGES_CSV"
  fi
  if [ -n "$TEMP_BASELINE_CSV" ] && [ -f "$TEMP_BASELINE_CSV" ]; then
    rm -f "$TEMP_BASELINE_CSV"
  fi
  
  echo "Cleanup completed."
}

# Register the cleanup handler
trap cleanup EXIT

# Parse arguments
BASELINE_COMMIT=""
DOTNET_ARGS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --baseline|-b)
      if [[ -z "$2" ]]; then
        echo "Error: --baseline requires a commit ID/ref."
        exit 1
      fi
      BASELINE_COMMIT="$2"
      shift 2
      ;;
    --help|-h)
      echo "Usage: $0 [options] [-- [dotnet-benchmark-args]]"
      echo ""
      echo "Options:"
      echo "  -b, --baseline <commit-id>   The git commit/branch to use as baseline (default: clean HEAD)"
      echo "  -h, --help                   Show this help message"
      echo ""
      echo "All arguments after '--' or unrecognized options will be forwarded to the BenchmarkDotNet runner."
      echo "Example:"
      echo "  $0 --baseline main -- --filter '*GenJson*' --job Dry"
      exit 0
      ;;
    --)
      shift
      DOTNET_ARGS+=("$@")
      break
      ;;
    *)
      DOTNET_ARGS+=("$1")
      shift
      ;;
  esac
done

# If no filter is specified, default to GenJson methods
HAS_FILTER=false
for arg in "${DOTNET_ARGS[@]}"; do
  if [[ "$arg" == "--filter" ]] || [[ "$arg" == -f ]] || [[ "$arg" == --filter=* ]] || [[ "$arg" == -f=* ]]; then
    HAS_FILTER=true
    break
  fi
done

if [ "$HAS_FILTER" = false ]; then
  DOTNET_ARGS+=("--filter" "*GenJson*")
fi

# Check if there are any uncommitted changes (tracked, unstaged, or untracked)
HAS_CHANGES=false
if ! git diff --quiet || ! git diff --cached --quiet || [ -n "$(git status --porcelain | grep '??')" ]; then
  HAS_CHANGES=true
fi

# If there are no changes and no baseline specified, there is nothing to compare
if [ "$HAS_CHANGES" = false ] && [ -z "$BASELINE_COMMIT" ]; then
  echo "No uncommitted changes detected, and no baseline commit specified."
  echo "Please specify a baseline commit to compare against (e.g., --baseline main) or run with uncommitted changes."
  exit 1
fi

# Get current branch or commit
INITIAL_BRANCH=$(git symbolic-ref --short -q HEAD || git rev-parse HEAD)
INITIAL_COMMIT=$(git rev-parse HEAD)

# Define temp file paths in artifacts directory (which is ignored by git)
mkdir -p "$REPO_ROOT/artifacts"
TEMP_CHANGES_CSV="$REPO_ROOT/artifacts/benchmark_changes.csv"
TEMP_BASELINE_CSV="$REPO_ROOT/artifacts/benchmark_baseline.csv"

# Remove any old temp files from prior runs
rm -f "$TEMP_CHANGES_CSV" "$TEMP_BASELINE_CSV"

# Phase 1: Run benchmark on the current changes (working tree)
echo "=== Phase 1: Gathering benchmark data for CHANGES ==="
# Delete any existing BenchmarkDotNet reports to avoid picking up outdated files
rm -f "$REPO_ROOT/BenchmarkDotNet.Artifacts/results/"*-report.csv

echo "Running benchmark..."
# We run dotnet run from the root. If it fails, script will exit and trap cleanup will trigger
dotnet run -c Release --project "$REPO_ROOT/src/GenJson.Benchmark" -- "${DOTNET_ARGS[@]}"

# Locate the generated CSV report
REPORT_CSV=$(ls -t "$REPO_ROOT/BenchmarkDotNet.Artifacts/results/"*-report.csv 2>/dev/null | head -n 1)
if [ -z "$REPORT_CSV" ] || [ ! -f "$REPORT_CSV" ]; then
  echo "Error: Benchmark failed to generate a CSV report."
  exit 1
fi

cp "$REPORT_CSV" "$TEMP_CHANGES_CSV"
echo "Changes benchmark results gathered."
echo ""

# Phase 2: Stash changes and move to baseline
if [ "$HAS_CHANGES" = true ]; then
  echo "Stashing changes..."
  git stash push -u -m "benchmark-diff-temp-stash"
  STASHED_CHANGES=true
fi

if [ -n "$BASELINE_COMMIT" ]; then
  echo "Checking out baseline commit: $BASELINE_COMMIT"
  git checkout "$BASELINE_COMMIT"
  CHECKED_OUT_BASELINE=true
else
  echo "No baseline commit specified. Using clean HEAD ($INITIAL_COMMIT) as baseline."
fi

# Phase 3: Run benchmark on the baseline
echo "=== Phase 2: Gathering benchmark data for BASELINE ==="
rm -f "$REPO_ROOT/BenchmarkDotNet.Artifacts/results/"*-report.csv

echo "Running benchmark..."
dotnet run -c Release --project "$REPO_ROOT/src/GenJson.Benchmark" -- "${DOTNET_ARGS[@]}"

REPORT_CSV=$(ls -t "$REPO_ROOT/BenchmarkDotNet.Artifacts/results/"*-report.csv 2>/dev/null | head -n 1)
if [ -z "$REPORT_CSV" ] || [ ! -f "$REPORT_CSV" ]; then
  echo "Error: Benchmark failed to generate a CSV report for baseline."
  exit 1
fi

cp "$REPORT_CSV" "$TEMP_BASELINE_CSV"
echo "Baseline benchmark results gathered."
echo ""

# Phase 4: Compare results
echo "=== Phase 3: Benchmark Comparison ==="
awk -F';' '
# Clean value helper
function clean_val(val) {
  gsub("\"", "", val);
  gsub(",", "", val);
  gsub(/[ a-zA-Z]/, "", val);
  if (val == "" || val ~ /^ *$/) return 0;
  return val + 0;
}

BEGIN {
  # Color codes
  C_GREEN = "\033[0;32m"
  C_RED = "\033[0;31m"
  C_RESET = "\033[0m"
  C_BOLD = "\033[1m"
  C_CYAN = "\033[0;36m"
  
  col_method = 1
  col_mean = 45
  col_allocated = 48
}

# First file: baseline.csv
NR == FNR {
  if (FNR == 1) {
    # Dynamically find column indices
    for (i = 1; i <= NF; i++) {
      if ($i == "Method") col_method = i;
      if ($i == "Mean [ns]") col_mean = i;
      if ($i == "Allocated [KB]") col_allocated = i;
    }
    next;
  }
  
  method = $col_method;
  gsub("\"", "", method);
  
  base_time_raw[method] = $col_mean;
  base_mem_raw[method] = $col_allocated;
  
  base_time[method] = clean_val($col_mean);
  base_mem[method] = clean_val($col_allocated);
  next;
}

# Second file: changes.csv
{
  if (FNR == 1) {
    # Dynamically find column indices for changes in case they differ
    for (i = 1; i <= NF; i++) {
      if ($i == "Method") col_method = i;
      if ($i == "Mean [ns]") col_mean = i;
      if ($i == "Allocated [KB]") col_allocated = i;
    }
    next;
  }
  
  method = $col_method;
  gsub("\"", "", method);
  
  # Keep track of methods in order
  if (!(method in seen_methods)) {
    methods[method_count++] = method;
    seen_methods[method] = 1;
  }
  
  change_time_raw[method] = $col_mean;
  change_mem_raw[method] = $col_allocated;
  
  change_time[method] = clean_val($col_mean);
  change_mem[method] = clean_val($col_allocated);
}

END {
  print C_BOLD "Benchmark Comparison Summary" C_RESET
  print "================================================================================="
  
  for (i = 0; i < method_count; i++) {
    m = methods[i];
    
    print C_BOLD C_CYAN "Method: " m C_RESET
    
    # Check if we have baseline data for this method
    if (!(m in base_time)) {
      print "  No baseline data available."
      print "---------------------------------------------------------------------------------"
      continue;
    }
    
    b_t = base_time[m];
    c_t = change_time[m];
    b_m = base_mem[m];
    c_m = change_mem[m];
    
    b_t_raw = base_time_raw[m];
    c_t_raw = change_time_raw[m];
    b_m_raw = base_mem_raw[m];
    c_m_raw = change_mem_raw[m];
    
    gsub("\"", "", b_t_raw);
    gsub("\"", "", c_t_raw);
    gsub("\"", "", b_m_raw);
    gsub("\"", "", c_m_raw);
    
    # Time comparison
    diff_t = c_t - b_t;
    if (b_t > 0) {
      pct_t = (diff_t / b_t) * 100;
      pct_t_str = sprintf("%+.2f%%", pct_t);
    } else {
      pct_t_str = "N/A";
    }
    
    if (diff_t < -0.01) {
      diff_t_str = sprintf("%s%+.1f ns (%s)%s", C_GREEN, diff_t, pct_t_str, C_RESET);
    } else if (diff_t > 0.01) {
      diff_t_str = sprintf("%s%+.1f ns (%s)%s", C_RED, diff_t, pct_t_str, C_RESET);
    } else {
      diff_t_str = "0.0 ns (+0.00%)";
    }
    
    # Memory comparison
    diff_m = c_m - b_m;
    if (b_m > 0) {
      pct_m = (diff_m / b_m) * 100;
      pct_m_str = sprintf("%+.2f%%", pct_m);
    } else {
      pct_m_str = "N/A";
    }
    
    if (diff_m < -0.001) {
      diff_m_str = sprintf("%s%+.3f KB (%s)%s", C_GREEN, diff_m, pct_m_str, C_RESET);
    } else if (diff_m > 0.001) {
      diff_m_str = sprintf("%s%+.3f KB (%s)%s", C_RED, diff_m, pct_m_str, C_RESET);
    } else {
      diff_m_str = "0.000 KB (+0.00%)";
    }
    
    print "  " C_BOLD "Runtime:" C_RESET
    printf "    Baseline: %-20s\n", b_t_raw
    printf "    Changes:  %-20s\n", c_t_raw
    printf "    Diff:     %-20s\n", diff_t_str
    
    print "  " C_BOLD "Memory:" C_RESET
    printf "    Baseline: %-20s\n", b_m_raw
    printf "    Changes:  %-20s\n", c_m_raw
    printf "    Diff:     %-20s\n", diff_m_str
    print "---------------------------------------------------------------------------------"
  }
}
' "$TEMP_BASELINE_CSV" "$TEMP_CHANGES_CSV"
