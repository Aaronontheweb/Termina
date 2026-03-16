namespace Termina.Layout;

/// <summary>
/// How a copyable node should acknowledge a successful copy action.
/// </summary>
public enum CopyFeedbackMode
{
    None,
    Toast,
    InlineIndicator,
    ToastAndInline
}
