from toolbelt.client.session import BaseUrlSession
from toolbelt.constants import RELEASE_BASE_URL
from toolbelt.exceptions import ResponseError


def get_apv(path: str) -> str:
    session = BaseUrlSession(RELEASE_BASE_URL)
    resp = session.request("get", path)
    if not resp.ok:
        raise ResponseError(f"S3API ResponseError: {resp.content}")
    doc = resp.json()
    return doc["AppProtocolVersion"]
