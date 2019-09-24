#!/bin/bash

PORT=$1

/nekoyume/nekoyume --host host.docker.internal --port "$PORT" --private-key fbc20042b3a707a7d5a1fa577171f49cd3a9e67ab9295757c714e3f2f8c2d573 --console-sink true | tee /root/.config/seed.log
