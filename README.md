# P3DAutoBuild

Generator bangunan otomatis dari data OpenStreetMap untuk Prepar3D v4.5.

## Mode generic (default, tanpa model buatan sendiri)
1. `P3DAutoBuild.exe --bbox south,west,north,east --out buildings.xml`
2. `bglcomp.exe buildings.xml` (P3D SDK > Environment Kit > BGL Compiler SDK)
3. Taruh `.bgl` di folder scenery add-on, daftarkan lewat Scenery Library.

Ukuran (panjang, lebar, tinggi), arah, dan tipe atap (FLAT/RIDGE) dihitung per bangunan,
jadi tidak ada masalah scale uniform seperti library object.

## textures.json
ID tekstur diambil dari "Generic Building Textures.xls" di BGL Compiler SDK.
Tiga set bawaan hanyalah kombinasi umum, ganti sesuai tampilan yang kamu mau.
Konstanta tiling ada di atas GenericEmitter.cs (kalau tekstur terlihat melar).

## Mode library (opsional)
`--mode library --models models.json` memakai model MDL buatanmu sendiri.

## Batasan
- Hanya bentuk kotak (footprint L/U jadi kotak). Tidak ada warna/ornamen khusus.
- Generic Building wajib menempel tanah (alt 0 AGL), sudah ditangani.
- Data OSM: ODbL, cantumkan atribusi OpenStreetMap contributors.

## Kalau koneksi ke Overpass gagal (SSL / HandshakeFailure)
Tool otomatis mencoba 4 server. Kalau semuanya gagal (biasanya diblokir ISP/DNS/antivirus):
1. Buka di browser (bbox: south,west,north,east):
   `https://overpass-api.de/api/interpreter?data=[out:json][timeout:90];way["building"](-3.99,122.50,-3.97,122.52);out geom;`
   Kalau browser juga gagal, ganti host dengan `overpass.kumi.systems`, atau nyalakan VPN / ganti DNS ke 1.1.1.1.
2. Simpan hasilnya sebagai `osm.json` (Ctrl+S).
3. Jalankan: `P3DAutoBuild --osm-file osm.json --out buildings.xml`

## Pakai file OSM (.osm) hasil download
Format yang didukung: `.osm`, `.xml`, `.osm.gz` (OSM XML) dan `.json` (Overpass).
- Via browser (bbox = west,south,east,north):
  `https://api.openstreetmap.org/api/0.6/map?bbox=122.50,-3.99,122.52,-3.97`
  Simpan sebagai `map.osm`. Batas API: area kecil (maks ~50 ribu node). Kalau error "too many nodes", kecilkan area.
- Atau pakai openstreetmap.org > Export > Manually select area > Export.
- Area besar: extract.bbbike.org (pilih format OSM XML gzip), lalu pakai file `.osm.gz` langsung.
Jalankan: `dotnet run -- --osm-file map.osm --out buildings.xml`
