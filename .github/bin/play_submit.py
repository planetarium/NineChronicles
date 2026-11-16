#!/usr/bin/env python3
"""Send an already-uploaded Play build to production review.

Promotes `--version-code` (already on the internal track) to the production
track with the given release notes, then commits the edit. With --dry-run the
edit is validated and discarded.

This does NOT stop Play from publishing on approval: turn on managed
publishing in Play Console for the app, otherwise an approved release goes
live immediately.
"""
import argparse
import base64
import json
import sys

import requests
from google.auth.transport.requests import Request
from google.oauth2 import service_account

API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications"
# Release notes keys (shared with the iOS lane) -> Play language codes.
LANGS = {"en": "en-US", "ja": "ja-JP", "vi": "vi", "ko": "ko-KR"}
PLAY_NOTE_LIMIT = 500


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--package", required=True)
    p.add_argument("--version-code", required=True, type=int)
    p.add_argument("--key-file", required=True)
    p.add_argument("--notes-b64", required=True)
    p.add_argument("--dry-run", action="store_true")
    a = p.parse_args()

    notes = json.loads(base64.b64decode(a.notes_b64).decode("utf-8"))
    unknown = set(notes) - set(LANGS)
    if unknown or not notes:
        sys.exit(f"release notes keys must be a non-empty subset of {sorted(LANGS)}: {sorted(notes)}")
    for k, v in notes.items():
        if not v.strip() or len(v) > PLAY_NOTE_LIMIT:
            sys.exit(f"release notes [{k}] must be 1..{PLAY_NOTE_LIMIT} chars (got {len(v)})")

    creds = service_account.Credentials.from_service_account_file(
        a.key_file, scopes=["https://www.googleapis.com/auth/androidpublisher"])
    creds.refresh(Request())
    s = requests.Session()
    s.headers["Authorization"] = f"Bearer {creds.token}"

    def call(method, path, **kw):
        r = s.request(method, f"{API}/{a.package}{path}", timeout=60, **kw)
        if r.status_code >= 300:
            sys.exit(f"{method} {path} -> {r.status_code}: {r.text[:500]}")
        return r.json() if r.text else {}

    edit = call("POST", "/edits")["id"]
    committed = False
    try:
        internal = call("GET", f"/edits/{edit}/tracks/internal")
        codes = {c for rel in internal.get("releases", []) for c in rel.get("versionCodes", [])}
        if str(a.version_code) not in codes:
            sys.exit(f"version code {a.version_code} is not on the internal track (has {sorted(codes)})")

        release = {
            "versionCodes": [str(a.version_code)],
            "status": "completed",
            "releaseNotes": [{"language": LANGS[k], "text": v} for k, v in notes.items()],
        }
        call("PUT", f"/edits/{edit}/tracks/production", json={"track": "production", "releases": [release]})

        if a.dry_run:
            call("POST", f"/edits/{edit}:validate")
            print(f"dry-run ok: {a.package} {a.version_code} would go to production review")
            return
        # A 400 mentioning changesNotSentForReview means Play wants this sent from the console.
        call("POST", f"/edits/{edit}:commit")
        committed = True
        print(f"submitted: {a.package} {a.version_code} sent to production review")
    finally:
        if not committed:
            s.delete(f"{API}/{a.package}/edits/{edit}", timeout=60)


if __name__ == "__main__":
    main()
