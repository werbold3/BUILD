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
