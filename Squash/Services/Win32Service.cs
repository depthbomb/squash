using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Squash.Services;

public sealed class Win32Service
{
    private readonly ITaskbarList3 _taskbar;

    public Win32Service()
    {
        var taskbarType = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090"))!;

        _taskbar = (ITaskbarList3)Activator.CreateInstance(taskbarType)!;
        _taskbar.HrInit();
    }

    public void SetTaskbarProgress(Form form, int value, int maximum)
    {
        maximum = Math.Max(maximum, 1);
        value   = Math.Clamp(value, 0, maximum);

        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_NORMAL);
        _taskbar.SetProgressValue(GetHandle(form), (ulong)value, (ulong)maximum);
    }

    public void SetTaskbarIndeterminate(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_INDETERMINATE);
    }

    public void SetTaskbarPaused(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_PAUSED);
    }

    public void SetTaskbarError(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_ERROR);
    }

    public void ClearTaskbarProgress(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_NOPROGRESS);
    }

    public void Flash(Form form)
    {
        var info = CreateFlashInfo(form, FLASHWINFO_FLAGS.FLASHW_ALL, 3);

        PInvoke.FlashWindowEx(info);
    }

    public void FlashUntilFocused(Form form)
    {
        var info = CreateFlashInfo( form, FLASHWINFO_FLAGS.FLASHW_ALL | FLASHWINFO_FLAGS.FLASHW_TIMERNOFG, uint.MaxValue);

        PInvoke.FlashWindowEx(info);
    }

    public void StopFlashing(Form form)
    {
        var info = CreateFlashInfo(form, FLASHWINFO_FLAGS.FLASHW_STOP, 0);

        PInvoke.FlashWindowEx(info);
    }
    
    public void PreventSleep()
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS      |
                                        EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                                        EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }

    public void AllowSleep()
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
    }

    private static HWND GetHandle(Form form) => (HWND)form.Handle;

    private static FLASHWINFO CreateFlashInfo(Form form, FLASHWINFO_FLAGS flags, uint count)
        => new()
        {
            cbSize    = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd      = GetHandle(form),
            dwFlags   = flags,
            uCount    = count,
            dwTimeout = 0
        };
}