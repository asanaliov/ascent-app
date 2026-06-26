window.AscentRouteEditor = function (opts) {
    const input = document.getElementById(opts.inputId);
    const map = AscentMap.display(opts.mapId, { pitch: 0 });
    let pts = [];
    let startMarker = null;
    let ready = false;

    const existing = AscentMap.normalize(input.value);
    if (existing) pts = existing.coordinates.slice();

    function sync() {
        if (!ready) return;
        map.getSource('draw-line').setData({
            type: 'Feature', properties: {},
            geometry: { type: 'LineString', coordinates: pts.length >= 2 ? pts : [] },
        });
        map.getSource('draw-pts').setData({
            type: 'FeatureCollection',
            features: pts.map((p, i) => ({ type: 'Feature', properties: { i }, geometry: { type: 'Point', coordinates: p } })),
        });

        if (pts.length) {
            if (!startMarker) startMarker = new maplibregl.Marker({ color: AscentMap.GREEN }).setLngLat(pts[0]).addTo(map);
            else startMarker.setLngLat(pts[0]);
        } else if (startMarker) {
            startMarker.remove();
            startMarker = null;
        }

        input.value = pts.length >= 2 ? JSON.stringify({ type: 'LineString', coordinates: pts }) : '';
        if (opts.countId) document.getElementById(opts.countId).textContent = pts.length;
    }

    AscentMap.onReady(map, () => {
        map.addSource('draw-line', { type: 'geojson', data: { type: 'Feature', properties: {}, geometry: { type: 'LineString', coordinates: [] } } });
        map.addLayer({
            id: 'draw-line-lyr', type: 'line', source: 'draw-line',
            layout: { 'line-cap': 'round', 'line-join': 'round' },
            paint: { 'line-color': AscentMap.GREEN, 'line-width': 4 },
        });
        map.addSource('draw-pts', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });
        map.addLayer({
            id: 'draw-pts-lyr', type: 'circle', source: 'draw-pts',
            paint: { 'circle-radius': 4, 'circle-color': '#ffffff', 'circle-stroke-color': AscentMap.GREEN, 'circle-stroke-width': 2 },
        });

        map.getCanvas().style.cursor = 'crosshair';
        map.on('click', e => { pts.push([e.lngLat.lng, e.lngLat.lat]); sync(); });

        ready = true;
        if (pts.length) AscentMap.fit(map, pts, { padding: 50 });
        sync();
    });

    if (opts.undoId) document.getElementById(opts.undoId).addEventListener('click', () => { pts.pop(); sync(); });
    if (opts.clearId) document.getElementById(opts.clearId).addEventListener('click', () => { pts = []; sync(); });
};
