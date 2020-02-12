#!/bin/bash

PORT=$1

apt update
apt install -y libc6-dev
/app/Nine\ Chronicles --host host.docker.internal --port "$PORT" --private-key fbc20042b3a707a7d5a1fa577171f49cd3a9e67ab9295757c714e3f2f8c2d573 --console-sink --storage-type rocksdb | tee /root/.config/seed.log
