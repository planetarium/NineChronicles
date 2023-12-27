import os

ROOT_DIR = os.path.abspath(os.path.dirname(__file__))
DATA_DIR = os.path.abspath(os.path.join(ROOT_DIR, "data"))
OUTPUT_DIR = os.path.abspath(os.path.join(ROOT_DIR, "output"))

# k8s config dir hard coding
INTERNAL_CONFIG_PATH = "internal/config.json"
ONBOARDING_CONFIG_PATH = "9c-launcher-config.json"
MAIN_CONFIG_PATH = "9c-launcher-config.json"

RELEASE_BASE_URL = "https://release.nine-chronicles.com"
RELEASE_BUCKET = "9c-release.planetariumhq.com"

WIN = "Windows"
MAC = "macOS"
LINUX = "Linux"

BINARY_FILENAME_MAP = {
    WIN: "Windows.zip",
    MAC: "macOS.tar.gz",
    LINUX: "Linux.tar.gz",
}
