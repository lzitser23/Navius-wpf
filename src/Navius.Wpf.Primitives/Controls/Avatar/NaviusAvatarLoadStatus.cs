namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Mirrors the contract's AvatarContext.Status (docs/parity/avatar.md "State + data attributes"):
/// Idle (no Source set), Loading (Source assigned, image not yet resolved), Loaded, Error.
/// </summary>
public enum NaviusAvatarLoadStatus
{
    Idle,
    Loading,
    Loaded,
    Error,
}
