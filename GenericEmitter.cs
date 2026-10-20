using System.Globalization;
using System.Text;

namespace P3DAutoBuild;

// Satu set tekstur Generic Building (ID dari "Generic Building Textures.xls" di BGL Compiler SDK)
public class TextureSet
{
    public string Name { get; set; } = "";
    public int BottomTexture { get; set; }
    public int WindowTexture { get; set; }
    public int TopTexture { get; set; }
    public int RoofTexture { get; set; }
    public int GableTexture { get; set; }

    // Kombinasi ID yang dipakai di contoh scenery publik. Cek tampilannya, lalu ganti sesuai selera.
    public static List<TextureSet> Defaults() => new()
    {
        new() { Name = "A", BottomTexture = 130, WindowTexture = 118, TopTexture = 0, RoofTexture = 37, GableTexture = 1109 },
        new() { Name = "B", BottomTexture = 148, WindowTexture = 118, TopTexture = 0, RoofTexture = 37, GableTexture = 1118 },
        new() { Name = "C", BottomTexture = 147, WindowTexture = 118, TopTexture = 0, RoofTexture = 35, GableTexture = 1093 },
    };
}

// Tulis XML GenericBuilding (RectangularBuilding) untuk bglcomp P3D v4.5.
public static class GenericEmitter
{
    // Konstanta tiling tekstur: perkiraan dari contoh publik. Kalau tekstur terlihat melar, ubah di sini.
    const double WallTexPerM = 3.5;
    const double WindowTexYPerM = 40;
    const double RoofTexXPerM = 85;
    const double RoofTexZPerM = 100;
    const double MaxSize = 500;

    public static string Build(IEnumerable<BuildingSpec> specs, List<TextureSet> sets)
    {
        var ci = CultureInfo.InvariantCulture;
        string F(double v) => v.ToString("F2", ci);
        int I(double v) => Math.Max(1, (int)Math.Round(v));

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<FSData version=\"9.0\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"bglcomp.xsd\">");

        foreach (var s in specs)
        {
            if (s.Width > MaxSize || s.Length > MaxSize) continue;

            var ts = sets[(int)(Math.Abs(s.Id) % sets.Count)]; // variasi tekstur, konsisten per bangunan
            bool ridge = s.Roof == RoofType.Ridge;

            double roofY = ridge ? Math.Clamp(s.Width * 0.3, 1.5, 4.0) : 0;
            double wall = Math.Max(2.5, s.Height - roofY);

            // Bangunan rendah: seluruh dinding pakai tekstur bottom. Tinggi: lantai dasar + lapisan jendela.
            double bottom, window;
            if (wall < 8) { bottom = wall; window = 0; }
            else { bottom = 3.5; window = wall - 3.5; }

            // sizeX = timur-barat, sizeZ = utara-selatan (pada heading 0)
            double sx = s.Width, sz = s.Length;

            sb.AppendLine($"  <SceneryObject lat=\"{s.Lat.ToString("F9", ci)}\" lon=\"{s.Lon.ToString("F9", ci)}\" alt=\"0.0M\" altitudeIsAgl=\"TRUE\" pitch=\"0\" bank=\"0\" heading=\"{F(s.HeadingDeg)}\" imageComplexity=\"NORMAL\">");
            sb.AppendLine($"    <GenericBuilding scale=\"1\" bottomTexture=\"{ts.BottomTexture}\" roofTexture=\"{ts.RoofTexture}\" topTexture=\"{ts.TopTexture}\" windowTexture=\"{ts.WindowTexture}\">");

            var rb = new StringBuilder();
            rb.Append($"      <RectangularBuilding roofType=\"{(ridge ? "RIDGE" : "FLAT")}\" sizeX=\"{F(sx)}\" sizeZ=\"{F(sz)}\"");
            rb.Append($" sizeBottomY=\"{F(bottom)}\" textureIndexBottomX=\"{I(sx * WallTexPerM)}\" textureIndexBottomZ=\"{I(sz * WallTexPerM)}\"");
            if (window > 0)
                rb.Append($" sizeWindowY=\"{F(window)}\" textureIndexWindowX=\"{I(sx * WallTexPerM)}\" textureIndexWindowY=\"{I(window * WindowTexYPerM)}\" textureIndexWindowZ=\"{I(sz * WallTexPerM)}\"");
            else
                rb.Append(" sizeWindowY=\"0\" textureIndexWindowX=\"0\" textureIndexWindowY=\"0\" textureIndexWindowZ=\"0\"");
            rb.Append(" sizeTopY=\"0\" textureIndexTopX=\"0\" textureIndexTopZ=\"0\"");
            rb.Append($" textureIndexRoofX=\"{I(sx * RoofTexXPerM)}\" textureIndexRoofZ=\"{I(sz * RoofTexZPerM)}\"");
            if (ridge)
                rb.Append($" sizeRoofY=\"{F(roofY)}\" gableTexture=\"{ts.GableTexture}\" textureIndexGableY=\"256\" textureIndexGableZ=\"{I(sz * WallTexPerM)}\"");
            rb.Append(" />");

            sb.AppendLine(rb.ToString());
            sb.AppendLine("    </GenericBuilding>");
            sb.AppendLine("  </SceneryObject>");
        }
        sb.AppendLine("</FSData>");
        return sb.ToString();
    }
}
