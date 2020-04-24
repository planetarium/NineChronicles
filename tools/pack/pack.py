#!/usr/bin/env python3
"""게임과 론처를 묶어서 새 앱 프로토콜 버전을 서명한 뒤 패키지로 생성한다."""
import argparse
import os
import os.path
import logging
import platform
import re
import subprocess
import shutil
import tarfile
import tempfile
from typing import Optional, Sequence
import zipfile

from boto3 import client
from commentjson import dump, load


DOWNLOAD_URL_BASE = 'https://download.nine-chronicles.com'
MACOS_DOWNLOAD_URL_FORMAT = DOWNLOAD_URL_BASE + "/v{version}/macOS.tar.gz"
WINDOWS_DOWNLOAD_URL_FORMAT = DOWNLOAD_URL_BASE + "/v{version}/Windows.zip"

S3_BUCKET = '9c-test'
S3_OBJECT_PREFIX = 'v'
S3_OBJECT_DELIMITER = '/'
S3_OBJECT_PATTERN = re.compile(r'^v(\d+)$')


parser = argparse.ArgumentParser(description=__doc__.replace('\n', ' '))
parser.add_argument('out_dir')
parser.add_argument('platform', choices={'macOS', 'Windows'})
parser.add_argument('game_dir')
parser.add_argument('launcher_dir')
parser.add_argument('private_key')
parser.add_argument('timestamp')
parser.add_argument(
    '--verbose', '-v',
    action='store_const', const=logging.DEBUG, default=logging.INFO,
)


def main() -> None:
    args = parser.parse_args()
    logging.basicConfig(level=args.verbose)

    temp_dir = tempfile.mkdtemp()
    for root in args.launcher_dir, args.game_dir:
        for name in os.listdir(root):
            path = os.path.join(root, name)
            tmppath = os.path.join(temp_dir, name)
            if os.path.isdir(path):
                shutil.copytree(path, tmppath)
            else:
                shutil.copy2(path, tmppath)
            logging.info('Copy: %s -> %s', path, tmppath)

    # 아직 실제로 올라가 있지 않더라도, 이쪽으로 올려야 함. 서명을 하기 위해 미리 URL을 결정해 둠.
    next_version = latest_version() + 1
    macos_url = MACOS_DOWNLOAD_URL_FORMAT.format(version=next_version)
    windows_url = WINDOWS_DOWNLOAD_URL_FORMAT.format(version=next_version)

    # 임시로 키 가져오기
    passphrase = os.urandom(40).hex()
    key_id = planet([
        'key', 'import',
        '--passphrase', passphrase,
        args.private_key,
    ]).split()[0].decode()
    public_key = planet([
        'key', 'export',
        '--passphrase', passphrase,
        '--public-key',
        key_id,
    ]).strip().decode()

    # 앱 버전 프로토콜 서명
    apv = planet([
        'apv', 'sign',
        '--passphrase', passphrase,
        '--extra', f'timestamp={args.timestamp}',
        '--extra', f'macOSBinaryUrl={macos_url}',
        '--extra', f'WindowsBinaryUrl={windows_url}',
        key_id,
        str(next_version),
    ]).strip().decode()

    # 임시로 가져왔던 키 파기
    planet(['key', 'remove', '--passphrase', passphrase, key_id])

    launcher_json = os.path.join(temp_dir, 'launcher.json')
    clo_json = None
    for p, _, _ in os.walk(temp_dir):
        if os.path.basename(p) == 'StreamingAssets':
            clo_json = os.path.join(p, 'clo.json')
            break
    else:
        return parser.exit(1, 'failed to find StreamingAssets directory')

    # launcher.json 서명 업데이트
    try:
        with open(launcher_json) as f:
            launcher_conf = load(f)
            logging.debug('Deserialzed launcher.json: %r', launcher_conf)
    except FileNotFoundError:
        launcher_conf = {}
        logging.warning('No launcher.json; create an empty one')
    launcher_conf.update(
        appProtocolVersionToken=apv,
        trustedAppProtocolVersionSigners=[public_key],
    )
    with open(launcher_json, 'w') as f:
        logging.debug('Serialize launcher.json: %r', launcher_conf)
        dump(launcher_conf, f, ensure_ascii=False, indent='  ')

    # clo.json 서명 업데이트
    try:
        with open(clo_json) as f:
            clo = load(f)
            logging.debug('Deserialzed clo.json: %r', clo)
    except FileNotFoundError:
        clo = {}
        warning.debug('No clo.json; create an empty one')
    clo.update(
        appProtocolVersionToken=apv,
        trustedAppProtocolVersionSigners=[public_key],
    )
    with open(clo_json, 'w') as f:
        logging.debug('Serialize clo.json: %r', clo)
        dump(clo, f, ensure_ascii=False, indent='  ')

    # 아카이브 생성 
    if args.platform.lower() == 'macos':
        archive_path = os.path.join(args.out_dir, 'macOS.tar.gz')
        with tarfile.open(archive_path, 'w') as archive:
            for arcname in os.listdir(temp_dir):
                name = os.path.join(temp_dir, arcname)
                archive.add(name, arcname=arcname)
                logging.info('Added: %s <- %s', arcname, name)
    elif args.platform.lower() == 'windows':
        archive_path = os.path.join(args.out_dir, 'Windows.zip')
        with zipfile.ZipFile(archive_path, 'w') as archive:
            basepath = os.path.abspath(temp_dir) + os.sep
            for path, dirs, files in os.walk(temp_dir):
                logging.debug('Walk: %r, %r, %r', path, dirs, files)
                for name in files + dirs:
                    fullname = os.path.abspath(os.path.join(path, name))
                    assert fullname.startswith(basepath)
                    relname = fullname[len(basepath):]
                    archive.write(fullname, relname)
                    logging.info('Added: %s <- %s', relname, fullname)
    else:
        return parser.exit(1, f'unsupported platform: {args.platform}')
    logging.info('Created an archive: %s', archive_path)

    shutil.rmtree(temp_dir)


def latest_version() -> int:
    """S3에 공개된 마지막 버전을 구한다."""
    v = 0
    s3 = client('s3')
    cont = {}
    while True:
        resp = s3.list_objects_v2(
            Bucket=S3_BUCKET,
            Delimiter=S3_OBJECT_DELIMITER,
            Prefix=S3_OBJECT_PREFIX,
            MaxKeys=2,
            **cont,
        )
        prefixes = (
            p[:-len(S3_OBJECT_DELIMITER)] if p.endswith(S3_OBJECT_DELIMITER) else p
            for d in resp['CommonPrefixes']
            for p in (d['Prefix'],)
        )
        matches = map(S3_OBJECT_PATTERN.match, prefixes)
        versions = (int(m.group(1)) for m in matches if m)
        v = max(max(versions), v)
        if not resp['IsTruncated']:
            break
        cont['ContinuationToken'] = resp['NextContinuationToken']
    return v


def which_planet() -> str:
    """Libplanet.Tools 바이너리 위치를 찾는다."""
    sys = platform.system()
    mach = platform.machine()
    tags = {
        ('Darwin', 'x86_64'): 'osx-x64',
        ('Linux',  'x86_64'): 'linux-x64',
        ('Windows', 'AMD64'): 'win-x64',
    }
    try:
        tag = tags[sys, mach]
    except KeyError:
        r = subprocess.run(['which', 'planet'], capture_output=True)
        if r.returncode == 0:
            return r.stdout.rstrip().decode()
        raise NotImplementedError(f'unsupported platform: ({sys!r}, {mach!r})')
    dir_ = f'planet-0.9.0-{tag}'
    bin_ = os.path.join(os.path.dirname(__file__), dir_, 'planet')
    if os.path.isfile(bin_):
        return bin_
    raise NotImplementedError(
        f'failed to find the bundled planet executable: {bin_}'
    )


planet_bin: Optional[str] = None


def planet(args: Sequence[str]) -> bytes:
    """planet 커맨드를 실행한다."""
    global planet_bin
    if planet_bin is None:
        planet_bin = which_planet()
    cmd = [planet_bin, *args]
    r = subprocess.run(cmd, capture_output=True)
    if r.returncode != 0:
        errmsg = r.stderr.decode()
        logging.error(
            f'The command %r terminated with an error: %s\n%s',
            cmd, r.returncode, '  ' + errmsg.replace('\n', '\n  ')
        )
    assert r.returncode == 0, \
        f'The command {cmd!r} terminated with an error: {r.returncode}'
    return r.stdout


if __name__ == '__main__':
    main()
