using System.Globalization;
using System.Text.Json;
using P3DAutoBuild;

// Contoh (generic, default):
//   P3DAutoBuild --bbox -3.99,122.50,-3.97,122.52 --out buildings.xml
// Mode library object:
//   P3DAutoBuild --mode library --bbox ... --models models.json --out buildings.xml
string Arg(string name, string def = "")
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : def;
}

string bboxStr = Arg("--bbox");
if (string.IsNullOrEmpty(bboxStr))
{
    Console.WriteLine("Usage: P3DAutoBuild --bbox south,west,north,east [--mode generic|library] [--textures textures.json] [--models models.json] [--out buildings.xml]");
    return 1;
}
var bb = bboxStr.Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
if (bb.Length != 4) { Console.WriteLine("bbox harus 4 angka: south,west,north,east"); return 1; }

string mode = Arg("--mode", "generic").ToLowerInvariant();
string outPath = Arg("--out", "buildings.xml");
var jsonOpt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine("Mengambil data OSM...");
var buildings = await OsmClient.FetchAsync(bb[0], bb[1], bb[2], bb[3]);
Console.WriteLine($"{buildings.Count} bangunan ditemukan.");
var specs = buildings.Select(Analyzer.Analyze).Where(s => s != null).Select(s => s!).ToList();

string xml;
if (mode == "library")
{
    var models = JsonSerializer.Deserialize<List<ModelEntry>>(File.ReadAllText(Arg("--models", "models.json")), jsonOpt);
    if (models == null || models.Count == 0) { Console.WriteLine("models.json kosong."); return 1; }
    xml = Emitter.Build(specs, models);
}
else
{
    string texPath = Arg("--textures", "textures.json");
    var sets = File.Exists(texPath)
        ? JsonSerializer.Deserialize<List<TextureSet>>(File.ReadAllText(texPath), jsonOpt) ?? TextureSet.Defaults()
        : TextureSet.Defaults();
    if (sets.Count == 0) sets = TextureSet.Defaults();
    xml = GenericEmitter.Build(specs, sets);
}

File.WriteAllText(outPath, xml);
Console.WriteLine($"{specs.Count} bangunan diproses ({mode}) -> {outPath}. Lanjut: bglcomp {outPath}");
return 0;
