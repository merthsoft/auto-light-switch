namespace Merthsoft.AutoOnAutoOff;

// Independent of poll scheduling: the grace period starts at observed vacancy.
internal sealed class VacancyTimer
{
    public int Since = -1;

    public bool MayTurnOff(bool occupied, int now, int delay)
    {
        if (occupied)
        {
            Since = -1;
            return false;
        }
        if (Since < 0 || now < Since)
            Since = now;
        return now - Since >= System.Math.Max(0, delay);
    }
}
