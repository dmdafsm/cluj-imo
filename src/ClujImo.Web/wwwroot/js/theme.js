// Material theme from a single seed color, plus a persisted light/dark toggle.
const SEED = "#1f6f5c";
const KEY = "cluj-imo-mode";

function storedMode() {
  try { return localStorage.getItem(KEY); } catch { return null; }
}

function applyMode(mode) {
  ui("mode", mode);
  const icon = document.querySelector("#theme-toggle i");
  if (icon) icon.textContent = ui("mode") === "dark" ? "light_mode" : "dark_mode";
  document.dispatchEvent(new CustomEvent("theme:changed"));
}

await ui("theme", SEED);
applyMode(storedMode() ?? "auto");

document.getElementById("theme-toggle")?.addEventListener("click", () => {
  const next = ui("mode") === "dark" ? "light" : "dark";
  try { localStorage.setItem(KEY, next); } catch { /* private mode */ }
  applyMode(next);
});
