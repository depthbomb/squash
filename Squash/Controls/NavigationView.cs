namespace Squash.Controls;

public sealed class NavigationView : UserControl
{
    private sealed class NavItem
    {
        public required string Text { get; init; }
        public required Control Content { get; init; }
        public required Label Label { get; init; }
    }

    [Browsable(true)]
    [DefaultValue(-1)]
    public int SelectedIndex { get; set; } = -1;

    [Browsable(true)]
    [DefaultValue(200)]
    public int NavigationWidth
    {
        get => _split.SplitterDistance;
        set => _split.SplitterDistance = Math.Max(100, value);
    }

    public Control? SelectedContent => SelectedIndex >= 0 && SelectedIndex < _items.Count
        ? _items[SelectedIndex].Content
        : null;

    private readonly SplitContainer _split        = new();
    private readonly Panel          _navPanel     = new();
    private readonly Panel          _contentPanel = new();
    private readonly List<NavItem>  _items        = [];

    public NavigationView()
    {
        Dock = DockStyle.Fill;

        _split.Dock             = DockStyle.Fill;
        _split.Orientation      = Orientation.Vertical;
        _split.FixedPanel       = FixedPanel.Panel1;
        _split.SplitterWidth    = 1;
        _split.IsSplitterFixed  = true;
        _split.SplitterDistance = 200;
        _split.BackColor        = ColorTranslator.FromHtml("#d5d5d5");

        _navPanel.Dock      = DockStyle.Fill;
        _navPanel.BackColor = SystemColors.Window;

        _contentPanel.Dock      = DockStyle.Fill;
        _contentPanel.BackColor = SystemColors.Control;

        _split.Panel1.Controls.Add(_navPanel);
        _split.Panel2.Controls.Add(_contentPanel);

        Controls.Add(_split);
    }

    #region Control Event Handlers
    private void OnNavClick(object? sender, EventArgs e)
    {
        if (sender is Label { Tag: int index })
        {
            SetActive(index);
        }
    }
    #endregion

    public void AddPage(string text, Control content, Image? image = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(content);

        content.Dock    = DockStyle.Fill;
        content.Visible = false;

        var label = CreateNavLabel(text, _items.Count, image);

        _navPanel.Controls.Add(label);
        _navPanel.Controls.SetChildIndex(label, 0);
        _contentPanel.Controls.Add(content);

        _items.Add(new NavItem
        {
            Text    = text,
            Content = content,
            Label   = label
        });

        if (SelectedIndex == -1)
            SetActive(0);
    }

    public void RemovePage(string text)
    {
        var index = _items.FindIndex(x => x.Text == text);
        if (index < 0)
            return;

        var item = _items[index];

        _navPanel.Controls.Remove(item.Label);
        _contentPanel.Controls.Remove(item.Content);

        item.Label.Dispose();
        item.Content.Dispose();

        _items.RemoveAt(index);

        ReflowNav();

        if (_items.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        SetActive(Math.Clamp(SelectedIndex, 0, _items.Count - 1));
    }

    private Label CreateNavLabel(string text, int index, Image? image)
    {
        var lbl = new Label
        {
            Text      = text,
            Dock      = DockStyle.Top,
            Height    = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor    = Cursors.Hand,
            BackColor = Color.Transparent,
            ForeColor = SystemColors.ControlText,
            Padding   = new Padding(8, 0, 8, 0)
        };

        if (image is not null)
        {
            lbl.TextAlign  = ContentAlignment.MiddleRight;
            lbl.Image      = image;
            lbl.ImageAlign = ContentAlignment.MiddleLeft;
        }

        lbl.Click += (_, _) => SetActive(index);
        lbl.MouseEnter += (_, _) =>
        {
            if (index != SelectedIndex)
                lbl.BackColor = SystemColors.Control;
        };
        lbl.MouseLeave += (_, _) =>
        {
            if (index != SelectedIndex)
                lbl.BackColor = Color.Transparent;
        };

        return lbl;
    }

    private void SetActive(int index)
    {
        if (_items.Count == 0)
            return;

        index = Math.Clamp(index, 0, _items.Count - 1);

        if (SelectedIndex == index)
            return;

        if (SelectedIndex >= 0)
        {
            var prev = _items[SelectedIndex];
            prev.Content.Visible = false;
            prev.Label.BackColor = Color.Transparent;
            prev.Label.ForeColor = SystemColors.ControlText;
        }

        SelectedIndex = index;

        var current = _items[SelectedIndex];
        current.Content.Visible = true;
        current.Label.BackColor = SystemColors.Highlight;
        current.Label.ForeColor = SystemColors.HighlightText;
    }

    private void ReflowNav()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var lbl = _items[i].Label;

            lbl.Click -= OnNavClick;
            lbl.Click += OnNavClick;

            lbl.Tag = i;
        }
    }
}
