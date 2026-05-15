using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;

namespace Squash.Services;

public sealed class TaskbarProgressService
{
    private readonly ITaskbarList3 _taskbar;
    
    public TaskbarProgressService()
    {
        var taskbarType = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090"));
        
        _taskbar = (ITaskbarList3)Activator.CreateInstance(taskbarType!)!;
        _taskbar.HrInit();
    }
    
    public void SetProgress(Form form, int value, int maximum)
    {
        value   = Math.Clamp(value, 0, maximum);
        maximum = Math.Max(maximum, 1);

        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_NORMAL);
        _taskbar.SetProgressValue( GetHandle(form), (ulong)value, (ulong)maximum);
    }

    public void SetIndeterminate(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_INDETERMINATE);
    }

    public void SetPaused(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_PAUSED);
    }

    public void SetError(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_ERROR);
    }

    public void Clear(Form form)
    {
        _taskbar.SetProgressState(GetHandle(form), TBPFLAG.TBPF_NOPROGRESS);
    }

    private static HWND GetHandle(Form form) => (HWND)form.Handle;
}
