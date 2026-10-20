using System.Globalization;
using System.Text;

namespace P3DAutoBuild;

// Pilih model library terdekat lalu tulis XML untuk bglcomp.
public static class Emitter
{
    public static string Build(IEnumerable<BuildingSpec> specs, List<ModelEntry> models)
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<FSData version=\"9.0\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"bglcomp.xsd\">");

        foreach (var s in specs)
        {
            var (m, scale) = Pick(s, models);
            sb.AppendLine($"  <SceneryObject lat=\"{s.Lat.ToString("F7", ci)}\" lon=\"{s.Lon.ToString("F7", ci)}\" alt=\"0\" altitudeIsAgl=\"TRUE\" pitch=\"0\" bank=\"0\" heading=\"{s.HeadingDeg.ToString("F2", ci)}\" imageComplexity=\"NORMAL\">");
            sb.AppendLine($"    <LibraryObject name=\"{m.Guid}\" scale=\"{scale.ToString("F2", ci)}\" />");
            sb.AppendLine("  </SceneryObject>");
        }
        sb.AppendLine("</FSData>");
        return sb.ToString();
    }

    static (ModelEntry, double) Pick(BuildingSpec s, List<ModelEntry> models)
    {
        ModelEntry best = models[0];
        double bestScore = double.MaxValue;
        foreach (var m in models)
        {
            double score = Math.Abs(Math.Log(s.Width / m.Width))
                         + Math.Abs(Math.Log(s.Length / m.Length))
                         + Math.Abs(Math.Log(s.Height / m.Height))
                         + (m.Roof.Equals(s.Roof.ToString(), StringComparison.OrdinalIgnoreCase) ? 0 : 1.5);
            if (score < bestScore) { bestScore = score; best = m; }
        }
        // Scale di P3D uniform, pakai rata-rata geometrik ketiga rasio
        double scale = Math.Pow((s.Width / best.Width) * (s.Length / best.Length) * (s.Height / best.Height), 1.0 / 3);
        return (best, Math.Clamp(scale, 0.6, 2.5));
    }
}
