using System.Globalization;
using System.Text;
using System.Text.Json;

namespace P3DAutoBuild;

public static class OsmClient
{
    const string Endpoint = "https://overpass-api.de/api/interpreter";

    public static async Task<List<Building>> FetchAsync(double s, double w, double n, double e)
    {
        var ci = CultureInfo.InvariantCulture;
        string q = $"[out:json][timeout:90];way[\"building\"]({s.ToString(ci)},{w.ToString(ci)},{n.ToString(ci)},{e.ToString(ci)});out geom;";
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("P3DAutoBuild/1.0");
        var resp = await http.PostAsync(Endpoint,
            new StringContent("data=" + Uri.EscapeDataString(q), Encoding.UTF8, "application/x-www-form-urlencoded"));
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

        var list = new List<Building>();
        foreach (var el in doc.RootElement.GetProperty("elements").EnumerateArray())
        {
            if (!el.TryGetProperty("geometry", out var geo)) continue;
            var pts = geo.EnumerateArray()
                .Select(g => new GeoPoint(g.GetProperty("lat").GetDouble(), g.GetProperty("lon").GetDouble()))
                .ToList();
            if (pts.Count < 4) continue;

            var tags = new Dictionary<string, string>();
            if (el.TryGetProperty("tags", out var t))
                foreach (var p in t.EnumerateObject()) tags[p.Name] = p.Value.GetString() ?? "";

            list.Add(new Building(el.GetProperty("id").GetInt64(), pts, tags));
        }
        return list;
    }
}
