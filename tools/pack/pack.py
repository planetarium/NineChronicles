#!/usr/bin/env python3
"""게임과 론처를 묶어서 새 앱 프로토콜 버전을 서명한 뒤 패키지로 생성한다."""
import argparse
import os
import os.path
import logging
import shutil
import tarfile
import tempfile
import zipfile
from zipfile import ZIP_DEFLATED


parser = argparse.ArgumentParser(description=__doc__.replace('\n', ' '))
parser.add_argument('out_dir')
parser.add_argument('platform', choices={'macOS', 'Windows', 'Linux'})
parser.add_argument('game_dir')
parser.add_argument('timestamp')
parser.add_argument(
    '--verbose', '-v',
    action='store_const', const=logging.DEBUG, default=logging.INFO,
)


def main() -> None:
    args = parser.parse_args()
    logging.basicConfig(level=args.verbose)

    temp_dir = tempfile.mkdtemp()
    for root in [args.game_dir]:
        for name in os.listdir(root):
            path = os.path.join(root, name)
            tmppath = os.path.join(temp_dir, name)
            if os.path.isdir(path):
                if not os.path.isdir(tmppath):  # skip duplicate dirs
                    shutil.copytree(path, tmppath)
            else:
                if not os.path.isfile(tmppath):  # skip duplicate files
                    shutil.copy2(path, tmppath)
            logging.info('Copy: %s -> %s', path, tmppath)

    # 아카이브 생성
    os.makedirs(args.out_dir, exist_ok=True)
    if args.platform.lower() == 'macos':
        archive_path = os.path.join(args.out_dir, 'macOS.tar.gz')
        executable_path = os.path.join(
            temp_dir,
            '9c.app/Contents/MacOS/9c'
        )
        os.chmod(executable_path, 0o755)
        with tarfile.open(archive_path, 'w:gz') as archive:
            for arcname in os.listdir(temp_dir):
                name = os.path.join(temp_dir, arcname)
                archive.add(name, arcname=arcname)
                logging.info('Added: %s <- %s', arcname, name)
    elif args.platform.lower() == 'linux':
        archive_path = os.path.join(args.out_dir, 'Linux.tar.gz')
        executable_path = os.path.join(
            temp_dir,
            '9c'
        )
        os.chmod(executable_path, 0o755)
        with tarfile.open(archive_path, 'w:gz') as archive:
            for arcname in os.listdir(temp_dir):
                name = os.path.join(temp_dir, arcname)
                archive.add(name, arcname=arcname)
                logging.info('Added: %s <- %s', arcname, name)
    elif args.platform.lower() == 'windows':
        archive_path = os.path.join(args.out_dir, 'Windows.zip')
        with zipfile.ZipFile(archive_path, 'w', ZIP_DEFLATED) as archive:
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


if __name__ == '__main__':
    main()
