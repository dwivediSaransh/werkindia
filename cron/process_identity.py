"""Windows PID identity using creation time; unknown is never treated as dead."""
from __future__ import annotations

import ctypes
import os


def identity(pid: int) -> str | None:
    if os.name != 'nt':
        raise RuntimeError('IndiaWorks watchdog process recovery currently requires Windows')
    from ctypes import wintypes
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    kernel.OpenProcess.restype = wintypes.HANDLE
    kernel.GetProcessTimes.argtypes = [wintypes.HANDLE] + [ctypes.POINTER(wintypes.FILETIME)] * 4
    kernel.GetExitCodeProcess.argtypes = [wintypes.HANDLE, ctypes.POINTER(wintypes.DWORD)]
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    handle = kernel.OpenProcess(0x1000, False, pid)
    if not handle:
        error = ctypes.get_last_error()
        if error == 87:  # PID no longer exists
            return None
        raise OSError(error, 'Cannot verify process identity')
    try:
        code = wintypes.DWORD()
        if not kernel.GetExitCodeProcess(handle, ctypes.byref(code)):
            raise ctypes.WinError(ctypes.get_last_error())
        if code.value != 259:  # STILL_ACTIVE
            return None
        times = [wintypes.FILETIME() for _ in range(4)]
        if not kernel.GetProcessTimes(handle, *(ctypes.byref(t) for t in times)):
            raise ctypes.WinError(ctypes.get_last_error())
        return str((times[0].dwHighDateTime << 32) | times[0].dwLowDateTime)
    finally:
        kernel.CloseHandle(handle)


def alive(record: dict) -> bool:
    stamp = identity(int(record['pid']))
    if stamp is not None and not record.get('created'):
        raise ValueError('Missing process creation identity')
    return stamp is not None and stamp == record['created']


def own_identity(pid: int) -> dict:
    return {'pid': pid, 'created': identity(pid)}
