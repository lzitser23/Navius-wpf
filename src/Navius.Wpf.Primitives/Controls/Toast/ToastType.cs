namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Visual/semantic toast type. Mirrors the web contract's NaviusToastRoot.Type
/// ("success"|"error"|"loading", or the unset default) and drives NaviusToast's
/// data-type-equivalent template trigger.
/// </summary>
public enum ToastType
{
    Default,
    Success,
    Error,
    Loading,
}
