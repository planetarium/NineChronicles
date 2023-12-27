import json
import os

import pytest

from tests.constants import DATA_DIR

SLACK_DIR = os.path.join("client", "slack")

FAILURE_RESPONSE_PATH = os.path.join(SLACK_DIR, "failureResponse.json")
POST_MESSAGE_RESPONSE_PATH = os.path.join(SLACK_DIR, "postMessageResponse.json")


GITHUB_DIR = os.path.join("client", "github")
TAGS_RESPONSE_PATH = os.path.join(GITHUB_DIR, "tags.json")
PATH_CONTENT_RESPONSE_PATH = os.path.join(GITHUB_DIR, "path_content.json")
GET_REF_RESPONSE_PATH = os.path.join(GITHUB_DIR, "get_ref.json")
CREATE_PULL_RESPONSE_PATH = os.path.join(GITHUB_DIR, "create_pull.json")
WORKFLOW_RUNS_RESPONSE_PATH = os.path.join(GITHUB_DIR, "workflow_runs.json")
CREATE_REF_RESPONSE_PATH = os.path.join(GITHUB_DIR, "create_ref.json")
UPDATE_CONTENT_RESPONSE_PATH = os.path.join(GITHUB_DIR, "update_content.json")


DOCKER_DIR = os.path.join("client", "docker")
CHECK_IMAGE_EXISTS_RESPONSE_PATH = os.path.join(DOCKER_DIR, "check_image_exists.json")

K8S_DIR = os.path.join("k8s", "values")
VALUES_FILE_PATH = os.path.join(K8S_DIR, "sample_values.yaml")
HEADLESS_VALUES_FILE_PATH = os.path.join(K8S_DIR, "headless_changed.yaml")
DP_VALUES_FILE_PATH = os.path.join(K8S_DIR, "dp_changed.yaml")
SEED_VALUES_FILE_PATH = os.path.join(K8S_DIR, "seed_changed.yaml")
