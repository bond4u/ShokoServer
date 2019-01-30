#!/bin/tcsh

xbuild /p:Configuration=CLI \
 /p:OutDir=/home/shoko/shoko/ |& tee build-rel.log
