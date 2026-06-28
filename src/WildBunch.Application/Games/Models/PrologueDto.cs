namespace WildBunch.Application.Games.Models;

/// <summary>
/// Player-facing prologue read model. The <see cref="Body"/> contains the substituted
/// copy (no <c>{trueCulpritMainIdentifier}</c> placeholder). No hidden culprit internals
/// (TrueCulpritId, isTrueCulprit, internal suspect ids) are exposed.
/// </summary>
public sealed record PrologueDto(
    string Heading,
    string Body,
    string PrimaryAction,
    string VariantId);
