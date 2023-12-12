import pytest

from tests.path import (
    DP_VALUES_FILE_PATH,
    HEADLESS_VALUES_FILE_PATH,
    SEED_VALUES_FILE_PATH,
    VALUES_FILE_PATH,
)
from tests.testdata import read_file
from toolbelt.apps.k8s.update_values import (
    ImageMetadata,
    extract_image_metadata,
    update_image_tag,
)


@pytest.mark.parametrize(
    "input,expect_result",
    [
        (
            "ninechronicles-headless/from tag 1",
            ("ninechronicles-headless", "tag", "1"),
        ),
        (
            "ninechronicles-headless/from branch main",
            ("ninechronicles-headless", "branch", "main"),
        ),
    ],
)
def test_extract_image_metadata(input: str, expect_result: ImageMetadata):
    result = extract_image_metadata(input)
    assert result == expect_result


@pytest.mark.parametrize(
    "expect_result_file_path,repo_to_change,tag_to_change",
    [
        (
            HEADLESS_VALUES_FILE_PATH,
            "ninechronicles-headless",
            "123",
        ),
        (
            DP_VALUES_FILE_PATH,
            "ninechronicles-dataprovider",
            "git-123",
        ),
        (
            SEED_VALUES_FILE_PATH,
            "libplanet-seed",
            "git-456",
        ),
    ],
)
def test_patch_values_file(expect_result_file_path, repo_to_change, tag_to_change):
    input = read_file(VALUES_FILE_PATH)
    expect_result = read_file(expect_result_file_path)

    result = update_image_tag(
        input, repo_to_change=repo_to_change, tag_to_change=tag_to_change
    )

    assert result == expect_result
