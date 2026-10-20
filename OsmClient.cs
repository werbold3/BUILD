using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

namespace P3DAutoBuild;

public static class OsmClient
{
    // Server Overpass cadangan, dicoba berurutan sampai ada yang berhasil
    static readonly string[] Endpoints =
    {
        "https://overpass-api.de/api/interpreter",
        "https://overpass.kumi.systems/api/interpreter",
        "https://overpass.private.coffee/api/interpreter",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter"
    };

    public static string BuildQuery(double s, double w, double n, double e)
    {
        var ci = CultureInfo.InvariantCulture;
        return $"[out:json][timeout:90];way[\"building\"]({s.ToString(ci)},{w.ToString(ci)},{n.ToString(ci)},{e.ToString(ci)});out geom;";
    }

    public static async Task<List<Building>> FetchAsync(double s, double w, double n, double e)
    {
        string q = BuildQuery(s, w, n, e);
        var handler = new SocketsHttpHandler
        {
            SslOptions = { EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13 }
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("P3DAutoBuild/1.0");

        foreach (var url in Endpoints)
        {
            try
            {
                Console.WriteLine($"Mencoba {new Uri(url).Host} ...");
                var content = new StringContent("data=" + Uri.EscapeDataString(q), Encoding.UTF8, "application/x-www-form-urlencoded");
                var resp = await http.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();
                return Parse(await resp.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  gagal: {ex.GetBaseException().Message}");
            }
        }
        throw new InvalidOperationException(
            "Semua server Overpass gagal. Download manual lewat browser lalu pakai --osm-file osm.json (lihat README).");
    }

    // Mendukung: .osm / .xml / .osm.gz (OSM XML dari openstreetmap.org atau BBBike) dan .json (Overpass)
    public static List<Building> LoadFile(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.EndsWith(".osm") || lower.EndsWith(".xml") || lower.EndsWith(".gz"))
            return ParseXml(path);
        return Parse(File.ReadAllText(path));
    }

    static List<Building> ParseXml(string path)
    {
        using Stream fs = File.OpenRead(path);
        using Stream s = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(fs, CompressionMode.Decompress) : fs;
        var doc = XDocument.Load(s);
        var root = doc.Root ?? throw new InvalidDataException("File OSM kosong.");

        var nodes = new Dictionary<long, GeoPoint>();
        foreach (var n in root.Elements("node"))
            nodes[(long)n.Attribute("id")!] = new GeoPoint((double)n.Attribute("lat")!, (double)n.Attribute("lon")!);

        var list = new List<Building>();
        foreach (var w in root.Elements("way"))
        {
            var tags = new Dictionary<string, string>();
            foreach (var t in w.Elements("tag"))
                tags[(string)t.Attribute("k")!] = (string?)t.Attribute("v") ?? "";
            if (!tags.ContainsKey("building")) continue;

            var pts = w.Elements("nd")
                .Select(nd => (long)nd.Attribute("ref")!)
                .Where(nodes.ContainsKey)
                .Select(id => nodes[id])
                .ToList();
            if (pts.Count < 4) continue;

            list.Add(new Building((long)w.Attribute("id")!, pts, tags));
        }
        return list;
    }

    static List<Building> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
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
