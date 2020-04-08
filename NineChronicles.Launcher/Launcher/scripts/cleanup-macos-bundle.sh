#!/bin/bash
set -e

if ! command -v realpath > /dev/null; then
  # macOS does not have realpath(1)
  realpath() {
    python2.7 -c 'import os.path, sys; print os.path.abspath(sys.argv[1]),' "$@"
  }
fi

if [[ "$#" = "0" || "$#" = "1" ]]; then
  {
    echo "error: too few arguments"
    echo "usage: $0 DIR EXCLUDES..."
    echo "Deletes all files and directories in the DIR except for EXCLUDES."
  } > /dev/stderr
fi

publish_dir="$1"

declare -a excludes
for f in "$@"; do
  excludes+=( "$(realpath "$f")" )
done

for f in "$publish_dir"/*; do
  f="$(realpath "$f")"
  if ( dlm=$'\x1F' ; IFS="$dlm" ; [[ "$dlm${excludes[*]}$dlm" == *"$dlm${f}$dlm"* ]] ) ; then
    echo "Remained: $f" > /dev/stderr
  else
    rm -r "$f"
    echo "Removed:  $f" > /dev/stderr
  fi
done
