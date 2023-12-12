from datetime import datetime


from toolbelt.client.github import GithubClient
from toolbelt.constants import LINUX, MAC, WIN, BINARY_FILENAME_MAP


def get_artifact_urls(github_client: GithubClient, run_id) -> dict:
    artifacts_response = github_client.generate_artifacts_url(run_id)
    artifacts = github_client.handle_response(artifacts_response)

    result = {
        WIN: "",
        MAC: "",
        LINUX: "",
    }

    for artifact in artifacts["value"]:
        expires_on = datetime.fromisoformat(artifact["expiresOn"].rstrip("Z")[:23])
        assert expires_on > datetime.now()

        if "Window" in artifact["name"] or "win" in artifact["name"]:
            result[WIN] = f"{artifact['fileContainerResourceUrl']}?itemPath={artifact['name']}/{BINARY_FILENAME_MAP[WIN]}"
        if "OSX" in artifact["name"] or "mac" in artifact["name"]:
            result[MAC] = f"{artifact['fileContainerResourceUrl']}?itemPath={artifact['name']}/{BINARY_FILENAME_MAP[MAC]}"
        if "Linux" in artifact["name"] or "linux" in artifact["name"]:
            result[LINUX] = f"{artifact['fileContainerResourceUrl']}?itemPath={artifact['name']}/{BINARY_FILENAME_MAP[LINUX]}"

    return result
