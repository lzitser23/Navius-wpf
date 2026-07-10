namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Mirrors the web contract's NaviusToastRoot.Priority: Low maps to role="status" (polite),
/// High maps to role="alert" (assertive). Drives NaviusToastAutomationPeer's LiveSetting and
/// the AutomationNotificationProcessing used by NaviusToast's UIA notification.
/// </summary>
public enum ToastPriority
{
    Low,
    High,
}
