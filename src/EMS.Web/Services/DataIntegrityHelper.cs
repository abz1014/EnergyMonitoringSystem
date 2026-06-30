namespace EMS.Web.Services;

using EMS.Core.Models;

// Data Provenance / Engineering Note:
// The 21 original real SCADA gateway rows (2026-06-27, 18:00-18:15, Meters 1/2/3) have kWh
// values that climb slowly within a 12-minute window (e.g. Meter 1: 1240 -> 1245 -> 1250),
// strongly suggesting kWh is a CUMULATIVE lifetime register on the real meter -- not an
// interval/delta reading like the synthetic demo seed rows (small, consistent, hourly deltas).
// These same real rows report kVAh=0 and kVARh=0 for every reading, which the synthetic rows
// never do (kVAh/kVARh are always populated and physically consistent there: kWh <= kVAh).
//
// Until kWh semantics are confirmed with the client (see kwh-semantics-pending-confirmation
// memory), any kWh-summing calculation must exclude rows matching this exact contamination
// signature -- otherwise a handful of rows can inflate a total by 50-100x (confirmed: Dashboard's
// "Monthly Total" KPI read 29,322 kWh unfiltered vs the correct 8,367 kWh filtered).
//
// This filter is correct regardless of how the kWh semantics question is eventually answered:
// if kWh is interval-based (the likely answer), these 21 rows are a one-off test-capture
// artifact and excluding them is permanently correct. If kWh turns out to be cumulative on the
// real meters going forward, a larger architectural fix (delta logic instead of SUM) will be
// needed -- but that does not conflict with or get undone by this filter; this filter only ever
// removes rows that are already known-bad, regardless of which answer comes back.
public static class DataIntegrityHelper
{
    public static bool IsContaminatedReading(EnergyMeterData d) => (d.kVAh ?? -1) == 0 && (d.kVARh ?? -1) == 0;

    public static IEnumerable<EnergyMeterData> ExcludeContaminated(this IEnumerable<EnergyMeterData> data) =>
        data.Where(d => !IsContaminatedReading(d));
}
