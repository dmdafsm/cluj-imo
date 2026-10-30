// Leaflet map. Permit clusters come with every htmx swap (#map-points); plans and OSM sites once per page (#map-layers).
(function () {
  const CLUJ = [46.7712, 23.6236];
  const map = L.map("map", { preferCanvas: true }).setView(CLUJ, 13);
  L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  }).addTo(map);

  const layers = { permits: L.layerGroup().addTo(map), plans: L.layerGroup().addTo(map), sites: L.layerGroup() };
  const css = (name) => getComputedStyle(document.body).getPropertyValue(name).trim();
  const escape = (text) => String(text ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
  const read = (id, fallback) => { const el = document.getElementById(id); return el ? JSON.parse(el.textContent || "null") ?? fallback : fallback; };

  function drawPermits() {
    const points = read("map-points", []);
    const primary = css("--primary"), outline = css("--outline"), surface = css("--surface");
    const max = Math.max(1, ...points.map((p) => p.count));
    layers.permits.clearLayers();
    for (const p of points) {
      const strong = p.newBuilds > 0;
      const items = p.items.map((i) => `<li><a href="/autorizatii/${i.id}">${escape(i.title)}</a> · ${escape(i.category)}</li>`).join("");
      // New buildings stand out; everything else stays in the background.
      L.circleMarker([p.lat, p.lng], {
        radius: (strong ? 5 : 3) + 14 * Math.sqrt(p.count / max),
        color: surface,
        weight: strong ? 1 : 0,
        fillColor: strong ? primary : outline,
        fillOpacity: strong ? (p.exact ? 0.9 : 0.55) : 0.3,
      })
        .bindPopup(
          `<strong>${escape(p.label)}</strong><br>` +
          `${p.count} documente · ${p.newBuilds} construcții noi${p.units > 0 ? ` · ${p.units} unități` : ""}<br>` +
          `<span>Poziție: ${escape(p.precision)}</span><ul>${items}</ul>` +
          (p.count > p.items.length ? `<a href="${listUrl(p.label)}">Toate (${p.count})</a>` : ""))
        .addTo(layers.permits);
    }
  }

  function drawStatic() {
    const data = read("map-layers", {});
    const tertiary = css("--tertiary"), secondary = css("--secondary");
    layers.plans.clearLayers();
    layers.sites.clearLayers();
    for (const p of data.plans ?? []) {
      L.circleMarker([p.lat, p.lng], { radius: 9, color: tertiary, weight: 2.5, fill: false })
        .bindPopup(`<strong>${escape(p.kind)} · ${escape(p.stage)}</strong><br>${escape(p.date)}<br>${escape(p.title)}` +
          (p.url ? `<br><a href="${escape(p.url)}" target="_blank" rel="noopener">Sursa</a>` : ""))
        .addTo(layers.plans);
    }
    for (const s of data.sites ?? []) {
      L.marker([s.lat, s.lng], {
        icon: L.divIcon({ className: "", html: `<span style="display:block;width:10px;height:10px;border-radius:2px;background:${secondary}"></span>`, iconSize: [10, 10] }),
      })
        .bindPopup(`<strong>Șantier (OpenStreetMap)</strong>${s.name ? `<br>${escape(s.name)}` : ""}${s.building ? `<br>${escape(s.building)}` : ""}`)
        .addTo(layers.sites);
    }
  }

  function listUrl(label) {
    const params = new URLSearchParams(location.search);
    params.set("q", label.replace(/\s+\S*\d\S*$/, ""));
    return `/autorizatii?${params}`;
  }

  document.querySelectorAll("[data-layer]").forEach((box) => {
    const layer = layers[box.dataset.layer];
    if (box.checked && !map.hasLayer(layer)) layer.addTo(map);
    box.addEventListener("change", () => (box.checked ? layer.addTo(map) : map.removeLayer(layer)));
  });

  drawPermits();
  drawStatic();
  document.body.addEventListener("htmx:afterSettle", (e) => { if (e.detail.target.id === "map-data") drawPermits(); });
  document.addEventListener("theme:changed", () => { drawPermits(); drawStatic(); });
})();
