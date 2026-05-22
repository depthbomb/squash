using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Squash.Interop;

public static class Native
{
    private static readonly ITaskbarList3 Taskbar;

    static Native()
    {
        var taskbarType = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090"))!;

        Taskbar = (ITaskbarList3)Activator.CreateInstance(taskbarType)!;
        Taskbar.HrInit();
    }

    public static void SetTaskbarProgress(Form form, int value, int maximum)
    {
        maximum = Math.Max(maximum, 1);
        value   = Math.Clamp(value, 0, maximum);

        Taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_NORMAL);
        Taskbar.SetProgressValue(GetHandle(form), (ulong)value, (ulong)maximum);
    }

    public static void SetTaskbarIndeterminate(Form form)
    {
        Taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_INDETERMINATE);
    }

    public static void SetTaskbarPaused(Form form)
    {
        Taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_PAUSED);
    }

    public static void SetTaskbarError(Form form)
    {
        Taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_ERROR);
    }

    public static void ClearTaskbarProgress(Form form)
    {
        Taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_NOPROGRESS);
    }

    public static void Flash(Form form)
    {
        var info = CreateFlashInfo(form, FLASHWINFO_FLAGS.FLASHW_ALL, 3);

        PInvoke.FlashWindowEx(info);
    }

    public static void FlashUntilFocused(Form form)
    {
        var info = CreateFlashInfo(
            form,
            FLASHWINFO_FLAGS.FLASHW_ALL | FLASHWINFO_FLAGS.FLASHW_TIMERNOFG,
            uint.MaxValue
        );

        PInvoke.FlashWindowEx(info);
    }

    public static void StopFlashing(Form form)
    {
        var info = CreateFlashInfo(form, FLASHWINFO_FLAGS.FLASHW_STOP, 0);

        PInvoke.FlashWindowEx(info);
    }

    public static void PreventSleep()
    {
        PInvoke.SetThreadExecutionState(
            EXECUTION_STATE.ES_CONTINUOUS      |
            EXECUTION_STATE.ES_SYSTEM_REQUIRED |
            EXECUTION_STATE.ES_DISPLAY_REQUIRED
        );
    }

    public static void AllowSleep() => PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

    public static void SetCurrentProcessExplicitAppUserModelId(string appId) => PInvoke.SetCurrentProcessExplicitAppUserModelID(appId);

    public static HWND FindWindow(string windowName) => PInvoke.FindWindow(null, windowName);

    private static HWND GetHandle(Form form) => (HWND)form.Handle;

    private static FLASHWINFO CreateFlashInfo(Form form, FLASHWINFO_FLAGS flags, uint count) => new()
    {
        cbSize    = (uint)Marshal.SizeOf<FLASHWINFO>(),
        hwnd      = GetHandle(form),
        dwFlags   = flags,
        uCount    = count,
        dwTimeout = 0
    };
}
