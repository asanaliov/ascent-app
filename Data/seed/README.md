# Trail seed editorial notes

`macedonia-trails.json` uses stable `seedKey` values for database identity.
Display names may be corrected without changing those keys. Old display names
belong in `legacyNames` so an existing seeded row is renamed instead of
duplicated.

Trail names follow the pattern `trailhead – destination` where an established
route is available. Macedonian place names use Latin transliteration with
diacritics, for example `Šar`, `Galičica`, `Kožuf`, `Kruševo`, and `Čeples`.

Descriptions and headline route figures were reviewed against these sources:

- [National Park Galičica trail catalog](https://galicica.org.mk/en/trails/our-suggestion/)
- [National Park Pelister: Bolnici route to Pelister Peak](https://park-pelister.com/en/activity/%D0%BF%D0%B0%D1%82%D0%B5%D0%BA%D0%B0-%D0%BF%D0%BE-%D0%BA%D0%B0%D0%BC%D0%B5%D1%9A%D0%B0%D1%80/)
- [National Park Mavrovo hiking information](https://npmavrovo.org.mk/en/%D0%BF%D0%BB%D0%B0%D0%BD%D0%B8%D1%80%D0%B0%D1%9A%D0%B5-%D0%B8-%D0%BF%D0%BB%D0%B0%D0%BD%D0%B8%D0%BD%D1%81%D0%BA%D0%B8-%D0%B2%D0%B5%D0%BB%D0%BE%D1%81%D0%B8%D0%BF%D0%B5%D0%B4%D0%B8%D0%B7%D0%B0%D0%BC/)
- [Discover Šar Mountain study](https://sharmountain.com/images/dokumenti/03%20Study-Discover%20Shar%20Mountain%20eng_design.pdf)
- [High Scardus Trail](https://www.high-scardus-trail.com/)
- [North Macedonia Timeless: Zelen Breg](https://macedonia-timeless.com/eng/things_to_do/senses/sight/peaks/zelen-breg/)
- [North Macedonia Timeless: Smolare Waterfall](https://macedonia-timeless.com/eng/things_to_do/senses/sight/waterfalls/smolare-waterfall/)
- [Visit Berovo: Berovo Lake – Dvorište – Bela Voda](https://visitberovo.mk/en/aktivnosti/berovsko-ezero-dvorishte-bela-voda)
- [Cultural Heritage Protection Office: Kokino](https://uzkn.gov.mk/mk/wp-content/uploads/2023/02/KOKINO-ENG.pdf)
- [High Scardus route: Staro Selo – Ljuboten hut](https://www.outdooractive.com/en/route/hiking-trail/north-macedonia/high-scardus-trail-stage-01-staro-selo-mountain-hut-ljuboten/65666968/)
- [Pella Trails: Prilep – Treskavec Monastery](https://www.outdooractive.com/en/route/hiking-trail/north-macedonia/prilep-treskavec-monastery/809205012/)

Route geometry in this demo catalog is illustrative, not a navigation-grade
GPX track. Geometry is `null` where the previous line did not match the
reviewed route. Users should obtain current official maps, check park and
border rules, and verify trail conditions before hiking.
