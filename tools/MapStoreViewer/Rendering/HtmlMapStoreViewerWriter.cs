//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using System.Text.Encodings.Web;
using System.Text.Json;
using MapStoreViewer.Scene;

namespace MapStoreViewer.Rendering;

/**
  * Writes self-contained HTML inspection pages for extracted mapstore .bin data.
  */
public static class HtmlMapStoreViewerWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static void WriteTile(string outputPath, MapTileScene scene)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(scene);
        EnsureParentDirectory(outputPath);

        string json = JsonSerializer.Serialize(scene, JsonOptions);
        string title = HtmlEncoder.Default.Encode($"Map {scene.TileKey} Preview");
        string html = TileTemplate.Replace("__TITLE__", title, StringComparison.Ordinal).Replace("__DATA__", json, StringComparison.Ordinal);
        File.WriteAllText(outputPath, html);
    }

    public static void WriteOverview(string outputPath, MapOverviewScene scene)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(scene);
        EnsureParentDirectory(outputPath);

        string json = JsonSerializer.Serialize(scene, JsonOptions);
        string title = HtmlEncoder.Default.Encode($"Map {scene.MapKey} Overview");
        string html = OverviewTemplate.Replace("__TITLE__", title, StringComparison.Ordinal).Replace("__DATA__", json, StringComparison.Ordinal);
        File.WriteAllText(outputPath, html);
    }

    private static void EnsureParentDirectory(string outputPath)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private const string OverviewTemplate = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>__TITLE__</title>
<style>
:root{color-scheme:dark;background:#101010;color:#ddd;font-family:system-ui,Segoe UI,sans-serif}body{margin:0;padding:16px}h1{margin:0 0 12px}.panel{background:#1b1b1b;border:1px solid #333;border-radius:10px;padding:16px;margin-bottom:16px}.toolbar{display:flex;gap:12px;flex-wrap:wrap;align-items:center}.toolbar label{font-size:13px;color:#ccc}.toolbar select,.toolbar input,.toolbar button{background:#222;color:#ddd;border:1px solid #444;border-radius:6px;padding:5px 8px}.toolbar button{cursor:pointer}.toolbar button:hover{background:#2d2d2d}.zoomLabel{display:inline-block;min-width:54px;text-align:center;color:#ccc;font-size:13px}.previewWrap{overflow:auto;background:#0b0b0b;border:1px solid #333;border-radius:10px;padding:12px;max-height:calc(100vh - 310px);min-height:360px}.previewCanvas{display:block;image-rendering:auto;background:#080808}.legend{display:flex;gap:18px;flex-wrap:wrap}.swatch{display:inline-block;width:14px;height:14px;border-radius:2px;margin-right:6px;vertical-align:middle}.table{border-collapse:collapse;width:100%;font-size:13px}.table th,.table td{border-bottom:1px solid #333;padding:6px 8px;text-align:left}.muted{color:#aaa}.messages{white-space:pre-wrap;color:#ccc}.status{font-size:13px;color:#ccc;margin-top:8px}.coordPanel{display:grid;grid-template-columns:1fr;gap:6px}.coordLine{font-size:13px;color:#ccc}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,Liberation Mono,monospace}.copyBtn{background:#222;color:#ddd;border:1px solid #444;border-radius:6px;padding:4px 7px;cursor:pointer}.copyBtn:disabled{opacity:.45;cursor:not-allowed}.pill{display:inline-block;padding:2px 7px;border-radius:999px;background:#2a2a2a;border:1px solid #444;margin-right:6px}.hint{font-size:13px;color:#aaa;margin-top:8px}.split{display:grid;grid-template-columns:minmax(320px,1fr);gap:16px}@media (min-width:1300px){.split{grid-template-columns:minmax(760px,1.15fr) minmax(400px,.85fr)}}@media (max-height:760px){.previewWrap{max-height:calc(100vh - 245px);min-height:280px}}</style>
</head>
<body>
<h1 id="title"></h1>
<div class="panel">
  <div class="toolbar">
    <label>View mode <select id="mode">
      <option value="terrainLiquid">Terrain + liquid</option>
      <option value="terrain">Terrain only</option>
      <option value="liquid">Liquid only</option>
      <option value="holes">Terrain holes</option>
      <option value="coverage">Tile coverage</option>
      <option value="collision">Collision density</option>
      <option value="navmesh">Navmesh presence</option>
      <option value="dataSize">Data size heatmap</option>
      <option value="health">Extraction health</option>
    </select></label>
    <label>Terrain model <select id="palette">
      <option value="natural">Natural relief</option>
      <option value="height">Height ramp</option>
      <option value="grayscale">Grayscale</option>
      <option value="debug">Debug contrast</option>
    </select></label>
    <label><input id="showGrid" type="checkbox" checked> Tile grid</label>
    <label><input id="smooth" type="checkbox" checked> Smooth preview</label>
    <button id="zoomOut" type="button" title="Zoom out">−</button>
    <button id="zoomIn" type="button" title="Zoom in">+</button>
    <button id="zoomFit" type="button" title="Fit preview to viewport">Fit</button>
    <button id="zoomActual" type="button" title="Show one preview pixel as one screen pixel">100%</button>
    <span id="zoomLabel" class="zoomLabel">Fit</span>
  </div>
  <div class="hint">The preview is generated only from the extracted .bin files. Use the modes above to inspect terrain height, liquid coverage, holes, collision/navmesh presence, and extraction health. Use the zoom buttons or Ctrl+mouse wheel over the preview to inspect details.</div>
</div>
<div class="split">
  <div class="panel">
    <h2>2D map preview</h2>
    <div class="previewWrap"><canvas id="preview" class="previewCanvas"></canvas></div>
    <div id="hover" class="status">Move over the preview to inspect a tile.</div>
  </div>
  <div>
    <div class="panel"><h2>Legend</h2><div id="legend" class="legend"></div></div>
    <div class="panel">
      <h2>Hover coordinates</h2>
      <div class="coordPanel">
        <div class="coordLine">Tile: <span id="overviewCoordTile" class="mono">—</span></div>
        <div class="coordLine">Local: <span id="overviewCoordLocal" class="mono">—</span></div>
        <div class="coordLine">XYZ: <span id="overviewCoordXyz" class="mono">—</span></div>
        <div class="coordLine">Command: <span id="overviewCoordCommand" class="mono">—</span></div>
        <button id="copyOverviewGo" class="copyBtn" type="button" disabled>Copy estimated .go xyz</button>
      </div>
      <div class="hint">Overview coordinates are estimated from the sampled 2D preview. Use the 3D tile/detail page for exact terrain-pick coordinates.</div>
    </div>
  </div>
</div>
<div class="panel"><h2>Messages</h2><div id="messages" class="messages"></div></div>
<div class="panel"><h2>Tiles</h2><table class="table"><thead><tr><th>Tile</th><th>Preview</th><th>Flags</th><th>Terrain</th><th>Liquid</th><th>Collision</th><th>Navmesh</th></tr></thead><tbody id="tiles"></tbody></table></div>
<script>
const sceneData = __DATA__;
const mapSize = 64;
const previewResolution = Math.max(4, Math.min(64, sceneData.previewResolution || 16));
const canvas = document.getElementById('preview');
const ctx = canvas.getContext('2d');
const previewWrap = document.querySelector('.previewWrap');
canvas.width = mapSize * previewResolution;
canvas.height = mapSize * previewResolution;
let previewZoom = 1;
let previewFitMode = true;
const tileMap = new Map((sceneData.tiles || []).map(t => [`${t.tileX}_${t.tileY}`, t]));
const terrainRange = getTerrainRange();
const maxTotalBytes = Math.max(1, ...((sceneData.tiles || []).map(totalBytes)));
const maxCollisionBytes = Math.max(1, ...((sceneData.tiles || []).map(t => t.collisionBytes || 0)));
const maxNavmeshBytes = Math.max(1, ...((sceneData.tiles || []).map(t => t.navmeshBytes || 0)));
const TILE_SIZE = 533.333333;
const MAP_GRID_COUNT = 64;
const MAP_HALF_GRID = MAP_GRID_COUNT / 2;
let latestOverviewGoCommand = '';

document.getElementById('title').textContent = `Map ${sceneData.mapKey} Overview | Build ${sceneData.build} | ${sceneData.tiles?.length || 0} extracted tile(s)`;
document.getElementById('messages').textContent = (sceneData.messages || []).join('\n');
document.getElementById('mode').addEventListener('change', drawPreview);
document.getElementById('palette').addEventListener('change', drawPreview);
document.getElementById('showGrid').addEventListener('change', drawPreview);
document.getElementById('smooth').addEventListener('change', event => { canvas.style.imageRendering = event.target.checked ? 'auto' : 'pixelated'; });
document.getElementById('zoomOut').addEventListener('click', () => setPreviewZoom(previewZoom / 1.25));
document.getElementById('zoomIn').addEventListener('click', () => setPreviewZoom(previewZoom * 1.25));
document.getElementById('zoomFit').addEventListener('click', fitPreviewToViewport);
document.getElementById('zoomActual').addEventListener('click', () => setPreviewZoom(1));
previewWrap.addEventListener('wheel', event => {
  if (!event.ctrlKey && !event.metaKey) return;
  event.preventDefault();
  const factor = event.deltaY < 0 ? 1.15 : 1 / 1.15;
  setPreviewZoom(previewZoom * factor);
}, { passive: false });
window.addEventListener('resize', () => { if (previewFitMode) fitPreviewToViewport(); });
document.getElementById('copyOverviewGo').addEventListener('click', () => copyText(latestOverviewGoCommand));

buildRows();
drawPreview();
fitPreviewToViewport();

canvas.addEventListener('mousemove', event => {
  const point = previewPointFromEvent(event);
  const tile = point?.tile || null;
  document.getElementById('hover').innerHTML = tile ? tileSummary(tile, point) : 'Move over the preview to inspect a tile.';
  updateOverviewCoordinatePanel(point);
});

canvas.addEventListener('mouseleave', () => {
  document.getElementById('hover').textContent = 'Move over the preview to inspect a tile.';
  updateOverviewCoordinatePanel(null);
});

function fitPreviewToViewport(){
  const rect = previewWrap.getBoundingClientRect();
  const availableWidth = Math.max(240, previewWrap.clientWidth - 24);
  const availableHeight = Math.max(260, window.innerHeight - rect.top - 52);
  const zoom = Math.min(4, availableWidth / canvas.width, availableHeight / canvas.height);
  setPreviewZoom(zoom, true);
}

function setPreviewZoom(value, fitMode = false){
  previewFitMode = fitMode;
  previewZoom = Math.max(0.1, Math.min(8, value));
  canvas.style.width = `${Math.round(canvas.width * previewZoom)}px`;
  canvas.style.height = `${Math.round(canvas.height * previewZoom)}px`;
  document.getElementById('zoomLabel').textContent = fitMode ? `Fit ${Math.round(previewZoom * 100)}%` : `${Math.round(previewZoom * 100)}%`;
}

function drawPreview(){
  const mode = document.getElementById('mode').value;
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  ctx.fillStyle = '#080808';
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  for (const tile of sceneData.tiles || []) {
    const ox = tile.tileX * previewResolution;
    const oy = tile.tileY * previewResolution;
    if (mode === 'coverage') drawCoverageTile(tile, ox, oy);
    else if (mode === 'terrain') drawTerrainTile(tile, ox, oy, false, false);
    else if (mode === 'terrainLiquid') drawTerrainTile(tile, ox, oy, true, false);
    else if (mode === 'liquid') drawLiquidTile(tile, ox, oy);
    else if (mode === 'holes') drawTerrainTile(tile, ox, oy, true, true);
    else if (mode === 'collision') drawHeatTile(tile, ox, oy, tile.collisionBytes || 0, maxCollisionBytes, [255,166,77]);
    else if (mode === 'navmesh') drawHeatTile(tile, ox, oy, tile.navmeshBytes || 0, maxNavmeshBytes, [126,211,255]);
    else if (mode === 'dataSize') drawHeatTile(tile, ox, oy, totalBytes(tile), maxTotalBytes, [255,222,89]);
    else if (mode === 'health') drawHealthTile(tile, ox, oy);
  }

  if (document.getElementById('showGrid').checked) drawTileGrid();
  updateLegend(mode);
}

function drawCoverageTile(tile, ox, oy){
  ctx.fillStyle = tile.preview ? '#3d7a48' : '#2e5738';
  ctx.fillRect(ox, oy, previewResolution, previewResolution);
}

function drawTerrainTile(tile, ox, oy, includeLiquid, includeHoles){
  const preview = tile.preview;
  const heights = preview?.terrainHeights;
  if (!heights || heights.length === 0) {
    ctx.fillStyle = tile.terrainBytes > 0 ? '#303030' : '#191919';
    ctx.fillRect(ox, oy, previewResolution, previewResolution);
  } else {
    for (let y = 0; y < previewResolution; y++) {
      for (let x = 0; x < previewResolution; x++) {
        const idx = y * previewResolution + x;
        const h = heights[idx];
        const n = normalizeHeight(h);
        ctx.fillStyle = terrainColor(n, h);
        ctx.fillRect(ox + x, oy + y, 1, 1);
      }
    }
  }

  if (includeLiquid) overlayMask(tile.preview?.liquidMask, ox, oy, 'rgba(46, 134, 255, 0.58)');
  if (includeHoles) overlayMask(tile.preview?.holeMask, ox, oy, 'rgba(255, 77, 77, 0.72)');
}

function drawLiquidTile(tile, ox, oy){
  ctx.fillStyle = '#101522';
  ctx.fillRect(ox, oy, previewResolution, previewResolution);
  overlayMask(tile.preview?.liquidMask, ox, oy, 'rgba(48, 151, 255, 0.9)');
}

function drawHeatTile(tile, ox, oy, value, max, rgb){
  const n = max <= 0 ? 0 : Math.min(1, value / max);
  const base = value > 0 ? Math.round(45 + n * 190) : 28;
  ctx.fillStyle = value > 0 ? `rgb(${Math.round(rgb[0] * n + 35 * (1 - n))},${Math.round(rgb[1] * n + 35 * (1 - n))},${Math.round(rgb[2] * n + 35 * (1 - n))})` : '#191919';
  ctx.fillRect(ox, oy, previewResolution, previewResolution);
  if (value > 0 && previewResolution >= 8) {
    ctx.fillStyle = `rgba(255,255,255,${0.08 + n * 0.18})`;
    ctx.fillRect(ox + 1, oy + 1, Math.max(1, Math.floor(previewResolution * n)), Math.max(1, Math.floor(previewResolution * n)));
  }
}

function drawHealthTile(tile, ox, oy){
  const expected = [tile.terrainBytes, tile.liquidBytes, tile.collisionBytes, tile.navmeshBytes];
  const missing = expected.filter(v => !v || v <= 0).length;
  const tinyTerrain = tile.terrainBytes > 0 && tile.terrainBytes < 1024;
  if (missing === 0 && !tinyTerrain) ctx.fillStyle = '#3f8f50';
  else if (missing === 0) ctx.fillStyle = '#a58b34';
  else if (missing < 4) ctx.fillStyle = '#9c5b35';
  else ctx.fillStyle = '#3a2222';
  ctx.fillRect(ox, oy, previewResolution, previewResolution);
}

function overlayMask(mask, ox, oy, color){
  if (!mask || mask.length === 0) return;
  ctx.fillStyle = color;
  for (let y = 0; y < previewResolution; y++) {
    for (let x = 0; x < previewResolution; x++) {
      if (mask[(y * previewResolution) + x]) ctx.fillRect(ox + x, oy + y, 1, 1);
    }
  }
}

function drawTileGrid(){
  ctx.save();
  ctx.strokeStyle = 'rgba(255,255,255,.15)';
  ctx.lineWidth = 1;
  for (let i = 0; i <= mapSize; i++) {
    const p = i * previewResolution + 0.5;
    ctx.beginPath(); ctx.moveTo(p, 0); ctx.lineTo(p, canvas.height); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(0, p); ctx.lineTo(canvas.width, p); ctx.stroke();
  }
  ctx.restore();
}

function terrainColor(n, height){
  const palette = document.getElementById('palette').value;
  n = Math.max(0, Math.min(1, n));
  if (palette === 'grayscale') {
    const v = Math.round(35 + n * 210);
    return `rgb(${v},${v},${v})`;
  }
  if (palette === 'height') {
    return ramp(n, [[27,67,122],[43,132,89],[207,176,83],[148,92,61],[232,232,220]]);
  }
  if (palette === 'debug') {
    const band = Math.floor(n * 10) % 2;
    return band ? ramp(n, [[31,89,158],[234,220,88]]) : ramp(n, [[83,42,130],[230,76,76]]);
  }
  return ramp(n, [[34,63,40],[54,105,52],[117,132,74],[143,110,72],[185,178,155],[232,232,222]]);
}

function ramp(n, stops){
  if (n <= 0) return rgb(stops[0]);
  if (n >= 1) return rgb(stops[stops.length - 1]);
  const scaled = n * (stops.length - 1);
  const i = Math.floor(scaled);
  const t = scaled - i;
  const a = stops[i];
  const b = stops[i + 1];
  return `rgb(${mix(a[0],b[0],t)},${mix(a[1],b[1],t)},${mix(a[2],b[2],t)})`;
}
function mix(a,b,t){ return Math.round(a + (b - a) * t); }
function rgb(v){ return `rgb(${v[0]},${v[1]},${v[2]})`; }
function normalizeHeight(value){
  const span = terrainRange.max - terrainRange.min;
  if (!Number.isFinite(value) || span <= 0.0001) return 0.5;
  return (value - terrainRange.min) / span;
}

function getTerrainRange(){
  let min = Number.POSITIVE_INFINITY;
  let max = Number.NEGATIVE_INFINITY;
  for (const tile of sceneData.tiles || []) {
    const heights = tile.preview?.terrainHeights;
    if (!heights) continue;
    for (const h of heights) {
      if (!Number.isFinite(h)) continue;
      min = Math.min(min, h);
      max = Math.max(max, h);
    }
  }
  if (!Number.isFinite(min) || !Number.isFinite(max)) return { min: 0, max: 1 };
  return { min, max };
}

function updateLegend(mode){
  const legend = document.getElementById('legend');
  const terrainText = `Terrain range: ${terrainRange.min.toFixed(2)} to ${terrainRange.max.toFixed(2)}`;
  const items = {
    terrainLiquid: [['#5b833e','Terrain height'], ['#2e86ff','Liquid overlay'], ['#ff4d4d','Terrain holes']],
    terrain: [['#5b833e', terrainText]],
    liquid: [['#3097ff','Liquid cells'], ['#101522','No liquid']],
    holes: [['#ff4d4d','Hole cells'], ['#5b833e','Terrain']],
    coverage: [['#3d7a48','Preview loaded'], ['#242424','Missing tile']],
    collision: [['#ffa64d','More collision data'], ['#191919','No collision data']],
    navmesh: [['#7ed3ff','Navmesh data present'], ['#191919','No navmesh data']],
    dataSize: [['#ffde59','Larger combined tile data'], ['#191919','No data']],
    health: [['#3f8f50','All components present'], ['#a58b34','Tiny/flat terrain'], ['#9c5b35','Partial data'], ['#3a2222','Missing data']],
  }[mode] || [];
  legend.innerHTML = items.map(([color, text]) => `<span><span class="swatch" style="background:${color}"></span>${escapeHtml(text)}</span>`).join('');
}

function buildRows(){
  const rows = document.getElementById('tiles');
  for (const tile of sceneData.tiles || []) {
    const preview = tile.preview;
    const tr = document.createElement('tr');
    tr.innerHTML = `<td>${pad(tile.tileX)}_${pad(tile.tileY)}</td><td>${previewBadges(preview)}</td><td>${tile.flags}</td><td>${bytes(tile.terrainBytes)}</td><td>${bytes(tile.liquidBytes)}</td><td>${bytes(tile.collisionBytes)}</td><td>${bytes(tile.navmeshBytes)}</td>`;
    rows.appendChild(tr);
  }
}

function previewBadges(preview){
  if (!preview) return '<span class="muted">none</span>';
  const badges = [];
  if (preview.hasTerrain) badges.push('<span class="pill">terrain</span>');
  if (preview.hasLiquid) badges.push('<span class="pill">liquid</span>');
  if (preview.hasHoles) badges.push('<span class="pill">holes</span>');
  return badges.length ? badges.join('') : '<span class="muted">flat/empty</span>';
}

function previewPointFromEvent(event){
  const rect = canvas.getBoundingClientRect();
  const pixelX = (event.clientX - rect.left) * (canvas.width / rect.width);
  const pixelY = (event.clientY - rect.top) * (canvas.height / rect.height);
  const tileX = Math.floor(pixelX / previewResolution);
  const tileY = Math.floor(pixelY / previewResolution);
  if (tileX < 0 || tileX >= 64 || tileY < 0 || tileY >= 64) return null;
  const tile = tileMap.get(`${tileX}_${tileY}`) || null;
  if (!tile) return null;
  const localPreviewX = clamp(pixelX - tileX * previewResolution, 0, Math.max(0, previewResolution - 0.0001));
  const localPreviewY = clamp(pixelY - tileY * previewResolution, 0, Math.max(0, previewResolution - 0.0001));
  const localX = (localPreviewX / previewResolution) * TILE_SIZE;
  const localZ = (localPreviewY / previewResolution) * TILE_SIZE;
  const height = sampleOverviewHeight(tile, localPreviewX, localPreviewY);
  return { tile, tileX, tileY, localX, localZ, localPreviewX, localPreviewY, coordinate: overviewToGameCoordinate(tileX, tileY, localX, localZ, height) };
}

function overviewToGameCoordinate(tileX, tileY, localX, localZ, height){
  return {
    x: (MAP_HALF_GRID - tileY) * TILE_SIZE - localZ,
    y: (MAP_HALF_GRID - tileX) * TILE_SIZE - localX,
    z: height,
    mapId: sceneData.mapId,
  };
}

function sampleOverviewHeight(tile, localPreviewX, localPreviewY){
  const preview = tile.preview;
  const heights = preview?.terrainHeights;
  if (!heights || !heights.length) {
    return preview ? ((preview.minimumHeight + preview.maximumHeight) * 0.5) : 0;
  }

  const maxSample = previewResolution - 1;
  if (maxSample <= 0) return heights[0] ?? 0;
  const sx = clamp(localPreviewX, 0, maxSample);
  const sy = clamp(localPreviewY, 0, maxSample);
  const x0 = Math.floor(sx);
  const y0 = Math.floor(sy);
  const x1 = Math.min(maxSample, x0 + 1);
  const y1 = Math.min(maxSample, y0 + 1);
  const tx = sx - x0;
  const ty = sy - y0;
  const h00 = heights[y0 * previewResolution + x0] ?? 0;
  const h10 = heights[y0 * previewResolution + x1] ?? h00;
  const h01 = heights[y1 * previewResolution + x0] ?? h00;
  const h11 = heights[y1 * previewResolution + x1] ?? h10;
  const hx0 = h00 + (h10 - h00) * tx;
  const hx1 = h01 + (h11 - h01) * tx;
  return hx0 + (hx1 - hx0) * ty;
}

function updateOverviewCoordinatePanel(point){
  const tileEl = document.getElementById('overviewCoordTile');
  const localEl = document.getElementById('overviewCoordLocal');
  const xyzEl = document.getElementById('overviewCoordXyz');
  const commandEl = document.getElementById('overviewCoordCommand');
  const copyButton = document.getElementById('copyOverviewGo');
  if (!point) {
    tileEl.textContent = '—';
    localEl.textContent = '—';
    xyzEl.textContent = '—';
    commandEl.textContent = '—';
    latestOverviewGoCommand = '';
    copyButton.disabled = true;
    return;
  }

  const c = point.coordinate;
  const xyz = `${formatCoord(c.x)} ${formatCoord(c.y)} ${formatCoord(c.z)}`;
  latestOverviewGoCommand = `.go xyz ${xyz} ${c.mapId}`;
  tileEl.textContent = `${pad(point.tileX)}_${pad(point.tileY)}`;
  localEl.textContent = `${formatCoord(point.localX)}, ${formatCoord(point.localZ)} within tile`;
  xyzEl.textContent = xyz;
  commandEl.textContent = latestOverviewGoCommand;
  copyButton.disabled = false;
}

function tileSummary(tile, point){
  const preview = tile.preview;
  const parts = [`<strong>Tile ${pad(tile.tileX)}_${pad(tile.tileY)}</strong>`, `flags=${tile.flags}`, `terrain=${bytesText(tile.terrainBytes)}`, `liquid=${bytesText(tile.liquidBytes)}`, `collision=${bytesText(tile.collisionBytes)}`, `navmesh=${bytesText(tile.navmeshBytes)}`];
  if (preview?.terrainHeights) parts.push(`height=${preview.minimumHeight.toFixed(2)} to ${preview.maximumHeight.toFixed(2)}`);
  if (point?.coordinate) parts.push(`hover XYZ=${formatCoord(point.coordinate.x)} ${formatCoord(point.coordinate.y)} ${formatCoord(point.coordinate.z)}`);
  if (preview?.hasLiquid) parts.push('liquid cells detected');
  if (preview?.hasHoles) parts.push('terrain holes detected');
  return parts.join(' &nbsp; ');
}

function clamp(value, min, max){ return Math.min(max, Math.max(min, value)); }
function formatCoord(value){ return Number(value).toFixed(3); }
async function copyText(value){
  if (!value) return;
  try {
    await navigator.clipboard.writeText(value);
  } catch {
    const textarea = document.createElement('textarea');
    textarea.value = value;
    textarea.style.position = 'fixed';
    textarea.style.left = '-9999px';
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    document.body.removeChild(textarea);
  }
}
function totalBytes(tile){ return (tile.terrainBytes || 0) + (tile.liquidBytes || 0) + (tile.collisionBytes || 0) + (tile.navmeshBytes || 0); }
function pad(value){ return String(value).padStart(2, '0'); }
function bytes(value){
  if (!value || value <= 0) return '<span class="muted">missing</span>';
  return `<span title="${value.toLocaleString()} bytes">${formatBytes(value)}</span>`;
}
function bytesText(value){ return value && value > 0 ? formatBytes(value) : 'missing'; }
function formatBytes(value){
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let size = Number(value);
  let unit = 0;
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024;
    unit++;
  }
  const decimals = unit === 0 ? 0 : (size >= 100 ? 0 : size >= 10 ? 1 : 2);
  return `${size.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals })} ${units[unit]}`;
}
function escapeHtml(value){ return String(value).replace(/[&<>'"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c])); }
</script>
</body>
</html>
""";

    private const string TileTemplate = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>__TITLE__</title>
<style>
html,body{margin:0;height:100%;overflow:hidden;background:#111;color:#ddd;font-family:system-ui,Segoe UI,sans-serif}#viewer{position:fixed;inset:0}.panel{position:fixed;left:12px;top:12px;max-width:min(430px,calc(100vw - 24px));max-height:calc(100vh - 24px);overflow:auto;background:rgba(20,20,20,.92);border:1px solid #333;border-radius:10px;padding:14px;box-shadow:0 8px 28px rgba(0,0,0,.35)}.panel.collapsed{max-height:none;overflow:hidden}.panel.collapsed .collapsible{display:none}h1{font-size:18px;margin:0 0 10px}h2{font-size:14px;margin:14px 0 6px}.row{display:flex;gap:10px;flex-wrap:wrap}.pill{padding:3px 7px;border-radius:999px;background:#2a2a2a;border:1px solid #444;font-size:12px}.ok{color:#9be29b}.bad{color:#ff9c9c}.muted{color:#aaa}.small{font-size:12px}.table{border-collapse:collapse;width:100%;font-size:12px}.table th,.table td{border-bottom:1px solid #333;padding:4px 6px;text-align:left}.checks{display:grid;grid-template-columns:1fr 1fr;gap:4px 10px}.checks label{font-size:13px}.viewerControls{display:flex;gap:6px;flex-wrap:wrap;margin:8px 0 10px}.viewerControls button{background:#222;color:#ddd;border:1px solid #444;border-radius:6px;padding:5px 8px;cursor:pointer}.viewerControls button:hover{background:#2d2d2d}.coordBox{background:#171717;border:1px solid #333;border-radius:8px;padding:8px;margin-top:6px}.coordBox .line{margin:3px 0}.coordBox button{background:#222;color:#ddd;border:1px solid #444;border-radius:6px;padding:4px 7px;cursor:pointer;margin:4px 4px 0 0}.coordBox button:hover{background:#2d2d2d}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,Liberation Mono,monospace}.warning{color:#ffd38d}.errors{white-space:pre-wrap;color:#ffb1b1}</style>
</head>
<body>
<div id="viewer"></div>
<div class="panel" id="infoPanel">
  <h1 id="title"></h1>
  <div class="viewerControls">
    <button id="fitView" type="button" title="Fit all visible geometry into the viewport">Fit view</button>
    <button id="zoomIn3d" type="button" title="Zoom camera in">Zoom +</button>
    <button id="zoomOut3d" type="button" title="Zoom camera out">Zoom −</button>
    <button id="togglePanel" type="button" title="Collapse or expand this panel">Hide details</button>
  </div>
  <div class="collapsible">
  <div class="row" id="summary"></div>
  <h2>In-game coordinates</h2>
  <div class="coordBox small" id="coordinateBox">
    <div class="line muted">Hover the terrain to preview coordinates. Click the terrain surface to lock accurate world-space coordinates for <span class="mono">.go xyz</span>.</div>
    <div class="line">Map: <span id="coordMap" class="mono">—</span></div>
    <div class="line">Hover XYZ: <span id="coordHoverXyz" class="mono">—</span></div>
    <div class="line">Hover command: <span id="coordHoverCommand" class="mono">—</span></div>
    <div class="line">Selected tile/local: <span id="coordLocal" class="mono">—</span></div>
    <div class="line">XYZ: <span id="coordXyz" class="mono">—</span></div>
    <div class="line">Command: <span id="coordCommand" class="mono">—</span></div>
    <button id="copyXyz" type="button" disabled>Copy XYZ</button>
    <button id="copyGo" type="button" disabled>Copy .go xyz</button>
  </div>
  <h2>Layers</h2>
  <div class="checks">
    <label><input type="checkbox" data-layer="terrain" checked> Terrain</label>
    <label><input type="checkbox" data-layer="liquid" checked> Liquid</label>
    <label><input type="checkbox" data-layer="holes" checked> Terrain holes</label>
    <label><input type="checkbox" data-layer="collisionBounds" checked> Collision bounds</label>
    <label><input type="checkbox" data-layer="collisionGeometry" checked> Collision geometry</label>
    <label><input type="checkbox" id="normalizeCollision" checked> Normalize collision to tile</label>
  </div>
  <h2>Components</h2>
  <table class="table"><thead><tr><th>Kind</th><th>Status</th><th>Size</th></tr></thead><tbody id="components"></tbody></table>
  <h2>Collision models</h2>
  <div id="collisionSummary" class="small muted"></div>
  <h2>Errors</h2>
  <div id="errors" class="errors small"></div>
  </div>
</div>
<script type="module">
import * as THREE from 'https://unpkg.com/three@0.160.0/build/three.module.js';
import { OrbitControls } from 'https://unpkg.com/three@0.160.0/examples/jsm/controls/OrbitControls.js';

const sceneData = __DATA__;
const TILE_SIZE = 533.333333;
const MAP_GRID_COUNT = 64;
const MAP_HALF_GRID = MAP_GRID_COUNT / 2;
const TERRAIN_GRID_SIZE = 128;
const layerRoots = new Map();
const coordRaycaster = new THREE.Raycaster();
const coordPointer = new THREE.Vector2();
const coordRaycastMeshes = [];
let terrainMesh = null;
let latestXyzText = '';
let latestGoCommand = '';
let latestHoverGoCommand = '';
let collisionOffset = new THREE.Vector3();
let normalizeCollision = true;

const container = document.getElementById('viewer');
const scene = new THREE.Scene();
scene.background = new THREE.Color(0x111111);
const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 250000);
const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
container.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;
controls.target.set(TILE_SIZE / 2, 0, TILE_SIZE / 2);
camera.position.set(TILE_SIZE * 0.9, Math.max(260, heightRange() * 2.0), TILE_SIZE * 1.25);

scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 1.3));
const dir = new THREE.DirectionalLight(0xffffff, 1.0);
dir.position.set(300, 800, 300);
scene.add(dir);
scene.add(new THREE.GridHelper(TILE_SIZE, 16, 0x444444, 0x222222));

setupPanel();
setCoordinateStatus(null);
buildScene();
applyLayerVisibility();
fitCameraToVisibleScene();
animate();

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

document.querySelectorAll('input[data-layer]').forEach(input => input.addEventListener('change', applyLayerVisibility));
document.getElementById('normalizeCollision').addEventListener('change', event => {
  normalizeCollision = event.target.checked;
  rebuildCollisionLayers();
});
document.getElementById('fitView').addEventListener('click', fitCameraToVisibleScene);
document.getElementById('zoomIn3d').addEventListener('click', () => zoomCamera(0.8));
document.getElementById('zoomOut3d').addEventListener('click', () => zoomCamera(1.25));
document.getElementById('togglePanel').addEventListener('click', event => {
  const panel = document.getElementById('infoPanel');
  panel.classList.toggle('collapsed');
  event.target.textContent = panel.classList.contains('collapsed') ? 'Show details' : 'Hide details';
});
renderer.domElement.addEventListener('pointermove', updateCoordinateHover);
renderer.domElement.addEventListener('click', updateCoordinateSelection);
document.getElementById('copyXyz').addEventListener('click', () => copyText(latestXyzText));
document.getElementById('copyGo').addEventListener('click', () => copyText(latestGoCommand));


function updateCoordinateHover(event) {
  const hit = getTerrainHit(event);
  if (!hit) {
    renderer.domElement.title = 'Click the terrain surface to generate .go xyz coordinates.';
    setHoverCoordinateStatus(null);
    return;
  }

  const coordinate = toGameCoordinate(hit.point);
  setHoverCoordinateStatus(coordinate);
  renderer.domElement.title = latestHoverGoCommand;
}

function updateCoordinateSelection(event) {
  const hit = getTerrainHit(event);
  if (!hit) {
    setCoordinateStatus(null);
    return;
  }

  setCoordinateStatus(toGameCoordinate(hit.point));
}

function getTerrainHit(event) {
  if (!coordRaycastMeshes.length) return null;
  const rect = renderer.domElement.getBoundingClientRect();
  coordPointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  coordPointer.y = -(((event.clientY - rect.top) / rect.height) * 2 - 1);
  coordRaycaster.setFromCamera(coordPointer, camera);
  const hits = coordRaycaster.intersectObjects(coordRaycastMeshes, false);
  return hits.length ? hits[0] : null;
}

function toGameCoordinate(point) {
  const localX = clamp(point.x, 0, TILE_SIZE);
  const localZ = clamp(point.z, 0, TILE_SIZE);
  const gridX = (localX / TILE_SIZE) * TERRAIN_GRID_SIZE;
  const gridY = (localZ / TILE_SIZE) * TERRAIN_GRID_SIZE;
  const height = sampleTerrainHeight(gridX, gridY, point.y);

  // Important: ADT/mapstore tile coordinates are not the same as in-game .go xyz coordinates.
  // The ADT tile name uses tileX_tileY, while WoW world-space .go xyz uses X/Y axes.
  // For normal ADT/MaNGOS/Trinity-style map coordinates:
  //   world X comes from tileY + local row/Z inside the tile.
  //   world Y comes from tileX + local column/X inside the tile.
  const worldX = (MAP_HALF_GRID - sceneData.tileY) * TILE_SIZE - localZ;
  const worldY = (MAP_HALF_GRID - sceneData.tileX) * TILE_SIZE - localX;

  return {
    x: worldX,
    y: worldY,
    z: height,
    mapId: sceneData.mapId,
    localX,
    localZ,
    gridX,
    gridY,
  };
}

function sampleTerrainHeight(gridX, gridY, fallbackHeight) {
  const terrain = sceneData.terrain;
  const heights = terrain?.v9Heights;
  if (!terrain || !heights || !heights.length) return fallbackHeight;

  const clampedX = clamp(gridX, 0, TERRAIN_GRID_SIZE);
  const clampedY = clamp(gridY, 0, TERRAIN_GRID_SIZE);
  const x0 = Math.floor(clampedX);
  const y0 = Math.floor(clampedY);
  const x1 = Math.min(TERRAIN_GRID_SIZE, x0 + 1);
  const y1 = Math.min(TERRAIN_GRID_SIZE, y0 + 1);
  const tx = clampedX - x0;
  const ty = clampedY - y0;
  const rowSize = TERRAIN_GRID_SIZE + 1;
  const h00 = heights[y0 * rowSize + x0];
  const h10 = heights[y0 * rowSize + x1];
  const h01 = heights[y1 * rowSize + x0];
  const h11 = heights[y1 * rowSize + x1];
  const hx0 = h00 + (h10 - h00) * tx;
  const hx1 = h01 + (h11 - h01) * tx;
  return hx0 + (hx1 - hx0) * ty;
}

function setHoverCoordinateStatus(coordinate) {
  const hoverXyz = document.getElementById('coordHoverXyz');
  const hoverCommand = document.getElementById('coordHoverCommand');
  if (!coordinate) {
    hoverXyz.textContent = '—';
    hoverCommand.textContent = '—';
    latestHoverGoCommand = '';
    return;
  }

  const xyz = `${formatCoord(coordinate.x)} ${formatCoord(coordinate.y)} ${formatCoord(coordinate.z)}`;
  latestHoverGoCommand = `.go xyz ${xyz} ${coordinate.mapId}`;
  hoverXyz.textContent = xyz;
  hoverCommand.textContent = latestHoverGoCommand;
}

function setCoordinateStatus(coordinate) {
  const coordMap = document.getElementById('coordMap');
  const coordLocal = document.getElementById('coordLocal');
  const coordXyz = document.getElementById('coordXyz');
  const coordCommand = document.getElementById('coordCommand');
  const copyXyz = document.getElementById('copyXyz');
  const copyGo = document.getElementById('copyGo');

  if (!coordinate) {
    coordMap.textContent = String(sceneData.mapId);
    coordLocal.textContent = 'No terrain point selected';
    coordXyz.textContent = '—';
    coordCommand.textContent = '—';
    latestXyzText = '';
    latestGoCommand = '';
    copyXyz.disabled = true;
    copyGo.disabled = true;
    return;
  }

  latestXyzText = `${formatCoord(coordinate.x)} ${formatCoord(coordinate.y)} ${formatCoord(coordinate.z)}`;
  latestGoCommand = `.go xyz ${latestXyzText} ${coordinate.mapId}`;
  coordMap.textContent = String(coordinate.mapId);
  coordLocal.textContent = `tile ${pad(sceneData.tileX)}_${pad(sceneData.tileY)} | local ${formatCoord(coordinate.localX)}, ${formatCoord(coordinate.localZ)} | terrain grid ${formatCoord(coordinate.gridX)}, ${formatCoord(coordinate.gridY)}`;
  coordXyz.textContent = latestXyzText;
  coordCommand.textContent = latestGoCommand;
  copyXyz.disabled = false;
  copyGo.disabled = false;
}

async function copyText(value) {
  if (!value) return;
  try {
    await navigator.clipboard.writeText(value);
  } catch {
    const textarea = document.createElement('textarea');
    textarea.value = value;
    textarea.style.position = 'fixed';
    textarea.style.left = '-9999px';
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    document.body.removeChild(textarea);
  }
}

function formatCoord(value) {
  return Number(value).toFixed(3);
}

function pad(value) {
  return String(value).padStart(2, '0');
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function formatBytes(value) {
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let size = Number(value);
  let unit = 0;
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024;
    unit++;
  }
  const decimals = unit === 0 ? 0 : (size >= 100 ? 0 : size >= 10 ? 1 : 2);
  return `${size.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals })} ${units[unit]}`;
}

function setupPanel() {
  document.getElementById('title').textContent = `Map ${sceneData.tileKey}`;
  const summary = document.getElementById('summary');
  summary.appendChild(pill(sceneData.terrain?.hasHeightGrid ? 'Terrain height grid' : 'No terrain height grid', sceneData.terrain ? 'ok' : 'bad'));
  summary.appendChild(pill(sceneData.liquid?.hasLiquid ? 'Liquid present' : 'No liquid', sceneData.liquid?.hasLiquid ? 'ok' : 'muted'));
  summary.appendChild(pill(`${sceneData.collision?.placements?.length ?? 0} collision placement(s)`, sceneData.collision ? 'ok' : 'muted'));
  summary.appendChild(pill(sceneData.navmesh?.hasNavigationData ? 'Navmesh present' : 'Navmesh placeholder/missing', sceneData.navmesh?.hasNavigationData ? 'ok' : 'muted'));

  const components = document.getElementById('components');
  for (const component of sceneData.components || []) {
    const tr = document.createElement('tr');
    tr.innerHTML = `<td>${component.kind}</td><td class="${component.loaded ? 'ok' : (component.exists ? 'bad' : 'muted')}">${component.loaded ? 'loaded' : (component.exists ? 'failed' : 'missing')}</td><td>${component.fileSize ? `<span title="${component.fileSize.toLocaleString()} bytes">${formatBytes(component.fileSize)}</span>` : ''}</td>`;
    components.appendChild(tr);
  }

  const collision = sceneData.collision;
  document.getElementById('collisionSummary').innerHTML = collision
    ? `Models loaded: ${collision.loadedModelCount}<br>Models missing: ${collision.missingModelCount}<br>Geometry instances embedded: ${collision.geometryInstances.length}<br>Triangles embedded: ${collision.embeddedTriangleCount.toLocaleString()}<br>Skipped geometry instances: ${collision.skippedGeometryInstances}`
    : 'No collision tile was loaded.';

  document.getElementById('errors').textContent = (sceneData.errors || []).join('\n') || 'None';
}

function buildScene() {
  addTerrain();
  addLiquid();
  rebuildCollisionLayers();
}

function rebuildCollisionLayers() {
  removeLayer('collisionBounds');
  removeLayer('collisionGeometry');
  collisionOffset = normalizeCollision ? computeCollisionOffset() : new THREE.Vector3();
  addCollisionBounds();
  addCollisionGeometry();
  applyLayerVisibility();
}

function addTerrain() {
  if (!sceneData.terrain) return;
  const root = layerRoot('terrain');
  const terrain = sceneData.terrain;
  const heights = terrain.v9Heights;
  const size = 129;
  const vertices = [];
  for (let row = 0; row < size; row++) {
    for (let col = 0; col < size; col++) {
      const height = heights ? heights[row * size + col] : terrain.gridHeight;
      vertices.push((col / 128) * TILE_SIZE, height, (row / 128) * TILE_SIZE);
    }
  }

  const indices = [];
  const holes = terrain.holes || [];
  for (let row = 0; row < 128; row++) {
    for (let col = 0; col < 128; col++) {
      const holeX = Math.floor(col / 8);
      const holeY = Math.floor(row / 8);
      if (holes[holeY * 16 + holeX]) continue;
      const a = row * size + col;
      const b = a + 1;
      const c = a + size;
      const d = c + 1;
      indices.push(a, c, b, b, c, d);
    }
  }

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();
  const material = new THREE.MeshLambertMaterial({ color: 0x667a4f, side: THREE.DoubleSide });
  const mesh = new THREE.Mesh(geometry, material);
  mesh.name = 'Terrain';
  mesh.userData.coordinateLayer = 'terrain';
  terrainMesh = mesh;
  coordRaycastMeshes.push(mesh);
  root.add(mesh);

  if (terrain.hasHoles) addHoleOverlay();
}

function addHoleOverlay() {
  const root = layerRoot('holes');
  const holes = sceneData.terrain?.holes || [];
  const cellSize = TILE_SIZE / 16;
  const material = new THREE.MeshBasicMaterial({ color: 0x000000, transparent: true, opacity: 0.55, side: THREE.DoubleSide });
  for (let row = 0; row < 16; row++) {
    for (let col = 0; col < 16; col++) {
      if (!holes[row * 16 + col]) continue;
      const geometry = new THREE.PlaneGeometry(cellSize, cellSize);
      geometry.rotateX(-Math.PI / 2);
      const mesh = new THREE.Mesh(geometry, material);
      mesh.position.set((col + 0.5) * cellSize, sceneData.terrain.maximumHeight + 0.5, (row + 0.5) * cellSize);
      root.add(mesh);
    }
  }
}

function addLiquid() {
  if (!sceneData.liquid?.hasLiquid) return;
  const root = layerRoot('liquid');
  const liquid = sceneData.liquid;
  const width = Math.max(1, liquid.width);
  const height = Math.max(1, liquid.height);
  const cell = TILE_SIZE / 128;
  const vertices = [];
  const indices = [];
  const heights = liquid.liquidHeights;

  for (let row = 0; row < height; row++) {
    for (let col = 0; col < width; col++) {
      const sample = heights ? heights[row * width + col] : liquid.liquidLevel;
      vertices.push((liquid.offsetX + col) * cell, sample, (liquid.offsetY + row) * cell);
    }
  }

  if (width > 1 && height > 1) {
    for (let row = 0; row < height - 1; row++) {
      for (let col = 0; col < width - 1; col++) {
        const a = row * width + col;
        const b = a + 1;
        const c = a + width;
        const d = c + 1;
        indices.push(a, c, b, b, c, d);
      }
    }
  } else {
    vertices.length = 0;
    const x0 = liquid.offsetX * cell;
    const z0 = liquid.offsetY * cell;
    const x1 = (liquid.offsetX + Math.max(1, liquid.width)) * cell;
    const z1 = (liquid.offsetY + Math.max(1, liquid.height)) * cell;
    vertices.push(x0, liquid.liquidLevel, z0, x1, liquid.liquidLevel, z0, x0, liquid.liquidLevel, z1, x1, liquid.liquidLevel, z1);
    indices.push(0, 2, 1, 1, 2, 3);
  }

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();
  const material = new THREE.MeshLambertMaterial({ color: 0x2d80d3, transparent: true, opacity: 0.55, side: THREE.DoubleSide });
  const mesh = new THREE.Mesh(geometry, material);
  mesh.name = 'Liquid';
  root.add(mesh);
}

function addCollisionBounds() {
  if (!sceneData.collision) return;
  const root = layerRoot('collisionBounds');
  for (const placement of sceneData.collision.placements || []) {
    const min = toSceneVector(placement.bounds.minimum, collisionOffset);
    const max = toSceneVector(placement.bounds.maximum, collisionOffset);
    const box = new THREE.Box3(min, max);
    const helper = new THREE.Box3Helper(box, placement.modelLoaded ? 0xffcc66 : 0xff6666);
    helper.name = placement.normalizedPath;
    root.add(helper);
  }
}

function addCollisionGeometry() {
  if (!sceneData.collision) return;
  const root = layerRoot('collisionGeometry');
  const material = new THREE.MeshBasicMaterial({ color: 0xffcc66, wireframe: true, transparent: true, opacity: 0.45, side: THREE.DoubleSide });
  for (const instance of sceneData.collision.geometryInstances || []) {
    const groupRoot = new THREE.Group();
    groupRoot.name = instance.normalizedPath;
    const position = toSceneVector(instance.position, collisionOffset);
    groupRoot.position.copy(position);
    groupRoot.rotation.set(THREE.MathUtils.degToRad(instance.rotation.z), THREE.MathUtils.degToRad(instance.rotation.x), THREE.MathUtils.degToRad(instance.rotation.y));
    for (const group of instance.groups || []) {
      const vertices = remapPackedVertices(group.vertices);
      const geometry = new THREE.BufferGeometry();
      geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
      geometry.setIndex(group.indices);
      const mesh = new THREE.Mesh(geometry, material);
      groupRoot.add(mesh);
    }
    root.add(groupRoot);
  }
}

function remapPackedVertices(values) {
  const result = [];
  for (let i = 0; i < values.length; i += 3) {
    result.push(values[i], values[i + 2], values[i + 1]);
  }
  return result;
}

function computeCollisionOffset() {
  const placements = sceneData.collision?.placements || [];
  if (!placements.length) return new THREE.Vector3();
  let min = new THREE.Vector3(Number.POSITIVE_INFINITY, Number.POSITIVE_INFINITY, Number.POSITIVE_INFINITY);
  let max = new THREE.Vector3(Number.NEGATIVE_INFINITY, Number.NEGATIVE_INFINITY, Number.NEGATIVE_INFINITY);
  for (const placement of placements) {
    const a = toSceneVector(placement.bounds.minimum, new THREE.Vector3());
    const b = toSceneVector(placement.bounds.maximum, new THREE.Vector3());
    min.min(a).min(b);
    max.max(a).max(b);
  }
  const center = min.clone().add(max).multiplyScalar(0.5);
  const target = new THREE.Vector3(TILE_SIZE / 2, sceneData.terrain ? (sceneData.terrain.minimumHeight + sceneData.terrain.maximumHeight) * 0.5 : 0, TILE_SIZE / 2);
  return target.sub(center);
}

function heightRange() {
  if (!sceneData.terrain) return 100;
  return Math.max(100, sceneData.terrain.maximumHeight - sceneData.terrain.minimumHeight);
}

function toSceneVector(value, offset) {
  return new THREE.Vector3(value.x + offset.x, value.z + offset.y, value.y + offset.z);
}

function layerRoot(name) {
  let root = layerRoots.get(name);
  if (!root) {
    root = new THREE.Group();
    root.name = name;
    layerRoots.set(name, root);
    scene.add(root);
  }
  return root;
}

function removeLayer(name) {
  const root = layerRoots.get(name);
  if (!root) return;
  scene.remove(root);
  root.traverse(object => {
    if (object.geometry) object.geometry.dispose();
    if (object.material) object.material.dispose?.();
  });
  layerRoots.delete(name);
}

function applyLayerVisibility() {
  for (const input of document.querySelectorAll('input[data-layer]')) {
    const root = layerRoots.get(input.dataset.layer);
    if (root) root.visible = input.checked;
  }
}

function fitCameraToVisibleScene() {
  const box = new THREE.Box3();
  let hasBox = false;
  for (const root of layerRoots.values()) {
    if (!root.visible && root.children.length === 0) continue;
    const rootBox = new THREE.Box3().setFromObject(root);
    if (rootBox.isEmpty()) continue;
    if (!hasBox) {
      box.copy(rootBox);
      hasBox = true;
    } else {
      box.union(rootBox);
    }
  }

  if (!hasBox || box.isEmpty()) {
    controls.target.set(TILE_SIZE / 2, 0, TILE_SIZE / 2);
    camera.position.set(TILE_SIZE * 0.9, Math.max(260, heightRange() * 2.0), TILE_SIZE * 1.25);
    controls.update();
    return;
  }

  const center = box.getCenter(new THREE.Vector3());
  const size = box.getSize(new THREE.Vector3());
  const maxDim = Math.max(size.x, size.y, size.z, 1);
  const fov = THREE.MathUtils.degToRad(camera.fov);
  const distance = (maxDim / (2 * Math.tan(fov / 2))) * 1.35;
  const direction = camera.position.clone().sub(controls.target);
  if (direction.lengthSq() < 0.0001) direction.set(1, 0.7, 1);
  direction.normalize();
  controls.target.copy(center);
  camera.position.copy(center).add(direction.multiplyScalar(distance));
  camera.near = Math.max(0.1, distance / 1000);
  camera.far = Math.max(250000, distance * 12);
  camera.updateProjectionMatrix();
  controls.update();
}

function zoomCamera(factor) {
  const direction = camera.position.clone().sub(controls.target);
  if (direction.lengthSq() < 0.0001) return;
  direction.multiplyScalar(factor);
  camera.position.copy(controls.target).add(direction);
  controls.update();
}

function pill(text, className) {
  const span = document.createElement('span');
  span.className = `pill ${className}`;
  span.textContent = text;
  return span;
}

function animate() {
  requestAnimationFrame(animate);
  controls.update();
  renderer.render(scene, camera);
}
</script>
</body>
</html>
""";
}
