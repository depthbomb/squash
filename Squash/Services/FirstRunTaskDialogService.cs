namespace Squash.Services;

public class FirstRunTaskDialogService
{
    public async Task<TaskDialogButton> ShowDialogAsync(IWin32Window owner)
    {
        #region Controls
        var vcb = new TaskDialogVerificationCheckBox
        {
            Text = "I understand"
        };
        #endregion
        
        #region Pages
        var initialPage = new TaskDialogPage
        {
            Caption = "Squash",
            Icon    = TaskDialogIcon.Information,
            Heading = "Disclaimer",
            Text = "Squash isn't magic. It can't encode a 150MB video down to 10MB without a severe loss in "       +
                   "quality; it's just not possible.\nYou should aim for reasonable targets based on the original " +
                   "video file size.\n\nThis message will only be displayed once.",
            SizeToContent = true,
            Buttons =
            {
                TaskDialogButton.Continue,
                TaskDialogButton.Cancel,
            },
            Verification = vcb
        };
        #endregion

        initialPage.Buttons[0].Enabled = false;
        
        vcb.CheckedChanged += (_, _) => initialPage.Buttons[0].Enabled = vcb.Checked;
        
        return await TaskDialog.ShowDialogAsync(owner, initialPage);
    }
}
