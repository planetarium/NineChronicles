import json
import os
from typing import Optional
from datetime import datetime

import structlog

from toolbelt.client import GithubClient
from toolbelt.client.aws import S3File
from toolbelt.config import config
from toolbelt.constants import BINARY_FILENAME_MAP, RELEASE_BUCKET
from toolbelt.github.constants import GITHUB_ORG, PLAYER_REPO
from toolbelt.github.workflow import get_artifact_urls
from toolbelt.utils.zip import extract as extract_player, compress as compress_player

from .copy_machine import CopyMachine
from .version import create_version_json

logger = structlog.get_logger(__name__)


class PlayerCopyMachine(CopyMachine):
    def __init__(self) -> None:
        super().__init__("player")

    def download(self, platform: str, commit_hash: str, run_id: str):
        logger.debug("Download artifact", app="player", input=commit_hash)

        github_client = GithubClient(
            config.github_token, org=GITHUB_ORG, repo=PLAYER_REPO
        )

        urls = get_artifact_urls(
            github_client,
            run_id,
        )
        logger.debug("Get artifact urls", app="player", urls=urls)

        logger.info(
            "Start download artifact",
            app="player",
            os=platform,
            url=urls[platform],
        )

        downloaded_path = download_artifact(
            github_client, urls[platform], self.base_dir
        )
        self.dir_map["downloaded"] = downloaded_path

        logger.info(
            "Finish download",
            app="player",
            os=platform,
            path=downloaded_path,
        )

    def preprocessing(
        self,
        platform: str,
        commit_hash: str,
        version: int,
    ):
        logger.debug("Preprocessing", app="player", dir_status=self.dir_map)

        logger.debug("Start extract artifact", app="player", os=platform)

        extract_path = extract_player(self.base_dir, self.dir_map["downloaded"], False)

        logger.info(
            "Finish extract artifact",
            app="player",
            os=platform,
            extract_path=extract_path,
        )

        binary_path = os.path.join(self.base_dir, BINARY_FILENAME_MAP[platform])

        logger.debug("Inject version metadata", app="player", os=platform)
        create_version_json(platform, commit_hash, version, f"{extract_path}/version.json")

        compressed = compress_player(self.base_dir, extract_path, binary_path, False)
        logger.debug(
            "Finish compressing artifact with version metadata",
            app="player",
            os=platform,
            compress_path=compressed
        )

        self.dir_map["binary"] = binary_path
        self.dir_map.pop("downloaded")

    def upload(
        self,
        platform: str,
        target_s3_dir: str,
    ):
        logger.debug(
            "Upload",
            app="player",
        )

        release_bucket = S3File(RELEASE_BUCKET)
        release_path = f"{target_s3_dir}/{BINARY_FILENAME_MAP[platform]}"

        logger.debug(
            "Release Path",
            app="player",
            os=platform,
            path=release_path,
        )

        release_bucket.upload(
            self.dir_map["binary"],
            release_path,
        )
        logger.info(
            "Upload Done",
            app="player",
            os=platform,
            release_path=release_path,
        )
        os.remove(self.dir_map["binary"])


def download_from_github(github_client: GithubClient, url: str, filename: str, dir: str):
    """
    Download a file from a URL and save it to a file

    :param github_client: GithubClient
    :type github_client: GithubClient
    :param url: The URL of the zip file to download
    :type url: str
    :param filename: The name of the file you want to download
    :type filename: str
    :param dir: The directory to download the zip file to
    :type dir: str
    :return: A path to the downloaded file.
    """

    path = f"{os.path.join(dir, url)}"
    res = github_client._session.get(url)
    res.raise_for_status()

    with open(path, "wb") as f:
        for chunk in res.iter_content(chunk_size=1024):
            f.write(chunk)

    return path


def download_artifact(github_client: GithubClient, url: str, dir: str):
    """
    Download a file from a URL and save it to a file

    :param github_client: GithubClient
    :type github_client: GithubClient
    :param url: The URL of the zip file to download
    :type url: str
    :param dir: The directory to download the zip file to
    :type dir: str
    :return: A path to the downloaded file.
    """

    path = f"{os.path.join(dir, url.split('/')[-1])}"
    res = github_client.get_runtime_api(url)
    res.raise_for_status()

    with open(path, "wb") as f:
        for chunk in res.iter_content(chunk_size=1024):
            f.write(chunk)

    return path
