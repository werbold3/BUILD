using System.Globalization;
using System.Text.Json;
using P3DAutoBuild;

// Online:   P3DAutoBuild --bbox -3.99,122.50,-3.97,122.52 --out buildings.xml
// Offline:  P3DAutoBuild --osm-file osm.json --out buildings.xml
// Library:  tambahkan --mode library --models models.json
string Arg(string name, string def = "")
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : def;
}

string osmFile = Arg("--osm-file");
string bboxStr = Arg("--bbox");
if (string.IsNullOrEmpty(osmFile) && string.IsNullOrEmpty(bboxStr))
{
    Console.WriteLine("Usage: P3DAutoBuild (--bbox south,west,north,east | --osm-file osm.json) [--mode generic|library] [--textures textures.json] [--models models.json] [--out buildings.xml]");
    return 1;
}

string mode = Arg("--mode", "generic").ToLowerInvariant();
string outPath = Arg("--out", "buildings.xml");
var jsonOpt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

List<Building> buildings;
try
{
    if (!string.IsNullOrEmpty(osmFile))
    {
        Console.WriteLine($"Membaca {osmFile} ...");
        buildings = OsmClient.LoadFile(osmFile);
    }
    else
    {
        var bb = bboxStr.Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        if (bb.Length != 4) { Console.WriteLine("bbox harus 4 angka: south,west,north,east"); return 1; }
        Console.WriteLine("Mengambil data OSM...");
        buildings = await OsmClient.FetchAsync(bb[0], bb[1], bb[2], bb[3]);
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
    return 1;
}
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
