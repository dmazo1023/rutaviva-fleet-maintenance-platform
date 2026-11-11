#!/usr/bin/env bash
# Deterministic gate for one wave of an OpenSpec change executed by parallel agents.
# Exit codes: 0 = pass, 1 = fail, 2 = fail in --hook mode (blocks the subagent in Claude Code).

set -uo pipefail
export DOTNET_CLI_UI_LANGUAGE=en
export DOTNET_NOLOGO=1

CHANGE=""
OWNED=""
FAST=0
HOOK=0
BASE="HEAD"
BUILD_CMD="dotnet build --no-restore"
TEST_CMD="dotnet test --no-build"
BASELINE=""
MAX_LINES=30

usage() {
  cat <<'EOF'
Usage: verify-wave.sh --change <name> --owned <owned-files.txt> [options]

Options:
  --fast              Only ownership and forbidden-pattern checks (no validate, build or tests)
  --hook              Exit with code 2 and write to stderr on failure (Claude Code SubagentStop)
  --base <ref>        Git reference to diff against (default: HEAD)
  --build "<cmd>"     Build command (default: dotnet build --no-restore)
  --test "<cmd>"      Test command (default: dotnet test --no-build)
  --baseline <n>      Minimum total number of tests expected
  -h, --help          Show this help
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --change) CHANGE="$2"; shift 2 ;;
    --owned) OWNED="$2"; shift 2 ;;
    --fast) FAST=1; shift ;;
    --hook) HOOK=1; shift ;;
    --base) BASE="$2"; shift 2 ;;
    --build) BUILD_CMD="$2"; shift 2 ;;
    --test) TEST_CMD="$2"; shift 2 ;;
    --baseline) BASELINE="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
  esac
done

fail() {
  local check="$1"
  local detail="$2"
  local message
  message="$(printf 'VERIFY-WAVE FAILED [%s]\n%s\n' "$check" "$detail" | head -n "$MAX_LINES")"
  if [[ $HOOK -eq 1 ]]; then
    echo "$message" >&2
    exit 2
  fi
  echo "$message"
  exit 1
}

pass() {
  echo "VERIFY-WAVE OK [$1]"
}

[[ -z "$CHANGE" ]] && fail "args" "--change is required"
[[ -z "$OWNED" || ! -f "$OWNED" ]] && fail "args" "--owned file not found: $OWNED"

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null)" || fail "args" "Not inside a git repository"
cd "$REPO_ROOT" || fail "args" "Cannot enter repository root"

CHANGED="$( { git -c core.quotepath=false diff --name-only "$BASE"; git -c core.quotepath=false ls-files --others --exclude-standard; } | sort -u )"

is_owned() {
  local file="$1"
  local entry
  [[ "$file" == "openspec/changes/$CHANGE/tasks.md" ]] && return 0
  [[ "$file" == "openspec/changes/$CHANGE/parallel-execution.md" ]] && return 0
  while IFS= read -r entry || [[ -n "$entry" ]]; do
    entry="${entry%$'\r'}"
    [[ -z "$entry" || "$entry" == \#* ]] && continue
    if [[ "$entry" == */ ]]; then
      [[ "$file" == "$entry"* ]] && return 0
    else
      [[ "$file" == "$entry" ]] && return 0
    fi
  done < "$OWNED"
  return 1
}

OUTSIDE=""
while IFS= read -r file; do
  [[ -z "$file" ]] && continue
  is_owned "$file" || OUTSIDE+="$file"$'\n'
done <<< "$CHANGED"
[[ -n "$OUTSIDE" ]] && fail "1-ownership" "Files changed outside owned-files.txt:"$'\n'"$OUTSIDE"
pass "1-ownership"

VIOLATIONS=""
# Only new content is checked: whole file when untracked, added lines when tracked.
# Pre-existing content (comments, generated headers) is never flagged.
added_lines() {
  local file="$1"
  if git ls-files --error-unmatch -- "$file" >/dev/null 2>&1; then
    git diff -U0 "$BASE" -- "$file" | grep -E '^\+' | grep -vE '^\+\+\+ ' | sed 's/^+//'
  else
    cat -- "$file"
  fi
}

while IFS= read -r file; do
  [[ -z "$file" || ! -f "$file" ]] && continue
  case "$file" in
    *.Designer.cs|*/Migrations/*.cs) ;;
    *.cs)
      content="$(added_lines "$file")"
      hits="$(printf '%s\n' "$content" | grep -E '(^|[^[:alnum:]_])var[[:space:]]+[[:alpha:]_]' | sed "s|^|$file:var:|")"
      [[ -n "$hits" ]] && VIOLATIONS+="$hits"$'\n'
      hits="$(printf '%s\n' "$content" | grep -E '^[[:space:]]*(//|/\*)|[;{})][[:space:]]*(//|/\*)' | sed "s|^|$file:comment:|")"
      [[ -n "$hits" ]] && VIOLATIONS+="$hits"$'\n'
      ;;
    src/*.sql|*.resx)
      content="$(added_lines "$file")"
      hits="$(printf '%s\n' "$content" | grep -E '^[[:space:]]*--([^>]|$)' | sed "s|^|$file:sql-comment:|")"
      [[ -n "$hits" ]] && VIOLATIONS+="$hits"$'\n'
      ;;
  esac
done <<< "$CHANGED"
[[ -n "$VIOLATIONS" ]] && fail "2-forbidden-patterns" "$VIOLATIONS"
pass "2-forbidden-patterns"

if [[ $FAST -eq 1 ]]; then
  echo "VERIFY-WAVE PASSED (fast)"
  exit 0
fi

OUTPUT="$(openspec validate "$CHANGE" --strict 2>&1)" || fail "3-openspec" "$OUTPUT"
pass "3-openspec"

OUTPUT="$(eval "$BUILD_CMD" 2>&1)" || fail "4-build" "$(echo "$OUTPUT" | grep -E 'error|Error' | head -n "$MAX_LINES")"
pass "4-build"

OUTPUT="$(eval "$TEST_CMD" 2>&1)"
STATUS=$?
[[ $STATUS -ne 0 ]] && fail "5-tests" "$(echo "$OUTPUT" | grep -E 'Failed|failed|Error' | head -n "$MAX_LINES")"

if [[ -n "$BASELINE" ]]; then
  TOTAL="$(echo "$OUTPUT" | sed -E 's/\x1B\[[0-9;]*[A-Za-z]//g' | grep -oiE 'total( tests)?:[[:space:]]*[0-9]+' | grep -oE '[0-9]+$' | awk '{ sum += $1 } END { print sum + 0 }')"
  [[ "$TOTAL" -lt "$BASELINE" ]] && fail "5-tests" "Total tests $TOTAL is below baseline $BASELINE"
fi
pass "5-tests"

echo "VERIFY-WAVE PASSED"
exit 0
