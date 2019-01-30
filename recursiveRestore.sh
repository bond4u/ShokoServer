#!/usr/local/bin/bash

LIST=`find . -iname packages.config -exec dirname {} \;`
SOL=`pwd`
for D in $LIST; do
  pushd `pwd`
  echo "dir=$D"
  cd $D
  nuget restore -SolutionDirectory=$SOL
  popd
done
