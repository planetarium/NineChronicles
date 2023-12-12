import structlog

from toolbelt.client.aws import S3File, create_invalidation
from toolbelt.constants import RELEASE_BUCKET
from toolbelt.utils.url import build_s3_url

download_distribution_id = "E1HPTSGY2RETN4"
release_distribution_id = "E3SBBH63NSNYX"
logger = structlog.get_logger(__name__)


def update_latest(rc: int, commit: str):
    release_bucket = S3File(RELEASE_BUCKET)

    latest_path = "latest/Windows.zip"

    release_bucket.copy(
        build_s3_url("main", rc, "launcher", commit, "Windows.zip"),
        latest_path,
    )

    invalidation_id = create_invalidation([latest_path], download_distribution_id)
    logger.info("DOWNLOAD - latest invalidation Finish", id=invalidation_id)
    invalidation_id = create_invalidation([latest_path], release_distribution_id)
    logger.info("RELEASE - latest invalidation Finish", id=invalidation_id)
    return invalidation_id


def update_root_config(apv: str, docker_image: str):
    release_bucket = S3File(RELEASE_BUCKET)
    config_path = "9c-launcher-config.json"
    apv_json_path = "apv.json"

    apv_json_data = {"apv": apv, "docker": docker_image}
    release_bucket.update(apv_json_path, apv_json_data)

    config_json_data = {"AppProtocolVersion": apv}
    release_bucket.update(config_path, config_json_data)

    invalidation_id = create_invalidation(
        [config_path, apv_json_path], download_distribution_id
    )
    logger.info("RELEASE - config invalidation Finish", id=invalidation_id)
