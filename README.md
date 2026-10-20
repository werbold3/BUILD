# P3DAutoBuild

Generator bangunan otomatis (ala autogen buildings MSFS 2020) untuk Prepar3D v4.5.

## Alur
1. Push ke GitHub, Actions akan build `P3DAutoBuild.exe` (download dari tab Actions > Artifacts).
2. Edit `models.json`: isi GUID library object bangunan milikmu + ukuran aslinya (meter).
3. Jalankan: `P3DAutoBuild.exe --bbox south,west,north,east --out buildings.xml`
4. Kompilasi dengan `bglcomp.exe buildings.xml` (dari P3D SDK > Environment Kit > BGL Compiler SDK).
5. Taruh `.bgl` di folder scenery add-on, daftarkan lewat Scenery Library / add-on.xml.

## Catatan
- Orientasi model: sisi panjang model dianggap searah utara pada heading 0.
- Tinggi/atap diperkirakan dari tag OSM + heuristik; ganti `Analyzer.cs` dengan model ML bila mau.
- Scale P3D itu uniform, jadi sediakan beberapa model dengan ukuran berbeda agar hasil lebih pas.
- Data OSM: ODbL, cantumkan atribusi OpenStreetMap contributors.
