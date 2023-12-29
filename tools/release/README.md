## Prerequisite

**.env**
- GITHUB_TOKEN: 1password k8s-github-token or your github token(required org:read permission)
- SLACK_TOKEN: 1password Slack Token
- KEY_PASSPHRASE: used for apv signing
- KEY_ADDRESS: used for apv signing

**boto3**
- aws_access_key_id, aws_secret_access_key: $aws configure (~/.aws/credentials)

**Installation**
- required [planet](https://www.npmjs.com/package/@planetarium/cli)
- python 3.9.10

**Python**
```python
# cd ./py-scripts
$ python -m venv .venv
$ . .venv/bin/activate
$ pip install -r requirements-dev.txt
$ flit install --extras all
```

## Usage

**Run cli**

```bash
python cli.py --help
```
