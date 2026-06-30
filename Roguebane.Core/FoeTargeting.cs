namespace Roguebane.Core;

// Which limb a foe goes for (data, §8). A foe erodes the player's PARTS the same way the player
// erodes a foe's — the personality only picks WHICH standing part each swing lands on.
public enum FoeAim
{
    Random, // no read of the body — any standing part, uniform
    Smart,  // strip the largest live stat share first (fastest to knock a technique/gear offline)
    Inept,  // botch it: waste the swing on the part with the least left to give
}

// Pure, deterministic part selection for foe offense. Engine-agnostic and headless-testable; the
// shared fight Rng keeps RANDOM reproducible.
public static class FoeTargeting
{
    // Pick the player part this foe's swing lands on, or null when nothing stands (caller then spills
    // onto the HP pool). Ties resolve by the body's part order so SMART/INEPT stay deterministic.
    public static BodyPart? Pick(FoeAim aim, Body target, Rng rng)
    {
        var standing = target.Parts.Where(p => target.Contribution(p) > 0).ToList();
        if (standing.Count == 0) return null;

        return aim switch
        {
            FoeAim.Smart => standing.Aggregate((best, p) =>
                target.Contribution(p) > target.Contribution(best) ? p : best),
            FoeAim.Inept => standing.Aggregate((worst, p) =>
                target.Contribution(p) < target.Contribution(worst) ? p : worst),
            _ => standing[rng.Next(standing.Count)],
        };
    }
}
