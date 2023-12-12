import json
import os
from typing import Dict

from toolbelt.constants import DATA_DIR
from datetime import datetime


def generate_latest(version: int, commit_hash: str) -> Dict:
    # 주어진 JSON 파일의 경로
    json_file_path = os.path.abspath(os.path.join(DATA_DIR, "latest.json"))

    # JSON 파일 열기
    with open(json_file_path, "r") as json_file:
        data = json.load(json_file)

    # 정보 수정
    data["version"] = version

    # files의 path 앞에 version 변경
    for file_info in data["files"]:
        _, artifact = file_info["path"].split("/")
        file_info["path"] = f"{version}/{artifact}"

    data["commit-hash"] = commit_hash
    data["timestamp"] = datetime.utcnow().isoformat(timespec="milliseconds") + "Z"

    return data


def create_version_json(platform: str, commit_hash: str, version: int, file_path: str):
    json_file_path = os.path.abspath(os.path.join(DATA_DIR, "version.json"))
    with open(json_file_path, "r") as json_file:
        data = json.load(json_file)

    data["version"] = version
    data["os"] = platform
    data["commit-hash"] = commit_hash
    data["timestamp"] = datetime.utcnow().isoformat(timespec="milliseconds") + "Z"

    with open(file_path, "w") as json_file:
        json.dump(data, json_file, indent=4)
