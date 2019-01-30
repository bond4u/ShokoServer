#!/bin/tcsh

xbuild /p:Configuration=Debug /p:Configuration=CLI \
 /t:rebuild \
 /p:OutDir=/home/shoko/shokoDbg/ |& tee build-dbg.log
