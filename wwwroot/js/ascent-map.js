// Shared MapLibre GL helpers for every trail map (overview, details, nearby, editor).
// Gives each map: Street / Terrain / Satellite basemaps + real 3D terrain (free AWS
// terrarium DEM, no API key), navigation/terrain/scale controls, and route drawing.
// Routes are GeoJSON LineStrings ([lng, lat]) — the same order MapLibre uses.
window.AscentMap = (function () {
    const GREEN = '#2F4A2C';
    const TERRACOTTA = '#B05E3B';
    const NM_CENTER = [21.7, 41.6]; // North Macedonia [lng, lat]
    const NM_ZOOM = 7;

    const DIFF_COLORS = { Easy: '#3E7C4F', Moderate: '#B8860B', Hard: '#B05E3B', Strenuous: '#7A2E2E' };
    function colorFor(label) { return DIFF_COLORS[label] || GREEN; }

    let routeSeq = 0;

    // raster basemaps + a raster-dem source that powers the 3D terrain
    function baseStyle() {
        return {
            version: 8,
            sources: {
                topo: {
                    type: 'raster', tileSize: 256, maxzoom: 17,
                    tiles: ['https://a.tile.opentopomap.org/{z}/{x}/{y}.png',
                            'https://b.tile.opentopomap.org/{z}/{x}/{y}.png',
                            'https://c.tile.opentopomap.org/{z}/{x}/{y}.png'],
                    attribution: '© OpenTopoMap (CC-BY-SA), © OpenStreetMap contributors',
                },
                satellite: {
                    type: 'raster', tileSize: 256, maxzoom: 19,
                    tiles: ['https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'],
                    attribution: '© Esri, Maxar, Earthstar Geographics',
                },
                street: {
                    type: 'raster', tileSize: 256, maxzoom: 19,
                    tiles: ['https://a.tile.openstreetmap.org/{z}/{x}/{y}.png',
                            'https://b.tile.openstreetmap.org/{z}/{x}/{y}.png',
                            'https://c.tile.openstreetmap.org/{z}/{x}/{y}.png'],
                    attribution: '© OpenStreetMap contributors',
                },
                dem: {
                    type: 'raster-dem', tileSize: 256, maxzoom: 15, encoding: 'terrarium',
                    tiles: ['https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png'],
                    attribution: '© Mapzen / AWS Terrain Tiles',
                },
            },
            layers: [
                { id: 'bm-topo', type: 'raster', source: 'topo', layout: { visibility: 'visible' } },
                { id: 'bm-satellite', type: 'raster', source: 'satellite', layout: { visibility: 'none' } },
                { id: 'bm-street', type: 'raster', source: 'street', layout: { visibility: 'none' } },
            ],
            terrain: { source: 'dem', exaggeration: 1.3 }, // the 3D
        };
    }

    // a little Street/Terrain/Satellite toggle, rendered as a MapLibre control
    function basemapControl(options) {
        return {
            onAdd(map) {
                const box = document.createElement('div');
                box.className = 'maplibregl-ctrl maplibregl-ctrl-group asc-basemap';
                options.forEach((opt, i) => {
                    const b = document.createElement('button');
                    b.type = 'button';
                    b.textContent = opt.label;
                    b.dataset.layer = opt.id;
                    if (i === 0) b.classList.add('active');
                    b.addEventListener('click', () => {
                        options.forEach(o => map.setLayoutProperty(o.id, 'visibility', o === opt ? 'visible' : 'none'));
                        box.querySelectorAll('button').forEach(x => x.classList.remove('active'));
                        b.classList.add('active');
                    });
                    box.appendChild(b);
                });
                this._box = box;
                return box;
            },
            onRemove() { this._box.remove(); },
        };
    }

    // build a fully-loaded interactive map with all the controls
    function display(elId, opts) {
        opts = opts || {};
        const map = new maplibregl.Map({
            container: elId,
            style: baseStyle(),
            center: opts.center || NM_CENTER,
            zoom: opts.zoom != null ? opts.zoom : NM_ZOOM,
            pitch: opts.pitch != null ? opts.pitch : 0,
            bearing: opts.bearing || 0,
            maxPitch: 85,
            cooperativeGestures: !!opts.cooperative, // stops the page-scroll hijack
            attributionControl: { compact: true },
        });
        map.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), 'top-right');
        map.addControl(new maplibregl.TerrainControl({ source: 'dem', exaggeration: 1.3 }), 'top-right');
        map.addControl(new maplibregl.ScaleControl({ unit: 'metric' }), 'bottom-left');
        map.addControl(basemapControl([
            { id: 'bm-topo', label: 'Terrain' },
            { id: 'bm-satellite', label: 'Satellite' },
            { id: 'bm-street', label: 'Street' },
        ]), 'top-left');
        return map;
    }

    function onReady(map, cb) {
        if (map.isStyleLoaded()) cb();
        else map.on('load', cb);
    }

    // accept a GeoJSON LineString (object/string/Feature) -> geometry or null
    function normalize(geojson) {
        if (!geojson) return null;
        if (typeof geojson === 'string') {
            try { geojson = JSON.parse(geojson); } catch (e) { return null; }
        }
        if (geojson.type === 'Feature') geojson = geojson.geometry;
        if (!geojson || geojson.type !== 'LineString' || !geojson.coordinates || geojson.coordinates.length < 2) return null;
        return geojson;
    }

    // draw a route line; returns its [lng,lat] coords (or null). Call after onReady.
    function addRoute(map, geojson, opts) {
        opts = opts || {};
        const g = normalize(geojson);
        if (!g) return null;
        const id = 'route-' + (routeSeq++);
        map.addSource(id, { type: 'geojson', data: { type: 'Feature', geometry: g, properties: {} } });
        map.addLayer({
            id: id + '-line', type: 'line', source: id,
            layout: { 'line-cap': 'round', 'line-join': 'round' },
            paint: { 'line-color': opts.color || GREEN, 'line-width': opts.width || 5, 'line-opacity': 0.9 },
        });
        return g.coordinates;
    }

    function marker(map, lngLat, opts) {
        opts = opts || {};
        const m = new maplibregl.Marker({ color: opts.color || GREEN }).setLngLat(lngLat);
        if (opts.popup) m.setPopup(new maplibregl.Popup({ offset: 24 }).setHTML(opts.popup));
        m.addTo(map);
        return m;
    }

    // programmatically switch basemap (used by the route simulation) + sync the toggle
    function setBasemap(map, layerId) {
        ['bm-topo', 'bm-satellite', 'bm-street'].forEach(id =>
            map.setLayoutProperty(id, 'visibility', id === layerId ? 'visible' : 'none'));
        document.querySelectorAll('.asc-basemap button').forEach(b =>
            b.classList.toggle('active', b.dataset.layer === layerId));
    }

    // --- geo helpers for the simulation ---
    function toRad(d) { return d * Math.PI / 180; }
    function haversineKm(a, b) {
        const R = 6371, dLat = toRad(b[1] - a[1]), dLon = toRad(b[0] - a[0]);
        const s = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(a[1])) * Math.cos(toRad(b[1])) * Math.sin(dLon / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(s), Math.sqrt(1 - s));
    }
    function lerp(a, b, f) { return a + (b - a) * f; }

    // Fly a marker along the route with a chase camera, drawing the traveled portion.
    // Returns { start, stop, isRunning }. Coarse routes are interpolated for smoothness.
    function simulate(map, coords, opts) {
        opts = opts || {};
        const segs = [];
        let total = 0;
        for (let i = 1; i < coords.length; i++) {
            const d = haversineKm(coords[i - 1], coords[i]) || 0;
            segs.push({ a: coords[i - 1], b: coords[i], d: d, start: total });
            total += d;
        }
        if (total === 0) total = 1;

        const PROG = 'sim-progress';
        // a clearly-visible moving dot (custom element, not the default pin)
        const dot = document.createElement('div');
        dot.className = 'asc-sim-dot';
        const marker = new maplibregl.Marker({ element: dot });
        let raf = null, running = false;

        function posAt(dist) {
            for (const s of segs) {
                if (dist <= s.start + s.d) {
                    const f = s.d ? (dist - s.start) / s.d : 0;
                    return [lerp(s.a[0], s.b[0], f), lerp(s.a[1], s.b[1], f)];
                }
            }
            return segs[segs.length - 1].b;
        }
        function progressLine(dist) {
            const pts = [coords[0]];
            for (const s of segs) {
                if (dist >= s.start + s.d) { pts.push(s.b); }
                else {
                    const f = s.d ? Math.max(0, (dist - s.start) / s.d) : 0;
                    pts.push([lerp(s.a[0], s.b[0], f), lerp(s.a[1], s.b[1], f)]);
                    break;
                }
            }
            return { type: 'Feature', properties: {}, geometry: { type: 'LineString', coordinates: pts } };
        }
        function ensureLayer() {
            if (map.getSource(PROG)) return;
            map.addSource(PROG, { type: 'geojson', data: progressLine(0) });
            map.addLayer({
                id: PROG + '-l', type: 'line', source: PROG,
                layout: { 'line-cap': 'round', 'line-join': 'round' },
                paint: { 'line-color': opts.progressColor || TERRACOTTA, 'line-width': 6 },
            });
        }

        function start() {
            if (running) return;
            ensureLayer();
            running = true;
            marker.setLngLat(coords[0]).addTo(map);
            const dur = opts.duration || 14000;
            const t0 = performance.now();
            // The camera stays framed on the whole route (set by the caller); we just
            // animate the dot + the growing traveled line, so the motion reads clearly.
            function frame(now) {
                if (!running) return;
                const k = Math.min(1, (now - t0) / dur);
                const dist = k * total;
                marker.setLngLat(posAt(dist));
                map.getSource(PROG).setData(progressLine(dist));
                if (k < 1) raf = requestAnimationFrame(frame);
                else { running = false; if (opts.onEnd) opts.onEnd(); }
            }
            raf = requestAnimationFrame(frame);
        }
        function stop() {
            running = false;
            if (raf) cancelAnimationFrame(raf);
            marker.remove();
            if (map.getSource(PROG)) map.getSource(PROG).setData(progressLine(0));
        }
        return { start, stop, isRunning: () => running };
    }

    // frame the map around a set of [lng,lat] points, keeping a 3D pitch if asked
    function fit(map, coords, opts) {
        opts = opts || {};
        if (!coords || !coords.length) { map.jumpTo({ center: NM_CENTER, zoom: NM_ZOOM }); return; }
        const b = new maplibregl.LngLatBounds(coords[0], coords[0]);
        coords.forEach(c => b.extend(c));
        map.fitBounds(b, { padding: opts.padding || 40, maxZoom: opts.maxZoom || 15, animate: false });
        if (opts.pitch != null) map.setPitch(opts.pitch);
        if (opts.bearing != null) map.setBearing(opts.bearing);
    }

    return { GREEN, TERRACOTTA, NM_CENTER, NM_ZOOM, colorFor, display, onReady, normalize, addRoute, marker, fit, setBasemap, simulate };
})();
