import base64
import time
from typing import Any, Iterator, Literal, Optional, Tuple

import requests

from toolbelt.client.session import BaseUrlSession
from toolbelt.config import config

GITHUB_BASE_URL = "https://api.github.com"
WORKFLOW_STATUS = Literal[
    "completed",
    "action_required",
    "cancelled",
    "failure",
    "neutral",
    "skipped",
    "stale",
    "success",
    "timed_out",
    "in_progress",
    "queued",
    "requested",
    "waiting",
    "pending",
]


class GithubClient:
    def __init__(self, token: str, *, org: str, repo: str) -> None:
        """
        GithubClient implementation with github rest api.
        [https://docs.github.com/en/rest]

        :param token: The token of the bot
        :type token: str
        """

        self._token = token
        self._session = BaseUrlSession(GITHUB_BASE_URL)

        self._session.headers.update({"Authorization": f"token {token}"})

        self._runtime_session = BaseUrlSession(config.runtime_url)
        self._runtime_session.headers.update({"Authorization": f"Bearer {config.runtime_token}"})

        self.org = org
        self.repo = repo

    def handle_response(self, r: requests.Response):
        """
        It takes a response object from the requests library,
        checks for errors, and returns the JSON response

        :param r: requests.Response
        :type r: requests.Response
        :return: json response
        """

        r.raise_for_status()
        res = r.json()

        return res

    def get_tags(self, *, offset: int = 1, per_page: int = 10) -> Iterator[Any]:
        """
        It returns a generator that yields a list of tags for a given repo.

        :param offset: The page number to start on, defaults to 1
        :type offset: int (optional)
        :param per_page: The number of items to return per page, defaults to 10
        :type per_page: int (optional)
        """

        # Max page hard coding(100)
        for page in range(offset, 100):
            params = {
                "per_page": per_page,
                "page": page,
            }
            r = self._session.get(f"/repos/{self.org}/{self.repo}/tags", params=params)
            response = self.handle_response(r)
            if len(response) == 0:
                break

            yield response

            # Temp delay
            time.sleep(1)

    def get_content(self, path: str, branch: str) -> Tuple[Optional[str], Any]:
        params = {"ref": branch}

        r = self._session.get(
            f"/repos/{self.org}/{self.repo}/contents/{path}", params=params
        )
        response = self.handle_response(r)

        content = (
            base64.b64decode(response["content"]).decode("utf-8")
            if "content" in response
            else None
        )

        return content, response

    def get_workflow_runs(
        self,
        status: WORKFLOW_STATUS,
        *,
        offset: int = 1,
        per_page: int = 10,
        branch: Optional[str] = None,
        created: Optional[str] = None,
        head_sha: Optional[str] = None,
        event: Optional[str] = None,
    ) -> Iterator[Any]:
        for page in range(offset, 100):
            params = {
                "status": status,
                "branch": branch,
                "created": created,
                "head_sha": head_sha,
                "event": event,
                "per_page": per_page,
                "page": page,
            }
            r = self._session.get(
                f"/repos/{self.org}/{self.repo}/actions/runs", params=params
            )
            response = self.handle_response(r)
            if len(response) == 0:
                break

            yield response

            # Temp delay
            time.sleep(1)

    def generate_artifacts_url(self, run_id: str):
        return self._runtime_session.get(f"_apis/pipelines/workflows/{run_id}/artifacts")

    def get_runtime_api(self, url: str):
        return requests.get(url, headers={"Authorization": f"Bearer {config.runtime_token}"})

    def update_content(
        self,
        *,
        commit: str,
        path: str,
        message: str,
        content: str,
        branch: str,
    ):
        data = {
            "message": message,
            "content": base64.b64encode(content.encode("utf-8")).decode("utf-8"),
            "sha": commit,
            "branch": branch,
        }
        r = self._session.put(
            f"/repos/{self.org}/{self.repo}/contents/{path}", json=data
        )
        response = self.handle_response(r)

        return response

    def get_ref(self, ref: str) -> Any:
        r = self._session.get(f"/repos/{self.org}/{self.repo}/git/ref/{ref}")
        response = self.handle_response(r)

        return response

    def create_ref(self, ref: str, commit: str, *, key: Optional[str] = None) -> Any:
        data = {
            "ref": ref,
            "sha": commit,
            "key": key,
        }
        r = self._session.post(f"/repos/{self.org}/{self.repo}/git/refs", json=data)
        response = self.handle_response(r)

        return response

    def create_pull(
        self,
        *,
        head: str,
        base: str,
        draft: bool = False,
        title: Optional[str] = None,
        body: Optional[str] = None,
    ) -> Any:
        data = {
            "title": title,
            "body": body,
            "head": head,
            "base": base,
            "draft": draft,
        }
        r = self._session.post(f"/repos/{self.org}/{self.repo}/pulls", json=data)
        response = self.handle_response(r)

        return response
