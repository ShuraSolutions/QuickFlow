namespace QuickFlow.Domain.Common;

public static class Percent
{
    /// <summary>part / total as a percentage rounded to 2 decimals; 0 when total is 0.</summary>
    public static double Of(int part, int total) =>
        total <= 0 ? 0 : Math.Round(part * 100.0 / total, 2, MidpointRounding.AwayFromZero);
}
