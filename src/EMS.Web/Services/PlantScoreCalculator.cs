namespace EMS.Web.Services;

// Data Provenance:
// Inputs: average power factor (PFL1-3, via PowerFactorHelper), consumption change % vs a
// 7-day trailing average (kWh), active alarm count (Alarms.IsActive), data availability ratio
// (online meters / total devices). All derived from already-computed values; this class does
// no database access itself.
//
// SHARED ON PURPOSE: Briefing's "Plant Score" and Dashboard's "Energy Score" were two
// independently-implemented copies of the exact same 5-component formula (PF 30pts /
// Consumption 25pts / Alarms 20pts / Power Quality 15pts / Data 10pts). They had already
// drifted -- Dashboard's consumption component was a hardcoded 25 (full marks, always),
// never actually computing a real consumption-change score, while Briefing's was fully
// implemented. This class is the single source of truth both pages now call, so the two
// scores can never again silently diverge.
//
// Confidence: components are simple threshold bands, not statistically validated weights --
// disclosed as a known simplification, same as when this was two separate implementations.
public static class PlantScoreCalculator
{
    public class Result
    {
        public int Score { get; set; }
        public int PfScore { get; set; }
        public int ConsumptionScore { get; set; }
        public int AlarmScore { get; set; }
        public int PowerQualityScore { get; set; }
        public int DataQualityScore { get; set; }
        public string Trend { get; set; } = "stable";
    }

    /// <param name="avgPowerFactor">3-phase average PF for the period (0-1)</param>
    /// <param name="consumptionChangePercent">% deviation from the 7-day trailing average (can be negative)</param>
    /// <param name="activeAlarmCount">Count of currently active alarms</param>
    /// <param name="dataAvailabilityRatio">Online/reporting meters divided by total meters (0-1)</param>
    public static Result Calculate(double avgPowerFactor, double consumptionChangePercent, int activeAlarmCount, double dataAvailabilityRatio)
    {
        var pfScore = avgPowerFactor >= 0.95 ? 30 : avgPowerFactor >= 0.90 ? 20 : avgPowerFactor >= 0.85 ? 10 : 0;

        var absChange = Math.Abs(consumptionChangePercent);
        var consumptionScore = absChange <= 5 ? 25 : absChange <= 10 ? 15 : absChange <= 20 ? 5 : 0;

        var alarmScore = activeAlarmCount == 0 ? 20 : activeAlarmCount <= 3 ? 15 : activeAlarmCount <= 10 ? 5 : 0;

        // Power Quality sub-score is intentionally also PF-based today (no harmonic/imbalance
        // input is wired into this plant-wide score -- that level of detail lives in the
        // separate, per-meter Equipment Health Score). This means PF currently influences two
        // of five components; disclosed rather than hidden, since collapsing them into one
        // would change the score's historical meaning for anyone tracking it over time.
        var pqScore = avgPowerFactor >= 0.92 ? 15 : avgPowerFactor >= 0.85 ? 10 : 5;

        var dataScore = dataAvailabilityRatio >= 0.95 ? 10 : dataAvailabilityRatio >= 0.80 ? 5 : 0;

        var total = pfScore + consumptionScore + alarmScore + pqScore + dataScore;
        var trend = activeAlarmCount == 0 && avgPowerFactor >= 0.90 ? "improving"
            : activeAlarmCount > 5 ? "declining"
            : "stable";

        return new Result
        {
            Score = total,
            PfScore = pfScore,
            ConsumptionScore = consumptionScore,
            AlarmScore = alarmScore,
            PowerQualityScore = pqScore,
            DataQualityScore = dataScore,
            Trend = trend
        };
    }
}
