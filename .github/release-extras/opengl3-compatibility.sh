#!/bin/sh
printf '\033c\033]0;%s\a' Rhythia
base_path="$(dirname "$(realpath "$0")")"
exec "$base_path/Rhythia.x86_64" --rendering-driver opengl3 "$@"
