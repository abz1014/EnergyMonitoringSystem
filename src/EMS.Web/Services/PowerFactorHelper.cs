namespace EMS.Web.Services;

using EMS.Core.Models;

// Data Provenance:
// Table: tblEnergyMetersData | Columns: PFL1, PFL2, PFL3 | Derived (simple average)
// Assumption: phases are averaged unweighted (not current-weighted) -- a disclosed
// simplification; true PF under unbalanced loading should weight by phase current.
// Validatable against SCADA: yes, matches the meter's own per-phase PF readings.
// Confidence: High (direct measurement, simple aggregation only).
public static class PowerFactorHelper
{
    // Per-row 3-phase average PF, ignoring phases with no/zero reading rather than treating them as 0
    public static double? ThreePhaseAverage(EnergyMeterData d)
    {
        var values = new List<double>();
        if (d.PFL1.HasValue && d.PFL1.Value > 0) values.Add(d.PFL1.Value);
        if (d.PFL2.HasValue && d.PFL2.Value > 0) values.Add((double)d.PFL2.Value);
        if (d.PFL3.HasValue && d.PFL3.Value > 0) values.Add((double)d.PFL3.Value);
        return values.Count > 0 ? values.Average() : null;
    }
}
