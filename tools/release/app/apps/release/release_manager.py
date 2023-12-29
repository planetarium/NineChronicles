import os

import structlog

from app.tools.esigner import Esigner
from app.client.aws import S3File
from app.config import config
from app.constants import BINARY_FILENAME_MAP, RELEASE_BUCKET
from app.utils.zip import extract, compress
from app.types import Platforms

logger = structlog.get_logger(__name__)


def unpack_player(tmp_path: str, binary_path: str, platform: Platforms):
    logger.debug("Start unpack player")

    extracted_path1 = extract(tmp_path, binary_path)
    extracted_path2 = extract(f"{tmp_path}/result", f"{extracted_path1}/{BINARY_FILENAME_MAP[platform]}")

    logger.debug("End unpack player")

    return extracted_path2


def upload_player(
    binary_path: str,
    platform: str,
    target_s3_dir: str,
):
    logger.debug(
        "Start upload",
        os=platform,
        target_s3_dir=target_s3_dir
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
        binary_path,
        release_path,
    )
    logger.info(
        "Upload Done",
        os=platform,
        release_path=release_path,
    )


def signing_for_windows(
    esigner: Esigner,
    unpacked_path: str,
    result_path: str,
    root_dir: str,
):
    # 2. Move exe files
    input_dir = os.path.join(root_dir, "for_signing_input")
    os.mkdir(input_dir)
    os.rename(
        os.path.join(unpacked_path, "NineChronicles.exe"),
        os.path.join(input_dir, "NineChronicles.exe"),
    )

    # 3. signing
    output_dir = os.path.join(root_dir, "for_signing_input_output")
    os.mkdir(output_dir)
    result = esigner.sign(
        **config.signing_secrets,
        input_dir_path=input_dir,
        output_dir_path=output_dir,
    )
    logger.debug("Signed", output=result.stdout)

    # 4. Re move exe files
    os.rename(
        os.path.join(output_dir, "NineChronicles.exe"),
        os.path.join(unpacked_path, "NineChronicles.exe"),
    )
    logger.debug("Signed path", path=result_path)
