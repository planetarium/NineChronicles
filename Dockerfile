FROM gableroux/unity3d:2019.1.0f2 AS build

ARG apt_source

RUN if [ "$apt_source" != "" ]; then \
  echo "Replace APT source to <$apt_source>."; \
  sed -i \
    "s|http://archive.ubuntu.com/ubuntu|$apt_source|" \
    /etc/apt/sources.list; \
  fi
RUN apt-get update || true && \
  apt-get install -y libxml2-utils xsltproc && \
  rm -rf /var/lib/apt/lists/*

ARG ulf

ENV ULF=$ulf

ADD nekoyume /src
ADD scripts /scripts
RUN chmod +x /scripts/*.sh

RUN /scripts/build.sh

FROM bitnami/minideb:stretch

RUN apt update && \
  apt install -y ca-certificates wget libc6-dev

COPY --from=build /src/Build/LinuxHeadless /app
COPY --from=build /scripts/entrypoint.sh /entrypoint.sh
VOLUME /data

ARG prior_dlls="prior_dlls"

ENV PRIOR_DLLS=$prior_dlls

ENTRYPOINT ["/entrypoint.sh", "--storage-path=/data/planetarium"]
