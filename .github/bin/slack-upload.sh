#!/bin/bash
# 빌드 산출물(APK·AAB)을 슬랙 채널에 올린다. 릴리즈 빌드에서 QA 가 슬랙만 보고 받아 가게 하려는 편의 기능이다.
#
# 사용법: slack-upload.sh <라벨> <파일> <설명> [<파일> <설명> ...]
#   예) slack-upload.sh "Android M" /tmp/slack-apk/NineChronicles-M-510.0.1-123.apk "QA 설치용"
#   메시지: [Player] Prepared 'Android M' apk - <파일명> (<MB>MB, <설명>)
#
# 환경 변수:
#   SLACK_TOKEN, SLACK_CHANNEL  internal 환경 시크릿. 둘 중 하나라도 없으면 아무것도 안 하고 0 으로 끝낸다.
#   RUN_URL                     실패·한도 초과 때 "아티팩트로 받으세요" 안내에 붙일 워크플로 실행 링크.
#
# 동작은 android-build-and-release.yml 의 원스토어 슬랙 스텝과 같다.
# 호출하는 스텝에는 반드시 continue-on-error 를 건다 — 슬랙 쪽 문제로 릴리즈 빌드가 깨지면 안 된다.
#
# 체크아웃이 core.autocrlf=true 라 CRLF 로 풀린다. `tr -d "\r"` 로 CR 을 지운 사본을 실행할 것.
set -euo pipefail

if [ "$#" -lt 3 ] || [ $((($# - 1) % 2)) -ne 0 ]; then
  echo "사용법: $0 <라벨> <파일> <설명> [<파일> <설명> ...]" >&2
  exit 2
fi

label=$1
shift

SLACK_TOKEN=${SLACK_TOKEN:-}
SLACK_CHANNEL=${SLACK_CHANNEL:-}
RUN_URL=${RUN_URL:-}

if [ -z "${SLACK_TOKEN}" ] || [ -z "${SLACK_CHANNEL}" ]; then
  echo "SLACK_TOKEN/SLACK_CHANNEL 이 없어 업로드를 건너뛴다."
  exit 0
fi

# chat.postMessage 는 채널 이름도 ID 도 받고, **응답의 .channel 은 항상 ID** 다.
# files.completeUploadExternal 이 ID 만 받으므로 여기서 얻는다(추가 스코프 불필요).
post_text() {
  local res
  res=$(curl -sS -X POST https://slack.com/api/chat.postMessage \
    -H "Authorization: Bearer ${SLACK_TOKEN}" \
    -H 'Content-Type: application/json; charset=utf-8' \
    --data "$(jq -nc --arg c "$SLACK_CHANNEL" --arg t "$1" '{channel:$c, text:$t}')")
  if ! echo "$res" | jq -e '.ok' > /dev/null; then
    echo "chat.postMessage 실패: $res" >&2
    return 1
  fi
  echo "$res" | jq -r '.channel'
}

# 토큰이 뭘 할 수 있는지 로그에 남긴다. 파일 업로드엔 files:write 가 필요하다. 스코프는 응답 헤더로 온다.
scopes=$(curl -sS -D - -o /dev/null -H "Authorization: Bearer ${SLACK_TOKEN}" \
  https://slack.com/api/auth.test | tr -d '\r' | awk -F': ' '/^[Xx]-[Oo][Aa]uth-[Ss]copes:/{print $2}') || true
echo "슬랙 토큰 스코프: ${scopes:-확인 불가}"

# 업로드가 안 되면 조용히 끝내지 않는다 — QA 는 슬랙만 보고 기다린다.
fallback() {
  echo "$1" >&2
  post_text "[Player] '${label}' ${2} 슬랙 업로드 실패 — 아티팩트로 받으세요: ${RUN_URL}" > /dev/null || true
}

failed=0

# $1=경로 $2=설명 — 파일 하나 실패해도 다음 파일은 올린다
upload() {
  local path=$1 note=$2 name kind size mb channel_id up res
  name=$(basename "$path")
  kind=${name##*.}
  if [ ! -s "$path" ]; then
    fallback "파일이 없거나 비어 있다: ${path}" "$kind"
    failed=1
    return 0
  fi
  size=$(wc -c < "$path" | tr -d ' ')
  mb=$((size / 1024 / 1024))
  # 슬랙 파일 한도는 1GB 다. 넘으면 올리지 않고 아티팩트 링크만 알린다.
  if [ "$size" -gt $((1024 * 1024 * 1024)) ]; then
    echo "${kind} 가 슬랙 한도(1GB)를 넘어 파일 업로드를 건너뛴다."
    post_text "[Player] Prepared '${label}' ${kind} (${name}, ${mb}MB, ${note}) — 슬랙 한도 초과라 아티팩트로 받으세요: ${RUN_URL}" > /dev/null \
      || failed=1
    return 0
  fi
  if ! channel_id=$(post_text "[Player] Prepared '${label}' ${kind} - ${name} (${mb}MB, ${note})"); then
    failed=1
    return 0
  fi
  echo "슬랙 채널 ID: ${channel_id}"
  # files.upload 은 폐기됐다. 외부 업로드 3단계를 쓴다.
  up=$(curl -sS -X POST https://slack.com/api/files.getUploadURLExternal \
    -H "Authorization: Bearer ${SLACK_TOKEN}" \
    --data-urlencode "filename=${name}" \
    --data-urlencode "length=${size}") || up='{}'
  echo "$up" | jq -e '.ok' > /dev/null || { fallback "getUploadURLExternal 실패: $up" "$kind"; failed=1; return 0; }
  curl -sS --fail -X POST "$(echo "$up" | jq -r '.upload_url')" \
    -F "file=@${path}" > /dev/null || { fallback "업로드 전송 실패" "$kind"; failed=1; return 0; }
  res=$(curl -sS -X POST https://slack.com/api/files.completeUploadExternal \
    -H "Authorization: Bearer ${SLACK_TOKEN}" \
    -H 'Content-Type: application/json; charset=utf-8' \
    --data "$(jq -nc \
      --arg id "$(echo "$up" | jq -r '.file_id')" \
      --arg t "$name" \
      --arg c "$channel_id" \
      '{files:[{id:$id, title:$t}], channel_id:$c}')") || res='{}'
  echo "$res" | jq -e '.ok' > /dev/null || { fallback "completeUploadExternal 실패: $res" "$kind"; failed=1; return 0; }
  echo "슬랙 업로드 완료: ${name}"
}

while [ "$#" -gt 0 ]; do
  upload "$1" "$2"
  shift 2
done

# 실패가 있었으면 스텝을 실패로 표시해 로그에서 보이게 한다(호출 스텝의 continue-on-error 가 빌드를 지킨다).
exit "$failed"
