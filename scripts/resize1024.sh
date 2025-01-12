#!/bin/bash

if [ $# -eq 0 ]; then
	echo "Pass image file name"
	exit 1
fi

f="$1"
name=${f%.*}
ext="${f##*.}"

convert $f -resize 1024x1024 -background white -gravity center -extent 1024x1024 ${name}_resize.${ext}

echo "$f is resized to ${name}_resize.${ext}"
