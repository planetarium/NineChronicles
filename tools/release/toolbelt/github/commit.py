import structlog

from toolbelt.client.github import GithubClient

from .exceptions import TagNotFoundError

logger = structlog.get_logger(__name__)


def get_latest_commit_hash(
    github_client: GithubClient, ref_name: str, ref_value: str
) -> str:
    func_map = {
        "tag": get_latest_commit_hash_from_tag,
        "branch": get_latest_commit_hash_from_branch,
    }

    try:
        return func_map[ref_name](github_client, ref_value)
    except KeyError:
        raise KeyError(f"ref_name must be either a tag or a branch, not {ref_name}")


def get_latest_commit_hash_from_branch(github_client: GithubClient, branch: str) -> str:
    ref = f"heads/{branch}"
    r = github_client.get_ref(ref)
    return r["object"]["sha"]


def get_latest_commit_hash_from_tag(github_client: GithubClient, tag: str) -> str:
    for tags_info in github_client.get_tags(per_page=100):
        for tag_info in tags_info:
            if tag_info["name"] == tag:
                return tag_info["commit"]["sha"]
    raise TagNotFoundError(f"Tag '{tag}' not found.")
