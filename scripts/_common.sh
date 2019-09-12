#!/bin/bash

title() {
  printf '=%.0s' {0..79}
  echo
  echo "$1"
  printf '=%.0s' {0..79}
  echo
}
