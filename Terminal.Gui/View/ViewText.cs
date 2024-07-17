#nullable enable

using static Unix.Terminal.Curses;

namespace Terminal.Gui;

public partial class View
{
    /// <summary>
    ///    Initializes the Text of the View. Called by the constructor.
    /// </summary>
    private void SetupText ()
    {
        Text = string.Empty;
        TextDirection = TextDirection.LeftRight_TopBottom;
    }

    private string _text;

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    ///     or not when <see cref="TextFormatter.WordWrap"/> is enabled.
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    ///     <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
    /// </summary>
    public virtual bool PreserveTrailingSpaces
    {
        get => TextFormatter.PreserveTrailingSpaces;
        set
        {
            if (TextFormatter.PreserveTrailingSpaces != value)
            {
                TextFormatter.PreserveTrailingSpaces = value;
                TextFormatter.NeedsFormat = true;
            }
        }
    }

    /// <summary>
    ///     The text displayed by the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The text will be drawn before any subviews are drawn.
    ///     </para>
    ///     <para>
    ///         The text will be drawn starting at the view origin (0, 0) and will be formatted according
    ///         to <see cref="TextAlignment"/> and <see cref="TextDirection"/>.
    ///     </para>
    ///     <para>
    ///         The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="GetContentSize ()"/>'s height
    ///         is 1, the text will be clipped.
    ///     </para>
    ///     <para>If <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>,
    ///     the <see cref="GetContentSize ()"/> will be adjusted to fit the text.</para>
    ///     <para>When the text changes, the <see cref="TextChanged"/> is fired.</para>
    /// </remarks>
    public virtual string Text
    {
        get => _text;
        set
        {
            string old = _text;
            _text = value;

            UpdateTextFormatterText ();
            OnResizeNeeded ();
#if DEBUG
            if (_text is { } && string.IsNullOrEmpty (Id))
            {
                Id = _text;
            }
#endif
            OnTextChanged ();
        }
    }

    /// <summary>
    /// Called when the <see cref="Text"/> has changed. Fires the <see cref="TextChanged"/> event.
    /// </summary>
    public void OnTextChanged ()
    {
        TextChanged?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>
    ///     Text changed event, raised when the text has changed.
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will
    ///     redisplay the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para> <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the <see cref="GetContentSize ()"/> will be adjusted to fit the text.</para>
    /// </remarks>
    /// <value>The text alignment.</value>
    public virtual Alignment TextAlignment
    {
        get => TextFormatter.Alignment;
        set
        {
            TextFormatter.Alignment = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary>
    ///     Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the
    ///     <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para> <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the <see cref="GetContentSize ()"/> will be adjusted to fit the text.</para>
    /// </remarks>
    /// <value>The text direction.</value>
    public virtual TextDirection TextDirection
    {
        get => TextFormatter.Direction;
        set
        {
            UpdateTextDirection (value);
            TextFormatter.Direction = value;
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="Gui.TextFormatter"/> used to format <see cref="Text"/>.
    /// </summary>
    public TextFormatter TextFormatter { get; init; } = new () { };

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will
    ///     redisplay
    ///     the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para> <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the <see cref="GetContentSize ()"/> will be adjusted to fit the text.</para>
    /// </remarks>
    /// <value>The vertical text alignment.</value>
    public virtual Alignment VerticalTextAlignment
    {
        get => TextFormatter.VerticalAlignment;
        set
        {
            TextFormatter.VerticalAlignment = value;
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
    ///     different format than the default.
    /// </summary>
    protected virtual void UpdateTextFormatterText ()
    {
        if (TextFormatter is { })
        {
            TextFormatter.Text = _text;
        }
    }

    /// <summary>
    ///     Internal API. Sets <see cref="TextFormatter"/>.Size to the current <see cref="Viewport"/> size, adjusted for
    ///     <see cref="TextFormatter.HotKeySpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     Use this API to set <see cref="TextFormatter.Size"/> when the view has changed such that the
    ///     size required to fit the text has changed.
    ///     changes.
    /// </remarks>
    /// <returns></returns>
    internal void SetTextFormatterSize ()
    {
        // View subclasses can override UpdateTextFormatterText to modify the Text it holds (e.g. Checkbox and Button).
        // We need to ensure TextFormatter is accurate by calling it here.
        UpdateTextFormatterText ();

        // Default is to use GetContentSize ().
        var size = GetContentSize ();

        // TODO: This is a hack. Figure out how to move this into DimDimAuto
        // Use _width & _height instead of Width & Height to avoid debug spew
        DimAuto? widthAuto = _width as DimAuto;
        DimAuto? heightAuto = _height as DimAuto;
        if ((widthAuto is { } && widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
            || (heightAuto is { } && heightAuto.Style.FastHasFlags (DimAutoStyle.Text)))
        {
            int width = 0;
            int height = 0;

            if (widthAuto is null || !widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
            {
                width = GetContentSize ().Width;
            }

            if (heightAuto is null || !heightAuto.Style.FastHasFlags (DimAutoStyle.Text))
            {
                height = GetContentSize ().Height;
            }

            if (widthAuto is { } && widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
            {
                if (height == 0 && heightAuto is { } && heightAuto.Style.FastHasFlags (DimAutoStyle.Text))
                {
                    height = Application.Screen.Height;
                }
                width = TextFormatter.FormatAndGetSize (new (Application.Screen.Width, height)).Width;
            }

            if (heightAuto is { } && heightAuto.Style.FastHasFlags (DimAutoStyle.Text))
            {
                if (width == 0 && widthAuto is { } && widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
                {
                    width = Application.Screen.Height;
                }
                height = TextFormatter.FormatAndGetSize (new (width, Application.Screen.Height)).Height;
            }

            size = new (width, height);
        }

        TextFormatter.Size = size;
    }

    private void UpdateTextDirection (TextDirection newDirection)
    {
        bool directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction) != TextFormatter.IsHorizontalDirection (newDirection);
        TextFormatter.Direction = newDirection;

        UpdateTextFormatterText ();

        if (directionChanged)
        {
            OnResizeNeeded ();
        }

        SetTextFormatterSize ();
        SetNeedsDisplay ();
    }
}
