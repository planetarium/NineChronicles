FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

COPY nekoyume /src
RUN dotnet build /src/Assets/_Scripts/NineChronicles.RPC.Shared/NineChronicles.RPC.Shared/NineChronicles.RPC.Shared.csproj

FROM unityci/editor:ubuntu-2020.3.4f1-linux-il2cpp-0.11 AS build

ARG apt_source

RUN if [ "$apt_source" != "" ]; then \
  echo "Replace APT source to <$apt_source>."; \
  sed -i \
    "s|http://archive.ubuntu.com/ubuntu|$apt_source|" \
    /etc/apt/sources.list; \
  fi
RUN apt-get update || true && \
  apt-get install -y libxml2-utils xsltproc git && \
  rm -rf /var/lib/apt/lists/*

ARG ulf

ENV ULF=$ulf

COPY --from=build-env /src /nekoyume
COPY scripts /scripts
RUN chmod +x /scripts/*.sh

RUN /scripts/build-player.sh LinuxHeadless

FROM bitnami/minideb:stretch

RUN apt update && \
  apt install -y ca-certificates wget libc6-dev

COPY --from=build /src/Build/LinuxHeadless /app
COPY --from=build /scripts/entrypoint.sh /entrypoint.sh
VOLUME /data

ARG prior_dlls="prior_dlls"

ENV PRIOR_DLLS=$prior_dlls

ENTRYPOINT ["/entrypoint.sh", "--storage-path=/data/planetarium"]
