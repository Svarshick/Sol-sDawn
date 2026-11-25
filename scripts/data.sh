#!/bin/sh
set -e
cd "$(git rev-parse --show-toplevel)"
dir=SolsDawn.Gameplay/_Data

fetch() { git fetch -q origin 'refs/tags/data/*:refs/tags/data/*'; }
tags() { git for-each-ref --sort=-v:refname --sort=-creatordate --format='%(refname:short)' refs/tags/data/; }

case "$1" in
save)
  [ -d "$dir" ] || { echo "no $dir to save"; exit 1; }
  fetch
  head=$(git rev-parse --short HEAD)
  git merge-base --is-ancestor HEAD "origin/$(git rev-parse --abbrev-ref HEAD)" 2>/dev/null \
    || echo "warning: $head is not pushed, the snapshot will point to a commit others cannot fetch"
  index=$(mktemp)
  GIT_INDEX_FILE=$index git read-tree HEAD
  GIT_INDEX_FILE=$index git add -f "$dir"
  tree=$(GIT_INDEX_FILE=$index git write-tree)
  rm -f "$index"
  tag=data/$head
  n=2
  while git rev-parse -q --verify "refs/tags/$tag" >/dev/null; do tag=data/$head-$n; n=$((n + 1)); done
  commit=$(git commit-tree "$tree" -p HEAD -m "data snapshot for $head")
  git tag "$tag" "$commit"
  git push -q origin "$tag"
  echo "saved $tag"
  ;;
list)
  fetch
  for tag in $(tags); do
    git log -1 --format="$tag  %cr  %s" "$tag"
    if [ "$2" = -v ]; then git log -1 --format='%s%n' "$tag^"; fi
  done
  ;;
load)
  fetch
  tag=${2:-$(tags | head -n 1)}
  [ -n "$tag" ] || { echo "no data tags"; exit 1; }
  case "$tag" in data/*) ;; *) tag=data/$tag ;; esac
  git rev-parse -q --verify "refs/tags/$tag" >/dev/null || { echo "no tag $tag"; exit 1; }
  if [ -d "$dir" ]; then
    rm -rf "$dir.bak"
    mv "$dir" "$dir.bak"
    echo "previous data moved to $dir.bak"
  fi
  git archive "$tag" "$dir" | tar -x
  base=$(git rev-parse --short "$tag^")
  echo "loaded $tag"
  git merge-base --is-ancestor "$base" HEAD || echo "warning: $tag was made on $base, which is not in your branch"
  ;;
help)
  cat <<EOF
git data: share $dir snapshots through data/* tags.

  git data save          Snapshot the local _Data on the current commit, tag it
                         data/<hash> (data/<hash>-2, -3... on repeat saves) and push.
                         Staged files and the working tree are not touched.
  git data list          Fetch and list snapshots, newest first.
  git data list -v       Same, plus the subject of the code commit each snapshot was made on.
  git data load          Replace the local _Data with the newest snapshot.
  git data load <tag>    Replace the local _Data with the given snapshot (data/ prefix optional).
                         The previous local _Data is moved to _Data.bak.
  git data help          Show this help.
EOF
  ;;
*)
  echo "usage: git data save | list [-v] | load [tag] | help"
  exit 1
  ;;
esac
