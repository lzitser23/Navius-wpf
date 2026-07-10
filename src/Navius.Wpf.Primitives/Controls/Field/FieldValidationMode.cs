namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Gates when a NaviusField's validity is surfaced to NaviusFieldError / the data-valid /
/// data-invalid state, mirroring the web contract's FieldValidationMode. Default is
/// OnSubmit: nothing in this family triggers that reveal on its own, an external caller
/// (NaviusForm.Submit, or a consumer calling NaviusField.Reveal() directly) has to.
/// </summary>
public enum FieldValidationMode
{
    OnSubmit,
    OnBlur,
    OnChange,
}
