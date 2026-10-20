namespace P3DAutoBuild;

public record GeoPoint(double Lat, double Lon);

public record Building(long Id, List<GeoPoint> Footprint, Dictionary<string, string> Tags);

public enum RoofType { Flat, Ridge }

// Hasil analisis satu bangunan (ukuran dalam meter)
public record BuildingSpec(
    long Id, double Lat, double Lon,
    double Width, double Length, double Height,
    double HeadingDeg, RoofType Roof);

// Entri katalog model library P3D milik kamu (lihat models.json)
public class ModelEntry
{
    public string Guid { get; set; } = "";
    public string Roof { get; set; } = "Flat";   // Flat / Ridge
    public double Width { get; set; }            // ukuran asli model (m)
    public double Length { get; set; }
    public double Height { get; set; }
}
