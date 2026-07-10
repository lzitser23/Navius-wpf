# 1. Web form-participation parameters drop across all families

Status: accepted

## Context

Several web Navius families expose `Name`/`Value`/`Form` parameters whose only purpose is
mirroring a hidden native `<input>` (or setting `name`/`form` on the real one) so the
component participates in an ordinary HTML `<form>` submission: for example
OneTimePasswordField's `NaviusOneTimePasswordFieldHiddenInput`, and the `Name`/`Form`
parameters on OneTimePasswordField's root and PasswordToggleField's input.

WPF has no HTML form submission model. There is no browser-level "submit this form to a
URL," no hidden-input DOM node to mirror a value into, and no `name`/`form` attribute
concept. The parity extraction's own cross-cutting risk list flagged this in
`docs/parity/README.md`: "Web form-participation parameters (`Name`/`Value`/`Form` hidden-
input mirroring) have no WPF equivalent and likely drop across all form controls; needs one
blanket ADR."

## Decision

Drop `Name`/`Value`/`Form` hidden-input-mirroring parameters, and any part whose sole job is
rendering a hidden native input, across every family in the WPF port. This is one blanket
decision, not a per-family judgment call: wherever a web parameter exists only to interoperate
with native HTML form submission, the WPF port has no equivalent surface to preserve, and
should not fake one.

Where a family separately needs a stable identifier for non-form purposes (e.g. NaviusField's
`Name`, used by NaviusForm to key its `Errors` dictionary and to look fields up by name), that
parameter is kept, since its purpose is unrelated to HTML form submission.

## Consequences

- Every family's WPF port is smaller than its web counterpart by exactly this surface; this
  is expected and should not be treated as a missed parity item during future review.
- Real submission in the WPF port goes through NaviusForm's `SubmitCommand`/`Submitted` event
  (see docs/parity/form.md), not through any native form-post equivalent, since none exists.
- A consumer that needs to post form-shaped data elsewhere (e.g. an HTTP call) reads values
  directly off the bound view-model or control, the same way any ordinary WPF app would; no
  Navius-specific hidden-input bridge is provided.
