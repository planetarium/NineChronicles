import base64

from tests.path import *
from tests.testdata import read_file_as_json
from toolbelt.client import GithubClient

repo = "9c-k8s-config"
org = "planetarium"


def test_get_tags(requests_mock, mocker):
    client = GithubClient("test token", org=org, repo=repo)
    mocker.patch("time.sleep")

    requests_mock.get(
        f"/repos/{client.org}/{client.repo}/tags?page=1",
        json=read_file_as_json(TAGS_RESPONSE_PATH),
    )
    requests_mock.get(
        f"/repos/{client.org}/{client.repo}/tags?page=2",
        json=[],
    )

    count = 0
    for r in client.get_tags():
        assert r[0]["name"]
        count += 1

    assert count == 1


def test_get_workflow_runs(requests_mock, mocker):
    client = GithubClient("test", org=org, repo=repo)
    mocker.patch("time.sleep")

    requests_mock.get(
        f"/repos/{client.org}/{client.repo}/actions/runs?status=success&page=1",
        json=read_file_as_json(WORKFLOW_RUNS_RESPONSE_PATH),
    )
    requests_mock.get(
        f"/repos/{client.org}/{client.repo}/actions/runs?status=success&page=2",
        json=[],
    )

    count = 0
    for r in client.get_workflow_runs(
        "success", head_sha="b3c6f7af52519c5f71ce1a93c3f939f8ae09496f"
    ):
        assert r["workflow_runs"][0]["id"]
        count += 1

    assert count == 1


def test_get_content(requests_mock):
    client = GithubClient("test token", org=org, repo=repo)
    path = "9c-internal/configmap-versions.yaml"

    requests_mock.get(
        f"/repos/{client.org}/{client.repo}/contents/{path}",
        json=read_file_as_json(PATH_CONTENT_RESPONSE_PATH),
    )

    content, r = client.get_content(path, "main")

    assert content == base64.b64decode(r["content"]).decode("utf-8")


def test_get_ref(requests_mock):
    client = GithubClient("test token", org=org, repo=repo)
    ref = "heads/main"

    requests_mock.get(
        f"/repos/{client.org}/{client.repo}/git/ref/{ref}",
        json=read_file_as_json(GET_REF_RESPONSE_PATH),
    )

    response = client.get_ref(ref)

    assert response["object"]["sha"] == "aa218f56b14c9653891f9e74264a383fa43fefbd"


def test_create_ref(requests_mock):
    client = GithubClient("test token", org=org, repo=repo)

    requests_mock.post(
        f"/repos/{client.org}/{client.repo}/git/refs",
        json=read_file_as_json(CREATE_REF_RESPONSE_PATH),
    )

    response = client.create_ref(
        "refs/heads/main", "aa218f56b14c9653891f9e74264a383fa43fefbd"
    )

    assert response["object"]["sha"] == "aa218f56b14c9653891f9e74264a383fa43fefbd"


def test_create_pull(requests_mock):
    client = GithubClient("test token", org=org, repo=repo)

    requests_mock.post(
        f"/repos/{client.org}/{client.repo}/pulls",
        json=read_file_as_json(CREATE_PULL_RESPONSE_PATH),
    )

    response = client.create_pull(title="test", body="test", head="test", base="test")

    assert response


def test_update_content(requests_mock):
    client = GithubClient("test token", org=org, repo=repo)
    path = "9c-internal/configmap-versions.yaml"

    requests_mock.put(
        f"/repos/{client.org}/{client.repo}/contents/{path}",
        json=read_file_as_json(UPDATE_CONTENT_RESPONSE_PATH),
    )

    r = client.update_content(
        commit="ff443fffce51aac238cb5a5b16ac413a1253cff6",
        path=path,
        message="test",
        content="",
        branch="feat/github",
    )

    assert r["commit"]["sha"] == "7638417db6d59f3c431d3e1f261cc637155684cd"
