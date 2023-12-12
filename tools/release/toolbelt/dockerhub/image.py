import structlog

from toolbelt.client import DockerClient

from .constants import DOCKERHUB_ORG

logger = structlog.get_logger(__name__)


def check_image_exists(repo: str, tag_name: str):
    docker_client = DockerClient(namespace=DOCKERHUB_ORG)

    try:
        result = docker_client.check_image_exists(repo, tag_name)
        return bool(result["id"])
    except KeyError:
        return False
