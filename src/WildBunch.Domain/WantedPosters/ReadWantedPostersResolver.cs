using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using DomainGameSession = WildBunch.Domain.Game.GameSession;

namespace WildBunch.Domain.WantedPosters;

public sealed class ReadWantedPostersResolver
{
    public ReadWantedPostersResult ReadWantedPosters(DomainGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var currentTown = session.World.GetTown(session.Player.CurrentTownId);

        if ((currentTown.Services & TownServices.NoticeBoard) == 0)
        {
            return ReadWantedPostersResult.Failed("There are no wanted posters here.");
        }

        var clue = session.CaseFile.RevealNextPublicClue();

        if (clue is null)
        {
            session.ApplyCaseUpdate("You study the wanted posters, but find nothing new.");
            return ReadWantedPostersResult.Succeeded("You study the wanted posters, but find nothing new.", sessionChanged: true);
        }

        session.ApplyCaseUpdate($"You study the wanted posters and note a public lead: {clue.Description}.");
        return ReadWantedPostersResult.Succeeded("You study the wanted posters and uncover a public lead.", sessionChanged: true);
    }
}
