import http.server
import socketserver
import threading
import webbrowser


HTML = r"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Mask Experiment Visual Analyzer</title>
  <style>
    body { margin: 0; font-family: Arial, "Malgun Gothic", sans-serif; background: #f5f5f2; color: #202124; }
    header { padding: 18px 24px; background: #202124; color: white; }
    h1 { margin: 0; font-size: 22px; }
    main { padding: 18px 24px 40px; }
    .drop { border: 3px dashed #4b88d8; background: white; padding: 26px; text-align: center; font-size: 18px; }
    .drop.drag { background: #e9f2ff; border-color: #0b63ce; }
    .controls, .grid, .wide { margin-top: 16px; }
    .controls { display: flex; flex-wrap: wrap; gap: 18px; align-items: center; background: white; padding: 14px; border: 1px solid #ddd; }
    .control { min-width: 220px; }
    input[type="range"] { width: 170px; vertical-align: middle; }
    .grid { display: grid; grid-template-columns: repeat(3, minmax(260px, 1fr)); gap: 14px; }
    .card { background: white; border: 1px solid #ddd; padding: 14px; }
    .card h2 { margin: 0 0 10px; font-size: 17px; }
    .metric { display: flex; justify-content: space-between; gap: 12px; border-top: 1px solid #eee; padding: 8px 0; }
    .metric:first-of-type { border-top: 0; }
    .value { font-weight: bold; }
    .ok { color: #128023; font-weight: bold; }
    .bad { color: #b42318; font-weight: bold; }
    .muted { color: #777; font-size: 13px; }
    canvas { width: 100%; height: 240px; background: white; border: 1px solid #e0e0e0; }
    table { width: 100%; border-collapse: collapse; background: white; font-size: 13px; }
    th, td { border: 1px solid #ddd; padding: 6px 8px; text-align: left; }
    th { background: #e8eef7; position: sticky; top: 0; }
    .wide { background: white; border: 1px solid #ddd; padding: 14px; }
    .scroll { max-height: 330px; overflow: auto; }
    @media (max-width: 1000px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <header>
    <h1>Mask Experiment Visual Analyzer</h1>
    <div class="muted">experiment_results.csv 또는 Export Labels에서 나온 Excel 호환 .xls 파일을 드래그하세요.</div>
  </header>
  <main>
    <div id="drop" class="drop">
      파일을 여기에 드래그하거나 클릭해서 선택
      <input id="fileInput" type="file" accept=".csv,.xls,.xml" hidden>
      <div id="fileName" class="muted"></div>
    </div>

    <div class="controls">
      <div class="control">
        유의수준 alpha:
        <input id="alpha" type="range" min="0.01" max="0.10" step="0.01" value="0.05">
        <span id="alphaValue">0.05</span>
      </div>
      <div class="control">
        점 성공 기준:
        <input id="pxThreshold" type="range" min="5" max="50" step="1" value="15">
        <span id="pxValue">15 px</span>
      </div>
      <div class="control">
        회전각 bin:
        <input id="angleBin" type="range" min="15" max="60" step="15" value="30">
        <span id="binValue">30 deg</span>
      </div>
      <div class="control">
        <label><input id="includeDirection" type="checkbox"> 방향 분석 포함</label>
      </div>
    </div>

    <section class="grid">
      <div class="card" id="directionCard"><h2>방향 판별</h2></div>
      <div class="card" id="sideCard"><h2>LEFT / RIGHT</h2></div>
      <div class="card" id="badCard"><h2>GOOD / BAD</h2></div>
    </section>

    <section class="grid">
      <div class="card">
        <h2>점별 평균 오차</h2>
        <canvas id="pointCanvas" width="700" height="300"></canvas>
      </div>
      <div class="card">
        <h2>회전각 구간별 점 오차</h2>
        <canvas id="angleCanvas" width="700" height="300"></canvas>
      </div>
      <div class="card">
        <h2>요약</h2>
        <div id="pointSummary"></div>
      </div>
    </section>

    <section class="wide">
      <h2>점 위치 오차가 가장 큰 이미지</h2>
      <div class="scroll"><table id="worstTable"></table></div>
    </section>

    <section class="wide">
      <h2>방향 / 좌우 / BAD 오분류 샘플</h2>
      <div class="scroll"><table id="missTable"></table></div>
    </section>
  </main>

<script>
let rows = [];

const drop = document.getElementById("drop");
const fileInput = document.getElementById("fileInput");
const alpha = document.getElementById("alpha");
const pxThreshold = document.getElementById("pxThreshold");
const angleBin = document.getElementById("angleBin");
const includeDirection = document.getElementById("includeDirection");

drop.addEventListener("click", () => fileInput.click());
drop.addEventListener("dragover", e => { e.preventDefault(); drop.classList.add("drag"); });
drop.addEventListener("dragleave", () => drop.classList.remove("drag"));
drop.addEventListener("drop", e => {
  e.preventDefault();
  drop.classList.remove("drag");
  if (e.dataTransfer.files.length) loadFile(e.dataTransfer.files[0]);
});
fileInput.addEventListener("change", e => {
  if (e.target.files.length) loadFile(e.target.files[0]);
});
[alpha, pxThreshold, angleBin, includeDirection].forEach(el => el.addEventListener("input", render));

function loadFile(file) {
  document.getElementById("fileName").textContent = file.name;
  const reader = new FileReader();
  reader.onload = e => {
    const text = e.target.result;
    rows = file.name.toLowerCase().endsWith(".csv") ? parseCsv(text) : parseXmlSpreadsheet(text);
    render();
  };
  reader.readAsText(file, "utf-8");
}

function parseCsv(text) {
  const lines = text.replace(/^\uFEFF/, "").split(/\r?\n/).filter(x => x.length);
  if (!lines.length) return [];
  const headers = parseCsvLine(lines[0]);
  return lines.slice(1).map(line => {
    const cells = parseCsvLine(line);
    const obj = {};
    headers.forEach((h, i) => obj[h] = cells[i] || "");
    return obj;
  });
}

function parseCsvLine(line) {
  const out = [];
  let cur = "", quoted = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (quoted) {
      if (ch === '"' && line[i + 1] === '"') { cur += '"'; i++; }
      else if (ch === '"') quoted = false;
      else cur += ch;
    } else {
      if (ch === '"') quoted = true;
      else if (ch === ',') { out.push(cur); cur = ""; }
      else cur += ch;
    }
  }
  out.push(cur);
  return out;
}

function parseXmlSpreadsheet(text) {
  const doc = new DOMParser().parseFromString(text, "application/xml");
  const rowsXml = Array.from(doc.getElementsByTagNameNS("urn:schemas-microsoft-com:office:spreadsheet", "Row"));
  const rowsData = rowsXml.map(row => Array.from(row.getElementsByTagNameNS("urn:schemas-microsoft-com:office:spreadsheet", "Data")).map(d => d.textContent || ""));
  if (!rowsData.length) return [];
  const headers = rowsData[0];
  return rowsData.slice(1).map(cells => {
    const obj = {};
    headers.forEach((h, i) => obj[h] = cells[i] || "");
    return obj;
  });
}

function norm(v) { return String(v ?? "").trim(); }
function upper(v) { return norm(v).toUpperCase(); }
function num(v) { const n = parseFloat(norm(v)); return Number.isFinite(n) ? n : null; }
function fmt(v, d=3) { return v === null || v === undefined || Number.isNaN(v) ? "" : Number(v).toFixed(d); }
function pct(v) { return v === null || v === undefined ? "" : (v * 100).toFixed(1) + "%"; }
function badNorm(v) {
  const t = upper(v);
  if (["BAD", "TRUE", "1"].includes(t)) return "BAD";
  if (["GOOD", "OK", "FALSE", "0"].includes(t)) return "GOOD";
  return "";
}

function binomGreater(k, n, p0=0.5) {
  if (!n) return null;
  let p = 0;
  for (let i = k; i <= n; i++) p += comb(n, i) * Math.pow(p0, i) * Math.pow(1-p0, n-i);
  return Math.min(1, p);
}
function comb(n, k) {
  if (k < 0 || k > n) return 0;
  k = Math.min(k, n-k);
  let c = 1;
  for (let i = 1; i <= k; i++) c = c * (n - k + i) / i;
  return c;
}

function binarySummary(predCol, trueCol, normalizer=upper, validTruth=null) {
  let n = 0, k = 0, misses = [];
  rows.forEach(r => {
    const truth = normalizer(r[trueCol]);
    const pred = normalizer(r[predCol]);
    if (!truth) return;
    if (validTruth && !validTruth.includes(truth)) return;
    n++;
    if (pred === truth) k++;
    else misses.push(r);
  });
  return {n, k, acc: n ? k/n : null, p: n ? binomGreater(k,n) : null, misses};
}

function pointStats(threshold) {
  const perPoint = Array.from({length: 6}, () => []);
  const imageStats = [];
  rows.forEach(r => {
    const errs = [];
    for (let i=1; i<=6; i++) {
      let e = num(r[`p${i}_error_px`]);
      if (e === null) {
        const px = num(r[`p${i}_x`]), py = num(r[`p${i}_y`]);
        const tx = num(r[`true_p${i}_x`]), ty = num(r[`true_p${i}_y`]);
        if (px !== null && py !== null && tx !== null && ty !== null) {
          e = Math.hypot(px - tx, py - ty);
        }
      }
      if (e !== null) { perPoint[i-1].push(e); errs.push(e); }
    }
    if (errs.length) {
      imageStats.push({
        row: r,
        count: errs.length,
        mean: avg(errs),
        max: Math.max(...errs),
        allWithin: errs.length === 6 && errs.every(e => e <= threshold)
      });
    }
  });
  return {perPoint, imageStats};
}

function avg(a) { return a.length ? a.reduce((x,y)=>x+y,0)/a.length : null; }
function median(a) {
  if (!a.length) return null;
  const s = [...a].sort((x,y)=>x-y), m = Math.floor(s.length/2);
  return s.length % 2 ? s[m] : (s[m-1] + s[m]) / 2;
}

function renderMetricCard(id, title, s, alphaValue) {
  const significant = s.p !== null && s.p < alphaValue;
  document.getElementById(id).innerHTML = `
    <h2>${title}</h2>
    <div class="metric"><span>N</span><span class="value">${s.n}</span></div>
    <div class="metric"><span>맞은 수</span><span class="value">${s.k}</span></div>
    <div class="metric"><span>정확도</span><span class="value">${pct(s.acc)}</span></div>
    <div class="metric"><span>p-value (H1 &gt; 0.5)</span><span class="value">${fmt(s.p, 5)}</span></div>
    <div class="metric"><span>판정</span><span class="${significant ? "ok" : "bad"}">${significant ? "유의함" : "유의하지 않음"}</span></div>
  `;
}

function render() {
  document.getElementById("alphaValue").textContent = alpha.value;
  document.getElementById("pxValue").textContent = pxThreshold.value + " px";
  document.getElementById("binValue").textContent = angleBin.value + " deg";
  if (!rows.length) return;

  const alphaValue = Number(alpha.value);
  const threshold = Number(pxThreshold.value);
  const useDirection = includeDirection.checked;
  const dir = useDirection ? binarySummary("pred_front_sign", "true_front_sign", upper, ["1", "-1"]) : {n: 0, k: 0, acc: null, p: null, misses: []};
  const side = binarySummary("pred_shoe_side", "true_shoe_side", upper);
  const bad = binarySummary("is_bad", "true_bad", badNorm);
  if (useDirection) {
    renderMetricCard("directionCard", "방향 판별", dir, alphaValue);
  } else {
    document.getElementById("directionCard").innerHTML = `
      <h2>방향 판별</h2>
      <div class="metric"><span>상태</span><span class="value">분석 제외</span></div>
      <div class="muted">방향 라벨은 기본 엑셀 평가에서 제외됩니다. 필요할 때만 위의 체크박스를 켜세요.</div>
    `;
  }
  renderMetricCard("sideCard", "LEFT / RIGHT", side, alphaValue);
  renderMetricCard("badCard", "GOOD / BAD", bad, alphaValue);

  const ps = pointStats(threshold);
  renderPointSummary(ps, threshold);
  drawBar("pointCanvas", ps.perPoint.map(avg), ["P1","P2","P3","P4","P5","P6"], "평균 오차(px)");
  drawBar("angleCanvas", angleGroups(ps.imageStats, Number(angleBin.value)), null, "평균 오차(px)");
  renderWorstTable(ps.imageStats);
  const missItems = [
    ...(useDirection ? dir.misses.map(r => [r, "direction"]) : []),
    ...side.misses.map(r => [r, "side"]),
    ...bad.misses.map(r => [r, "bad"])
  ];
  renderMissTable(missItems);
}

function renderPointSummary(ps, threshold) {
  const allErrors = ps.perPoint.flat();
  const full = ps.imageStats.filter(x => x.count === 6);
  const success = full.filter(x => x.allWithin).length;
  const pointAvg = ps.perPoint.map(avg);
  let worstPoint = -1, worstPointValue = -1;
  pointAvg.forEach((v,i) => { if (v !== null && v > worstPointValue) { worstPointValue = v; worstPoint = i + 1; } });
  const worstImage = [...ps.imageStats].sort((a,b)=>b.mean-a.mean)[0];
  document.getElementById("pointSummary").innerHTML = `
    <div class="metric"><span>수동 점 라벨 이미지</span><span class="value">${ps.imageStats.length}</span></div>
    <div class="metric"><span>6점 전체 라벨 이미지</span><span class="value">${full.length}</span></div>
    <div class="metric"><span>점 전체 개수</span><span class="value">${allErrors.length}</span></div>
    <div class="metric"><span>평균 오차</span><span class="value">${fmt(avg(allErrors))} px</span></div>
    <div class="metric"><span>중앙값 오차</span><span class="value">${fmt(median(allErrors))} px</span></div>
    <div class="metric"><span>이미지 success@${threshold}px</span><span class="value">${full.length ? pct(success/full.length) : ""}</span></div>
    <div class="metric"><span>가장 심한 점</span><span class="value">${worstPoint > 0 ? "P" + worstPoint + " (" + fmt(worstPointValue) + "px)" : ""}</span></div>
    <div class="metric"><span>가장 심한 이미지</span><span class="value">${worstImage ? fileName(worstImage.row) + " (" + fmt(worstImage.mean) + "px)" : ""}</span></div>
  `;
}

function angleGroups(imageStats, binSize) {
  const groups = new Map();
  imageStats.forEach(x => {
    const angle = num(x.row.rotation_angle_deg);
    if (angle === null) return;
    const bucket = Math.floor((angle + 180) / binSize) * binSize - 180;
    const label = `${bucket}~${bucket + binSize}`;
    if (!groups.has(label)) groups.set(label, []);
    groups.get(label).push(x.mean);
  });
  return Array.from(groups.entries()).map(([label, vals]) => ({label, value: avg(vals)}));
}

function drawBar(canvasId, valuesOrGroups, labels, title) {
  const canvas = document.getElementById(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);
  let data = Array.isArray(valuesOrGroups) && valuesOrGroups.length && typeof valuesOrGroups[0] === "object"
    ? valuesOrGroups
    : valuesOrGroups.map((v,i)=>({label: labels[i], value: v}));
  data = data.filter(d => d.value !== null && d.value !== undefined);
  if (!data.length) { ctx.fillText("라벨링된 점 오차 데이터가 없습니다.", 20, 40); return; }
  const max = Math.max(...data.map(d=>d.value), 1);
  const left = 50, top = 20, bottom = 250, width = canvas.width - 80;
  const barW = width / data.length * 0.65;
  ctx.fillStyle = "#111"; ctx.font = "14px Arial"; ctx.fillText(title, left, 16);
  ctx.strokeStyle = "#999"; ctx.beginPath(); ctx.moveTo(left,bottom); ctx.lineTo(canvas.width-20,bottom); ctx.stroke();
  data.forEach((d,i) => {
    const x = left + (i + 0.2) * width / data.length;
    const h = (bottom - top) * d.value / max;
    ctx.fillStyle = "#3b82f6"; ctx.fillRect(x, bottom - h, barW, h);
    ctx.fillStyle = "#111"; ctx.fillText(fmt(d.value,1), x, bottom - h - 5);
    ctx.save(); ctx.translate(x + barW/2, bottom + 12); ctx.rotate(-0.5); ctx.fillText(d.label, 0, 0); ctx.restore();
  });
}

function renderWorstTable(imageStats) {
  const top = [...imageStats].sort((a,b)=>b.mean-a.mean).slice(0, 30);
  const rowsHtml = top.map(x => `
    <tr><td>${fileName(x.row)}</td><td>${fmt(x.mean)}</td><td>${fmt(x.max)}</td><td>${x.count}</td><td>${norm(x.row.rotation_angle_deg)}</td><td>${norm(x.row.model_group)}</td><td>${norm(x.row.pred_shoe_side)}</td><td>${norm(x.row.matched_image_path)}</td></tr>
  `).join("");
  document.getElementById("worstTable").innerHTML = `<tr><th>file</th><th>mean px</th><th>max px</th><th>points</th><th>angle</th><th>group</th><th>pred side</th><th>image path</th></tr>${rowsHtml}`;
}

function renderMissTable(items) {
  const rowsHtml = items.slice(0, 80).map(([r,type]) => `
    <tr><td>${type}</td><td>${fileName(r)}</td><td>${norm(r.pred_front_sign)}</td><td>${norm(r.true_front_sign)}</td><td>${norm(r.pred_shoe_side)}</td><td>${norm(r.true_shoe_side)}</td><td>${norm(r.is_bad)}</td><td>${norm(r.true_bad)}</td><td>${norm(r.rotation_angle_deg)}</td><td>${norm(r.matched_image_path)}</td></tr>
  `).join("");
  document.getElementById("missTable").innerHTML = `<tr><th>type</th><th>file</th><th>pred front</th><th>true front</th><th>pred side</th><th>true side</th><th>pred bad</th><th>true bad</th><th>angle</th><th>image path</th></tr>${rowsHtml}`;
}

function fileName(r) {
  return norm(r.file_name) || norm(r.file_path).split(/[\\/]/).pop();
}
</script>
</body>
</html>
"""


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.end_headers()
        self.wfile.write(HTML.encode("utf-8"))

    def log_message(self, format, *args):
        return


def main():
    with socketserver.TCPServer(("127.0.0.1", 0), Handler) as httpd:
        port = httpd.server_address[1]
        url = f"http://127.0.0.1:{port}/"
        print("Mask Experiment Visual Analyzer")
        print("브라우저가 열리면 experiment_results.csv 또는 .xls 파일을 드래그하세요.")
        print(url)
        threading.Timer(0.3, lambda: webbrowser.open(url)).start()
        httpd.serve_forever()


if __name__ == "__main__":
    main()
