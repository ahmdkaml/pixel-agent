let currentMode = "split"; // 'design' | 'split' | 'render'
let splitPercent = 50;
let isDragging = false;

// 1. Canvas Viewport Mode Switcher
function setCanvasMode(mode) {
  currentMode = mode;
  document.querySelectorAll(".mode-btn").forEach((b) => {
    b.classList.toggle("active", b.dataset.mode === mode);
  });

  const renderLayer = document.getElementById("renderLayer");
  const splitSlider = document.getElementById("splitSlider");

  if (mode === "design") {
    renderLayer.style.clipPath = "inset(0 0 0 100%)";
    splitSlider.classList.add("hidden");
  } else if (mode === "render") {
    renderLayer.style.clipPath = "inset(0 0 0 0%)";
    splitSlider.classList.add("hidden");
  } else {
    // Split Diff Mode
    splitSlider.classList.remove("hidden");
    updateSplitPosition(splitPercent);
  }
}

// 2. Interactive Split Slider Dragging
const stage = document.getElementById("canvasStage");
const slider = document.getElementById("splitSlider");

slider.addEventListener("pointerdown", (e) => {
  if (currentMode !== "split") return;

  isDragging = true;
  slider.setPointerCapture(e.pointerId);
  document.body.style.cursor = "ew-resize";
});

slider.addEventListener("pointermove", (e) => {
  if (!isDragging || currentMode !== "split") return;

  const rect = stage.getBoundingClientRect();
  const offsetX = e.clientX - rect.left;

  let pct = (offsetX / rect.width) * 100;
  pct = Math.max(0, Math.min(100, pct));

  updateSplitPosition(pct);
});

slider.addEventListener("pointerup", (e) => {
  if (!isDragging) return;

  isDragging = false;

  if (slider.hasPointerCapture(e.pointerId)) {
    slider.releasePointerCapture(e.pointerId);
  }

  document.body.style.cursor = "default";
});

slider.addEventListener("pointercancel", (e) => {
  if (!isDragging) return;

  isDragging = false;

  if (slider.hasPointerCapture(e.pointerId)) {
    slider.releasePointerCapture(e.pointerId);
  }

  document.body.style.cursor = "default";
});

function updateSplitPosition(pct) {
  splitPercent = pct;
  stage.style.setProperty("--split-pos", pct);
  document.getElementById("renderLayer").style.clipPath = `inset(0 0 0 ${pct}%)`;
}

// 3. Collapsible Code Drawer
function toggleDrawer() {
  const drawer = document.getElementById("codeDrawer");
  drawer.classList.toggle("collapsed");
}

function switchCodeTab(tabName) {
  document.querySelectorAll(".drawer-tab").forEach((btn) => {
    btn.classList.toggle("active", btn.dataset.tab === tabName);
  });

  const htmlBlock = document.getElementById("htmlBlock");
  const cssBlock = document.getElementById("cssBlock");

  if (tabName === "html") {
    htmlBlock.classList.remove("hidden");
    cssBlock.classList.add("hidden");
  } else {
    htmlBlock.classList.add("hidden");
    cssBlock.classList.remove("hidden");
  }
}

// 4. WebView2 C# Host Interop API
function postToHost(action, payload = {}) {
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage({ action, ...payload });
  } else {
    console.log(`[WebView2 Mock Post] Action: ${action}`, payload);
  }
}

function triggerOpenDesign() {
  postToHost("open_design_dialog");
}

function triggerRunPipeline() {
  postToHost("run_pipeline");
}

function triggerExportZip() {
  postToHost("export_zip");
}

function notifyOpChange(opName, isEnabled) {
  postToHost("toggle_op", { opName, isEnabled });
}

function applyAction(actionType) {
  const target = document.getElementById("targetChip").innerText;
  postToHost("apply_action", { actionType, target });
}

// 5. Functions Called by C# via ExecuteScriptAsync

/**
 * Loads a design image (bypassing the "no design images" notice).
 */
window.setDesignImage = function (base64OrUrl) {
  const notice = document.getElementById("emptyDesignNotice");
  const img = document.getElementById("designImage");

  if (!base64OrUrl) {
    notice.classList.remove("hidden");
    img.classList.add("hidden");
    img.src = "";
    return;
  }

  notice.classList.add("hidden");
  img.classList.remove("hidden");
  img.src = base64OrUrl;
};

/**
 * Injects rendered HTML + CSS into the live frame & updates code deck.
 */
window.setRenderedContent = function (htmlString, cssString, matchPercent = null) {
  const iframe = document.getElementById("renderFrame");
  const doc = iframe.contentDocument || iframe.contentWindow.document;

  doc.open();
  doc.write(`
    <!DOCTYPE html>
    <html>
      <head>
        <style>${cssString || ""}</style>
      </head>
      <body>${htmlString || ""}</body>
    </html>
  `);
  doc.close();

  document.getElementById("htmlBlock").textContent =
    htmlString || "<!-- Empty -->";

  document.getElementById("cssBlock").textContent =
    cssString || "/* Empty */";

  if (matchPercent !== null) {
    document.getElementById("matchBadge").innerText =
      `${matchPercent}% MATCH`;
  }
};

/**
 * Populates detected elements in the left panel layer tree.
 */
window.setElementTree = function (elements) {
  const container = document.getElementById("layerTree");
  container.innerHTML = "";

  if (!elements || elements.length === 0) {
    container.innerHTML = '<div class="empty-layers">No elements detected yet</div>';
    return;
  }

  elements.forEach((el) => {
    const node = document.createElement("div");
    node.className = "layer-node";
    node.innerText = `${el.type === "container" ? "v" : "-"} ${el.id}`;
    node.onclick = () => selectElement(el);
    container.appendChild(node);
  });
};

function selectElement(el) {
  document.getElementById("targetChip").innerText = el.id;
  document.getElementById("propWidth").value = `${el.w}px`;
  document.getElementById("propHeight").value = `${el.h}px`;
  document.getElementById("propX").value = `${el.x}px`;
  document.getElementById("propY").value = `${el.y}px`;

  document.getElementById("styleBg").value = el.styles?.["background-color"] || "";
  document.getElementById("styleBorder").value = el.styles?.["border"] || "";
  document.getElementById("styleShadow").value = el.styles?.["box-shadow"] || "";
  document.getElementById("styleRadius").value = el.styles?.["border-radius"] || "";
  document.getElementById("stylePadding").value = el.styles?.["padding"] || "";

  document.querySelectorAll(".layer-node").forEach((node) => {
    node.classList.toggle("active", node.innerText.includes(el.id));
  });
}
