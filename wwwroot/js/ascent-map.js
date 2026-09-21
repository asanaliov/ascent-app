window.AscentMap = (function () {
    // brand colours come from the CSS tokens so the maps never drift from the palette
    const css = getComputedStyle(document.documentElement);
    const GREEN = css.getPropertyValue('--forest').trim() || '#2C5F4A';
    const TERRACOTTA = css.getPropertyValue('--terracotta').trim() || '#2E6E8E';
    const NM_CENTER = [21.7, 41.6];
    const NM_ZOOM = 7;

    const DIFF_COLORS = { Easy: '#3E7C4F', Moderate: '#B8860B', Hard: '#B05E3B', Strenuous: '#7A2E2E' };
    function colorFor(label) { return DIFF_COLORS[label] || GREEN; }

    let routeSeq = 0;

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
            terrain: { source: 'dem', exaggeration: 1.3 },
        };
    }

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
            cooperativeGestures: !!opts.cooperative,
            attributionControl: { compact: true },
            dragRotate: true,
            pitchWithRotate: true,
        });
        if (map.touchZoomRotate) map.touchZoomRotate.enableRotation();
        map.addControl(new maplibregl.NavigationControl({ visualizePitch: true, showCompass: true }), 'top-right');
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

    function normalize(geojson) {
        if (!geojson) return null;
        if (typeof geojson === 'string') {
            try { geojson = JSON.parse(geojson); } catch (e) { return null; }
        }
        if (geojson.type === 'Feature') geojson = geojson.geometry;
        if (!geojson || geojson.type !== 'LineString' || !geojson.coordinates || geojson.coordinates.length < 2) return null;
        return geojson;
    }

    function addRoute(map, geojson, opts) {
        opts = opts || {};
        const g = normalize(geojson);
        if (!g) return null;
        const id = 'route-' + (routeSeq++);
        const width = opts.width || 5;
        map.addSource(id, { type: 'geojson', data: { type: 'Feature', geometry: g, properties: {} } });
        // a light casing under the line keeps it legible on satellite imagery and dark terrain
        map.addLayer({
            id: id + '-casing', type: 'line', source: id,
            layout: { 'line-cap': 'round', 'line-join': 'round' },
            paint: { 'line-color': '#ffffff', 'line-width': width + 4, 'line-opacity': 0.85 },
        });
        map.addLayer({
            id: id + '-line', type: 'line', source: id,
            layout: { 'line-cap': 'round', 'line-join': 'round' },
            paint: { 'line-color': opts.color || GREEN, 'line-width': width, 'line-opacity': 0.95 },
        });
        return g.coordinates;
    }

    function marker(map, lngLat, opts) {
        opts = opts || {};
        let m;
        if (opts.icon) {
            // glass disc with a tabler icon and an always-visible label, e.g. trailhead / summit
            const el = document.createElement('div');
            el.className = 'asc-marker' + (opts.kind ? ' asc-marker-' + opts.kind : '');
            el.innerHTML = '<span class="asc-marker-dot"><i class="ti ' + opts.icon + '"></i></span>'
                + (opts.label ? '<span class="asc-marker-label">' + opts.label + '</span>' : '');
            // opacityWhenCovered: terrain occlusion would otherwise ghost markers down in valleys
            m = new maplibregl.Marker({ element: el, anchor: 'left', offset: [-14, 0], opacityWhenCovered: '1' }).setLngLat(lngLat);
        } else {
            m = new maplibregl.Marker({ color: opts.color || GREEN }).setLngLat(lngLat);
        }
        if (opts.popup) m.setPopup(new maplibregl.Popup({ offset: 24 }).setHTML(opts.popup));
        m.addTo(map);
        return m;
    }

    function setBasemap(map, layerId) {
        ['bm-topo', 'bm-satellite', 'bm-street'].forEach(id =>
            map.setLayoutProperty(id, 'visibility', id === layerId ? 'visible' : 'none'));
        document.querySelectorAll('.asc-basemap button').forEach(b =>
            b.classList.toggle('active', b.dataset.layer === layerId));
    }

    function toRad(d) { return d * Math.PI / 180; }
    function toDeg(r) { return r * 180 / Math.PI; }
    function haversineKm(a, b) {
        const R = 6371, dLat = toRad(b[1] - a[1]), dLon = toRad(b[0] - a[0]);
        const s = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(a[1])) * Math.cos(toRad(b[1])) * Math.sin(dLon / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(s), Math.sqrt(1 - s));
    }
    function lerp(a, b, f) { return a + (b - a) * f; }
    function bearing(a, b) {
        const y = Math.sin(toRad(b[0] - a[0])) * Math.cos(toRad(b[1]));
        const x = Math.cos(toRad(a[1])) * Math.sin(toRad(b[1]))
                - Math.sin(toRad(a[1])) * Math.cos(toRad(b[1])) * Math.cos(toRad(b[0] - a[0]));
        return (toDeg(Math.atan2(y, x)) + 360) % 360;
    }

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
                paint: { 'line-color': opts.progressColor || TERRACOTTA, 'line-width': 7 },
            });
        }

        function headingAt(dist) {
            const ahead = Math.min(total, dist + Math.max(0.04, total * 0.015));
            return bearing(posAt(dist), posAt(ahead));
        }

        let camTimer = null;
        function stopCam() { if (camTimer) { clearInterval(camTimer); camTimer = null; } }

        function start(mode) {
            if (running) return;
            ensureLayer();
            running = true;
            const follow = !!(mode && mode.follow);
            const speed = (mode && mode.speed) || 1;
            marker.setLngLat(coords[0]).addTo(map);
            const dur = (opts.duration || (follow ? 22000 : 14000)) / speed;
            const t0 = performance.now();

            if (follow) {
                map.easeTo({ center: posAt(0), bearing: headingAt(0), pitch: 70, zoom: 15.2, duration: 900 });
                const tick = Math.max(220, 600 / speed);
                camTimer = setInterval(() => {
                    if (!running) return;
                    const k = Math.min(1, (performance.now() - t0) / dur);
                    const dist = k * total;
                    map.easeTo({
                        center: posAt(dist), bearing: headingAt(dist),
                        pitch: 70, zoom: 15.2, duration: tick + 60, easing: t => t,
                    });
                }, tick);
            }

            function frame(now) {
                if (!running) return;
                const k = Math.min(1, (now - t0) / dur);
                const dist = k * total;
                marker.setLngLat(posAt(dist));
                map.getSource(PROG).setData(progressLine(dist));
                if (k < 1) raf = requestAnimationFrame(frame);
                else { stopCam(); running = false; if (opts.onEnd) opts.onEnd(); }
            }
            raf = requestAnimationFrame(frame);
        }
        function stop() {
            running = false;
            stopCam();
            if (raf) cancelAnimationFrame(raf);
            marker.remove();
            if (map.getSource(PROG)) map.getSource(PROG).setData(progressLine(0));
        }
        return { start, stop, isRunning: () => running };
    }

    function fit(map, coords, opts) {
        opts = opts || {};
        if (!coords || !coords.length) { map.jumpTo({ center: NM_CENTER, zoom: NM_ZOOM }); return; }
        const b = new maplibregl.LngLatBounds(coords[0], coords[0]);
        coords.forEach(c => b.extend(c));
        const pad = opts.padding || 40;
        map.fitBounds(b, { padding: typeof pad === 'number' ? { top: pad + 50, bottom: pad, left: pad, right: pad } : pad, maxZoom: opts.maxZoom || 15, animate: false });
        if (opts.pitch != null) map.setPitch(opts.pitch);
        if (opts.bearing != null) map.setBearing(opts.bearing);
    }

    return { GREEN, TERRACOTTA, NM_CENTER, NM_ZOOM, colorFor, display, onReady, normalize, addRoute, marker, fit, setBasemap, simulate };
})();
