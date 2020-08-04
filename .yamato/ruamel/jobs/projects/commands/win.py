from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS

def _cmd_base(project_folder, components):
    return [
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat',
        f'pip install unity-downloader-cli --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api["name"] != ""  else ''
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args):
    base = [
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat'
    ]

    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args}Windows64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
    ])
    return base


def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args}Windows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only'
    ])
    return base

def cmd_not_standalone_performance(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --platform=StandaloneWindows64 --report-performance-data --performance-project-id=URP_Performance --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api["name"] != ""  else ''
    return base

def cmd_standalone_performance(project_folder, platform, api, test_platform_args):
    base = [
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat'
    ]

    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --platform=StandaloneWindows64 --report-performance-data --performance-project-id=URP_Performance --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
    ])
    return base

def cmd_standalone_build_performance(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr --suite=playmode --platform=StandaloneWindows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only'
    ])
    return base