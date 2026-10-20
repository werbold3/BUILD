using System.Globalization;

namespace P3DAutoBuild;

// "Otak" sistem: ubah footprint OSM jadi spesifikasi bangunan.
// Rule-based + heuristik. Bisa diganti model ML nanti lewat method EstimateHeight/EstimateRoof.
public static class Analyzer
{
    const double MPerDegLat = 110540.0;

    public static BuildingSpec? Analyze(Building b)
    {
        double lat0 = b.Footprint.Average(p => p.Lat);
        double lon0 = b.Footprint.Average(p => p.Lon);
        double mPerDegLon = 111320.0 * Math.Cos(lat0 * Math.PI / 180);

        var pts = b.Footprint
            .Select(p => ((p.Lon - lon0) * mPerDegLon, (p.Lat - lat0) * MPerDegLat))
            .ToList();

        var (cx, cy, w, l, longAxisDeg) = MinRect(pts);
        if (w < 2 || l < 2) return null; // terlalu kecil, skip

        double heading = ((90 - longAxisDeg) % 360 + 360) % 360; // utara=0, searah jarum jam
        double height = EstimateHeight(b, w * l);
        var roof = EstimateRoof(b, w * l);

        return new BuildingSpec(b.Id,
            lat0 + cy / MPerDegLat, lon0 + cx / mPerDegLon,
            w, l, height, heading, roof);
    }

    static double EstimateHeight(Building b, double area)
    {
        if (b.Tags.TryGetValue("height", out var h) && TryNum(h, out var hv)) return hv;
        if (b.Tags.TryGetValue("building:levels", out var lv) && TryNum(lv, out var lvv)) return lvv * 3.2 + 1.0;

        var rnd = new Random((int)(b.Id % int.MaxValue));
        string type = b.Tags.GetValueOrDefault("building", "yes");
        double baseH = type switch
        {
            "house" or "detached" or "residential" or "semidetached_house" => 5.5,
            "apartments" or "dormitory" => 16,
            "commercial" or "office" or "retail" => 11,
            "industrial" or "warehouse" => 8,
            "garage" or "garages" or "shed" or "roof" => 3,
            "church" or "cathedral" or "mosque" => 12,
            _ => area > 800 ? 10 : 6
        };
        return baseH * (0.85 + rnd.NextDouble() * 0.3); // variasi deterministik
    }

    static RoofType EstimateRoof(Building b, double area)
    {
        if (b.Tags.TryGetValue("roof:shape", out var rs))
            return rs is "gabled" or "hipped" or "pitched" ? RoofType.Ridge : RoofType.Flat;
        string type = b.Tags.GetValueOrDefault("building", "yes");
        bool residential = type is "house" or "detached" or "residential" or "semidetached_house" or "yes";
        return residential && area < 300 ? RoofType.Ridge : RoofType.Flat;
    }

    static bool TryNum(string s, out double v)
    {
        var digits = new string(s.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        return double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v > 0;
    }

    // Oriented minimum-area bounding rectangle (brute force 1 derajat)
    static (double cx, double cy, double w, double l, double longAxisDeg) MinRect(List<(double x, double y)> p)
    {
        double bestArea = double.MaxValue, bu0 = 0, bu1 = 0, bv0 = 0, bv1 = 0, bestA = 0;
        for (int a = 0; a < 90; a++)
        {
            double r = a * Math.PI / 180, c = Math.Cos(r), s = Math.Sin(r);
            double u0 = double.MaxValue, u1 = double.MinValue, v0 = double.MaxValue, v1 = double.MinValue;
            foreach (var (x, y) in p)
            {
                double u = x * c + y * s, v = -x * s + y * c;
                u0 = Math.Min(u0, u); u1 = Math.Max(u1, u);
                v0 = Math.Min(v0, v); v1 = Math.Max(v1, v);
            }
            double area = (u1 - u0) * (v1 - v0);
            if (area < bestArea) { bestArea = area; bu0 = u0; bu1 = u1; bv0 = v0; bv1 = v1; bestA = r; }
        }
        double cu = (bu0 + bu1) / 2, cv = (bv0 + bv1) / 2;
        double cx = cu * Math.Cos(bestA) - cv * Math.Sin(bestA);
        double cy = cu * Math.Sin(bestA) + cv * Math.Cos(bestA);
        double du = bu1 - bu0, dv = bv1 - bv0;
        double axis = du >= dv ? bestA * 180 / Math.PI : bestA * 180 / Math.PI + 90;
        return (cx, cy, Math.Min(du, dv), Math.Max(du, dv), axis);
    }
}
