using System;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Mirrors the web contract's NaviusToastAction: a label, a required plain-text AltText for
/// assistive tech (contract: "required plain-text description ... for users who cannot perform
/// the underlying gesture"), and a handler invoked before the toast closes.
/// </summary>
public sealed record ToastActionSpec(string Label, Action OnClick, string AltText = "");
