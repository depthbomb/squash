from os import getcwd
from pathlib import Path
from sys import executable

_g = globals()

#region Flags
IS_COMPILED = '__compiled__' in _g
if IS_COMPILED:
    IS_STANDALONE = bool(_g['__compiled__'].standalone)
    IS_ONEFILE = bool(_g['__compiled__'].onefile)
else:
    IS_STANDALONE = False
    IS_ONEFILE = False
#endregion

#region Application info
APP_ID = '{{4440776D-1267-4071-865A-95F9405FF08E}'
APP_NAME = 'squash'
APP_DISPLAY_NAME = 'Squash'
APP_DESCRIPTION = 'A CLI tool to compress a video to a target size while maintaining as much quality as possible'
APP_ORG = 'Caprine Logic'
APP_USER_MODEL_ID = u'CaprineLogic.Squash'
APP_CLSID = '844389F3-A384-4DF0-9E32-A4D6DF80F438'
APP_VERSION = (1, 3, 0, 0)
APP_VERSION_STRING = '.'.join(str(v) for v in APP_VERSION)
APP_REPO_OWNER = 'depthbomb'
APP_REPO_NAME = 'Squash'
APP_REPO_URL = f'https://github.com/{APP_REPO_OWNER}/{APP_REPO_NAME}'
APP_RELEASES_URL = f'https://github.com/{APP_REPO_OWNER}/{APP_REPO_NAME}/releases'
APP_LATEST_RELEASE_URL = f'https://github.com/{APP_REPO_OWNER}/{APP_REPO_NAME}/releases/latest'
APP_NEW_ISSUE_URL = f'https://github.com/{APP_REPO_OWNER}/{APP_REPO_NAME}/issues/new/choose'
#endregion

#region Paths
BINARY_PATH = Path(executable).with_name(APP_NAME).with_suffix('.exe').resolve()

if IS_COMPILED:
    SEVENZIP_PATH = BINARY_PATH.parent / 'vendor' / '7za.exe'
else:
    SEVENZIP_PATH = Path(getcwd()) / 'resources' / 'vendor' / '7za.exe'
#endregion

__version__ = APP_VERSION_STRING
