#!/bin/bash
set -e

if [[ "$CI" = true || "$DEBUG" != "" ]]; then
  set -vx
fi

if ! command -v python3 > /dev/null; then
  {
    echo "error: python3, a prerequisite, is not installed on the system."
  } > /dev/stderr
  exit 1
fi

venv_dir="$(dirname "$0")/.pack-venv"
script_dir="$(dirname "$0")/../../tools/pack"

if [[ ! -d "$venv_dir" ]]; then
  python3 -m venv "$venv_dir"
fi

# shellcheck disable=SC1090
. "$venv_dir/bin/activate"

"$venv_dir/bin/pip" install wheel
"$venv_dir/bin/pip" install -r "$script_dir/requirements.txt"
"$venv_dir/bin/python3" "$script_dir/pack.py" "$@"
exit $?
