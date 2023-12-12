from typing import Any

from toolbelt.client.session import BaseUrlSession

DOCKER_HUB_URL = "https://hub.docker.com"


class DockerClient:
    def __init__(self, namespace: str) -> None:
        self._namespace = namespace
        self._session = BaseUrlSession(DOCKER_HUB_URL)

    def check_image_exists(self, repo: str, tag: str) -> Any:
        resp = self._session.get(
            f"/v2/namespaces/{self._namespace}/repositories/{repo}/tags/{tag}"
        )

        resp.raise_for_status()
        return resp.json()
