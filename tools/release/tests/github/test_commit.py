from tests.path import GET_REF_RESPONSE_PATH, TAGS_RESPONSE_PATH
from tests.testdata import read_file_as_json
from toolbelt.client import GithubClient
from toolbelt.github.commit import (
    get_latest_commit_hash_from_branch,
    get_latest_commit_hash_from_tag,
)


def test_get_latest_commit_hash_from_branch(mocker):
    client = GithubClient("test", org="test", repo="test")
    response = read_file_as_json(GET_REF_RESPONSE_PATH)
    mocker.patch.object(
        client,
        "get_ref",
        return_value=response,
    )

    r = get_latest_commit_hash_from_branch(client, "test")
    assert r == response["object"]["sha"]


def test_get_latest_commit_hash_from_tag(mocker):
    client = GithubClient("test", org="test", repo="test")
    response = read_file_as_json(TAGS_RESPONSE_PATH)
    mocker.patch.object(
        client,
        "get_tags",
        return_value=iter([response]),
    )

    r = get_latest_commit_hash_from_tag(client, "v100302")
    assert r == response[0]["commit"]["sha"]
