import pytest

from tests.path import *
from tests.testdata import read_file_as_json
from toolbelt.client import SlackClient
from toolbelt.exceptions import ResponseError


def test_send_msg_success(requests_mock):
    requests_mock.post(
        "/api/chat.postMessage",
        json=read_file_as_json(POST_MESSAGE_RESPONSE_PATH),
    )

    client = SlackClient("test token")

    r = client.send_simple_msg("CTESTTESTX", msg="test2")
    assert r["ok"]


def test_send_msg_failure(requests_mock):
    requests_mock.post(
        "/api/chat.postMessage", json=read_file_as_json(FAILURE_RESPONSE_PATH)
    )

    client = SlackClient("test token")

    with pytest.raises(ResponseError):
        client.send_simple_msg("CTESTTESTX", msg="test2")
