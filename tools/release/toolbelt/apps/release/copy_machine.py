import os
import tempfile
from typing import Dict, Literal

import structlog

from toolbelt.config import config
from toolbelt.constants import WIN
from toolbelt.tools.esigner import Esigner
from toolbelt.types import Platforms
from toolbelt.utils.zip import compress, extract

logger = structlog.get_logger(__name__)


class CopyMachine:
    def __init__(self, app: Literal["player", "launcher"]) -> None:
        self.app = app
        self.dir_map: Dict[str, str] = {}

    def run(
        self,
        platform: Platforms,
        commit_hash: str,
        target_s3_dir: str,
        version: int,
        run_id: str,
        *,
        dry_run: bool = False,
        signing: bool = False,
    ):
        with tempfile.TemporaryDirectory() as tmp_path:
            self.base_dir = tmp_path

            self.download(platform, commit_hash, run_id)
            self.preprocessing(platform, commit_hash, version)
            if signing:
                if platform == WIN:
                    signing_for_windows(
                        Esigner(),
                        self.dir_map["binary"],
                        self.base_dir,
                        self.app,
                    )
                    logger.info("Finish signing", os=platform, app=self.app)
            if not dry_run:
                self.upload(platform, target_s3_dir)

    def download(self, platform: str, commit_hash: str, run_id: str):
        raise NotImplementedError

    def preprocessing(
        self,
        platform: str,
        commit_hash: str,
        version: int,
    ):
        raise NotImplementedError

    def upload(self, platform: str, target_s3_dir: str):
        raise NotImplementedError


def signing_for_windows(
    esigner: Esigner,
    binary_path: str,
    dir: str,
    target_app: Literal["player", "launcher"],
):
    # 1. Extract binary
    extract_path = extract(os.path.join(dir, "for_signing"), binary_path, use7z=False)

    # 2. Move exe files
    input_dir = os.path.join(dir, "temp_input")
    os.mkdir(input_dir)
    if target_app == "player":
        os.rename(
            os.path.join(extract_path, "NineChronicles.exe"),
            os.path.join(input_dir, "NineChronicles.exe"),
        )
    elif target_app == "launcher":
        os.rename(
            os.path.join(extract_path, "Nine Chronicles.exe"),
            os.path.join(input_dir, "Nine Chronicles.exe"),
        )
    else:
        raise ValueError()

    # 3. signing
    output_dir = os.path.join(dir, "temp_output")
    os.mkdir(output_dir)
    result = esigner.sign(
        **config.signing_secrets,
        input_dir_path=input_dir,
        output_dir_path=output_dir,
    )
    logger.debug("Signed", output=result.stdout)

    # 4. Re move exe files
    if target_app == "player":
        os.rename(
            os.path.join(output_dir, "NineChronicles.exe"),
            os.path.join(extract_path, "NineChronicles.exe"),
        )
    elif target_app == "launcher":
        os.rename(
            os.path.join(output_dir, "Nine Chronicles.exe"),
            os.path.join(extract_path, "Nine Chronicles.exe"),
        )

    # 5. Compress
    result_path = compress(dir, extract_path, binary_path, use7z=False)
    logger.debug("Signed path", path=result_path)
