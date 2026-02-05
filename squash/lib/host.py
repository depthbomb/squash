from os import environ
from re import match, IGNORECASE

def is_in_windows_terminal() -> bool:
    wt_session = environ.get('WT_SESSION')
    if not wt_session:
        return False

    return match(r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$', wt_session, IGNORECASE) is not None
