FROM gableroux/unity3d:2019.1.0f2 AS build

ARG ulf

ENV ULF=$ulf

ADD nekoyume /src
ADD scripts /scripts
RUN /scripts/build.sh

FROM bitnami/minideb:stretch

COPY --from=build /src/Build/LinuxHeadless /app
VOLUME /data

ENTRYPOINT ["/app/nekoyume", "--storage-path=/data/planetarium"]
