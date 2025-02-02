import argparse
import os
import os.path
import logging
import tarfile
import shutil
import zipfile
from zipfile import ZIP_DEFLATED


parser = argparse.ArgumentParser()
parser.add_argument('out_dir')
parser.add_argument('platform', choices={'Android', 'StandaloneOSX', 'StandaloneWindows', 'StandaloneLinux64'})
parser.add_argument('input_dir')
parser.add_argument(
    '--verbose', '-v',
    action='store_const', const=logging.DEBUG, default=logging.INFO,
)


def zip_for_macos(build_result_dir: str, out_dir: str):
    archive_path = os.path.join(out_dir, 'macOS.tar.gz')
    executable_path = os.path.join(
        build_result_dir,
        'NineChronicles.app/Contents/MacOS/NineChronicles'
    )
    os.chmod(executable_path, 0o755)
    with tarfile.open(archive_path, 'w:gz') as archive:
        for arcname in os.listdir(build_result_dir):
            name = os.path.join(build_result_dir, arcname)
            archive.add(name, arcname=arcname)
    logging.info('Added: %s <- %s', arcname, name)


def zip_for_linux(build_result_dir: str, out_dir: str):
    archive_path = os.path.join(out_dir, 'Linux.tar.gz')
    executable_path = os.path.join(
        build_result_dir,
        'NineChronicles'
    )
    os.chmod(executable_path, 0o755)
    with tarfile.open(archive_path, 'w:gz') as archive:
        for arcname in os.listdir(build_result_dir):
            name = os.path.join(build_result_dir, arcname)
            archive.add(name, arcname=arcname)
    logging.info('Added: %s <- %s', arcname, name)


def zip_for_windows(build_result_dir: str, out_dir: str):
    archive_path = os.path.join(out_dir, 'Windows.zip')
    with zipfile.ZipFile(archive_path, 'w', ZIP_DEFLATED) as archive:
        basepath = os.path.abspath(build_result_dir) + os.sep
        for path, dirs, files in os.walk(build_result_dir):
            logging.debug('Walk: %r, %r, %r', path, dirs, files)
            for name in files + dirs:
                fullname = os.path.abspath(os.path.join(path, name))
                assert fullname.startswith(basepath)
                relname = fullname[len(basepath):]
                archive.write(fullname, relname)
                logging.info('Added: %s <- %s', relname, fullname)


def zip_for_android(build_result_dir: str, out_dir: str):
    archive_path = os.path.join(out_dir, 'Android.zip')
    with zipfile.ZipFile(archive_path, 'w', ZIP_DEFLATED) as archive:
        basepath = os.path.abspath(build_result_dir) + os.sep
        for path, dirs, files in os.walk(build_result_dir):
            logging.debug('Walk: %r, %r, %r', path, dirs, files)
            for name in files + dirs:
                fullname = os.path.abspath(os.path.join(path, name))
                assert fullname.startswith(basepath)
                relname = fullname[len(basepath):]
                archive.write(fullname, relname)
                logging.info('Added: %s <- %s', relname, fullname)


ZIP = {
    "Android": zip_for_android,
    "StandaloneOSX": zip_for_macos,
    "StandaloneLinux64": zip_for_linux,
    "StandaloneWindows": zip_for_windows,
}


def cleanup_debug_dir(build_result_dir: str, isMobile: bool):
    build_name = "android-build" if isMobile else "NineChronicles"
    debug_dir = os.path.join(build_result_dir, f"{build_name}_BurstDebugInformation_DoNotShip")
    if os.path.exists(debug_dir):
        shutil.rmtree(debug_dir)
        logging.info("Removed debug folder: %s", debug_dir)
    else:
        logging.info("Debug folder does not exist at expected path: %s", debug_dir)
        # 대체 디버그 폴더 확인
        alternative_dirs = []
        for root, dirs, _ in os.walk(build_result_dir):
            for d in dirs:
                if "BurstDebugInformation_DoNotShip" in d:
                    alternative_dirs.append(os.path.join(root, d))
        if alternative_dirs:
            logging.info("Found alternative debug folder(s): %s", alternative_dirs)
        else:
            logging.info("No alternative debug folder found in: %s", build_result_dir)
    
    # build_result_dir 내부가 비었는지 확인
    if len(os.listdir(build_result_dir)) == 0:
        logging.info("Build result folder is empty at: %s", build_result_dir)
        # 상위 디렉토리에서 대체 빌드 결과 디렉토리 탐색
        alternative_builds = []
        parent_dir = os.path.dirname(build_result_dir)
        for item in os.listdir(parent_dir):
            full_path = os.path.join(parent_dir, item)
            if os.path.isdir(full_path) and item != os.path.basename(build_result_dir):
                alternative_builds.append(full_path)
        if alternative_builds:
            logging.info("Found alternative build result directories: %s", alternative_builds)
            build_result_dir = alternative_builds[0]
            logging.info("Using alternative build result directory: %s", build_result_dir)
        else:
            raise Exception("Build result is empty")



def main() -> None:
    args = parser.parse_args()
    logging.basicConfig(level=args.verbose)

    os.makedirs(args.out_dir, exist_ok=True)

    build_result_dir = os.path.join(
        args.input_dir,
        args.platform
    )

    cleanup_debug_dir(build_result_dir, args.platform == "Android")

    try:
        ZIP[args.platform](build_result_dir, args.out_dir)
    except KeyError:
        raise Exception(f'unsupported platform: {args.platform}')
    logging.info("Finish")


if __name__ == '__main__':
    main()
