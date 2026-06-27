namespace WildBunch.GameContent.Prologue;

/// <summary>
/// Code-backed catalog of prologue copy for the start flow (name entry, story so far,
/// starting town selection) and the Start Over confirmation dialog (used by Task 4.2).
/// Follows the code-backed catalog pattern (ADR-0012). Copy is verbatim from the copy doc.
/// </summary>
public static class PrologueContent
{
    // Name entry copy
    public const string NameEntryHeading = "Howdy, pard'ner. What name d'you go by?";
    public const string NameEntryHelper = "A name's a useful thing to have when folks start shouting after you.";
    public const string NameEntryPrimaryAction = "Continue";
    public const string NameEntryValidation = "Tell me what name you go by before we ride on.";

    // Story so far copy
    public const string StorySoFarHeading = "The story so far";
    public const string StorySoFarPrimaryAction = "I understand. Keep riding.";

    // Starting town copy
    public const string StartingTownHeading = "Pick a starting town";
    public const string StartingTownBody = "You cannot go back to the town where the dying man fell. The sheriff will have that place locked down by now.\n\nSo pick the town where your run begins proper. From there, you will follow leads, read wanted posters, ride the trails, and hunt for the Wild Bunch killer before the law catches up with you.";
    public const string StartingTownEmptyState = "Saddling up the map…";
    public const string StartingTownPrimaryActionTemplate = "Start in {townName}";
    public const string StartingTownValidation = "Pick a town before you ride.";

    // Start Over copy (used by Task 4.2)
    public const string SettingsEntryLabel = "Game Settings";
    public const string SettingsHeading = "Game Settings";
    public const string SettingsSectionHeading = "Playthrough";
    public const string StartOverActionLabel = "Start Over";
    public const string StartOverHelper = "Archive this playthrough and begin again from the start.";
    public const string StartOverConfirmTitle = "Start over?";
    public const string StartOverConfirmBody = "This will archive your current playthrough and return you to the beginning.\n\nYour old game will not be deleted. It will be kept for posterity, and later you may be able to restore archived playthroughs. For now, only one playthrough can be active at a time.";
    public const string StartOverCancelLabel = "Cancel";
    public const string StartOverConfirmLabel = "Archive and Start Over";
    public const string StartOverSuccessCopy = "Your old playthrough has been archived. Start a new one when you are ready.";

    // Body copy variants — flavour-only wording, same starting facts preserved.
    // The {trueCulpritMainIdentifier} placeholder is substituted by Task 2.4's endpoint.
    public static IReadOnlyList<PrologueVariant> Variants { get; } =
    [
        new PrologueVariant(
            "prologue.story-so-far.variant-1",
            @"You were minding your own business when you found a man bleeding out in the dust.

He caught your sleeve with a hand gone cold and gasped his last words:

""It was a member of the Wild Bunch. The one with {trueCulpritMainIdentifier}.""

Then he died in your arms.

That was when the sheriff came running.

He found you kneeling over a dead man, your clothes red with blood, a smoking gun in your hand. Maybe the killer dropped it there. Maybe the sheriff did not care. His hand went to his holster and his voice cracked across the street.

""Lay that weapon down. I'm taking you in for murder.""

You ran.

Bullets snapped past your ears as you tore out of that unnamed town and into the open country. Now there is a warrant on your head, and every mile of trail carries the same truth: you are a fugitive wanted for a killing you did not commit.

Your only hope is to find the real killer, bring them in, and prove what the dying man told you.

The killer rides with the Wild Bunch."),
        new PrologueVariant(
            "prologue.story-so-far.variant-2",
            @"You were just passing through when you heard a man choking on his last breaths beside the trail.

By the time you reached him, the dust beneath him had gone dark. He clutched at your coat and forced out a whisper:

""Wild Bunch… the one with {trueCulpritMainIdentifier}.""

Then his eyes went still.

You barely had time to understand the words before the sheriff rounded the corner.

To him, the scene was plain enough: one dead man, one stranger covered in blood, one smoking gun lying in your grip like a confession.

""Drop it,"" he shouted. ""You're coming with me for murder.""

You did not wait for the rope.

You bolted while gunfire split the air behind you. Now your name is tied to a killing, and the law will not care much for your side of it unless you can drag the truth into daylight.

Somewhere out there rides the real killer.

A member of the Wild Bunch."),
        new PrologueVariant(
            "prologue.story-so-far.variant-3",
            @"The trouble started with a dying man and a few words he had no time left to explain.

You found him sprawled in the dirt, bleeding hard and fading fast. When you knelt beside him, he looked through you like he could already see the grave.

""It was one of the Wild Bunch,"" he rasped. ""The one with {trueCulpritMainIdentifier}.""

Then he was gone.

A moment later, the sheriff came upon you with blood on your hands and a smoking gun where the killer must have left it. He did not ask what happened. He drew down and called you murderer.

You ran because hanging men do not get much chance to prove a point.

Now the country knows you as a fugitive, and the only road back to your good name runs through the Wild Bunch. Find the one the dying man named. Take them in. Clear yourself before the law catches up.")
    ];

    /// <summary>
    /// Returns the variant with the given id, or falls back to the first variant
    /// when the id is unknown.
    /// </summary>
    public static PrologueVariant GetVariant(string id) =>
        Variants.FirstOrDefault(v => v.Id == id) ?? Variants[0];
}

/// <summary>
/// A flavour variant of the prologue body copy. All variants preserve the same
/// starting facts; they differ only in wording. The <see cref="BodyTemplate"/>
/// contains the <c>{trueCulpritMainIdentifier}</c> placeholder, substituted at
/// runtime by Task 2.4's endpoint. No internal culprit ids are exposed.
/// </summary>
public sealed record PrologueVariant(string Id, string BodyTemplate);
