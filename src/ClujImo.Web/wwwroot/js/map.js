// Leaflet map fed by the JSON that the htmx partial (#map-points) carries; redrawn after every filter swap.
(function () {
  const CLUJ = [46.7712, 23.6236];
  const map = L.map("map", { zoomControl: true }).setView(CLUJ, 13);
  L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  }).addTo(map);

  const layer = L.layerGroup().addTo(map);
  const css = (name) => getComputedStyle(document.body).getPropertyValue(name).trim();
  const escape = (text) => String(text ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);

  function listUrl(street) {
    const params = new URLSearchParams(location.search);
    params.set("q", street);
    return `/autorizatii?${params}`;
  }

  function draw() {
    const source = document.getElementById("map-points");
    if (!source) return;
    const points = JSON.parse(source.textContent || "[]");
    const primary = css("--primary"), outline = css("--outline"), surface = css("--surface");
    layer.clearLayers();
    const max = Math.max(1, ...points.map((p) => p.count));

    for (const p of points) {
      const isNew = p.newBuilds > 0;
      L.circleMarker([p.lat, p.lng], {
        radius: 4 + 20 * Math.sqrt(p.count / max),
        color: surface,
        weight: 1,
        fillColor: isNew ? primary : outline,
        fillOpacity: isNew ? 0.75 : 0.4,
      })
        .bindPopup(
          `<strong>${escape(p.street)}</strong><br>` +
          (p.neighborhood ? `${escape(p.neighborhood)}<br>` : "") +
          `${p.count} documente · ${p.newBuilds} construcții noi` +
          (p.units > 0 ? ` · ${p.units} unități` : "") +
          `<br><span>Predominant: ${escape(p.category)}</span><br>` +
          `<a href="${listUrl(p.street)}">Vezi lista</a>`)
        .addTo(layer);
    }
  }

  draw();
  document.body.addEventListener("htmx:afterSettle", (e) => { if (e.detail.target.id === "map-data") draw(); });
  document.addEventListener("theme:changed", draw);
})();
