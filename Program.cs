using System.Globalization;
using System.Text.Json;
using P3DAutoBuild;

// Contoh: P3DAutoBuild --bbox -3.99,122.50,-3.97,122.52 --models models.json --out buildings.xml
string Arg(string name, string def = "")
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : def;
}

string bboxStr = Arg("--bbox");
if (string.IsNullOrEmpty(bboxStr))
{
    Console.WriteLine("Usage: P3DAutoBuild --bbox south,west,north,east [--models models.json] [--out buildings.xml]");
    return 1;
}
var bb = bboxStr.Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
if (bb.Length != 4) { Console.WriteLine("bbox harus 4 angka: south,west,north,east"); return 1; }

string modelsPath = Arg("--models", "models.json");
string outPath = Arg("--out", "buildings.xml");

var models = JsonSerializer.Deserialize<List<ModelEntry>>(File.ReadAllText(modelsPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
if (models == null || models.Count == 0) { Console.WriteLine("models.json kosong."); return 1; }

Console.WriteLine("Mengambil data OSM...");
var buildings = await OsmClient.FetchAsync(bb[0], bb[1], bb[2], bb[3]);
Console.WriteLine($"{buildings.Count} bangunan ditemukan.");

var specs = buildings.Select(Analyzer.Analyze).Where(s => s != null).Select(s => s!).ToList();
File.WriteAllText(outPath, Emitter.Build(specs, models));
Console.WriteLine($"{specs.Count} bangunan ditulis ke {outPath}. Lanjut: bglcomp {outPath}");
return 0;
