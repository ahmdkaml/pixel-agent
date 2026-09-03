let currentMode = "split"; // 'design' | 'split' | 'render'
let splitPercent = 50;
let isDragging = false;
let designDimensions = { width: 0, height: 0 };
let editDebounceTimer = null;
const elementsCache = new Map();
const loadedImages = new Map();

// 1. Dynamic Scaling Engine
function updateStageDimensions() {
  const stage = document.getElementById("canvasStage");
  const viewport = document.getElementById("canvasViewport");
  const renderFrame = document.getElementById("renderFrame");

  if (!designDimensions.width || !designDimensions.height) {
    stage.style.width = "520px";
    stage.style.height = "600px";
    renderFrame.style.width = "100%";
    renderFrame.style.height = "100%";
    renderFrame.style.transform = "none";
    return;
  }

  const origW = designDimensions.width;
  const origH = designDimensions.height;

  // Viewport padding buffer (24px left + 24px right)
  const paddingBuffer = 48;
  const availW = Math.max(100, viewport.clientWidth - paddingBuffer);

  // 1. Never scale up more than 1x original image dimensions
  const scaleFactor = Math.min(1.0, availW / origW);

  // 2. Uniform scaling: height scales by the width factor
  const scaledW = Math.round(origW * scaleFactor);
  const scaledH = Math.round(origH * scaleFactor);

  stage.style.width = `${scaledW}px`;
  stage.style.height = `${scaledH}px`;

  // 3. Render iframe at 1:1 original dimensions, then scale visually via CSS transform
  renderFrame.style.width = `${origW}px`;
  renderFrame.style.height = `${origH}px`;
  renderFrame.style.transform = `scale(${scaleFactor})`;
  renderFrame.style.transformOrigin = "top left";
}

const canvasViewport = document.getElementById("canvasViewport");
const canvasResizeObserver = new ResizeObserver(() => {
  updateStageDimensions();
});
canvasResizeObserver.observe(canvasViewport);

// 2. Canvas Viewport Mode Switcher
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
    splitSlider.classList.remove("hidden");
    updateSplitPosition(splitPercent);
  }
}

// 3. Interactive Split Slider Dragging (Pointer Events API)
const stage = document.getElementById("canvasStage");
const slider = document.getElementById("splitSlider");

slider.addEventListener("pointerdown", (e) => {
  if (currentMode !== "split") return;

  isDragging = true;
  document.body.classList.add("is-dragging");
  slider.setPointerCapture(e.pointerId);
  document.body.style.cursor = "ew-resize";
});

slider.addEventListener("pointermove", (e) => {
  if (!isDragging || currentMode !== "split") return;

  const rect = stage.getBoundingClientRect();
  if (rect.width === 0) return;

  const offsetX = e.clientX - rect.left;
  const pct = Math.max(0, Math.min(100, (offsetX / rect.width) * 100));

  updateSplitPosition(pct);
});

function stopDragging(e) {
  if (!isDragging) return;
  isDragging = false;
  document.body.classList.remove("is-dragging");

  if (slider.hasPointerCapture(e.pointerId)) {
    slider.releasePointerCapture(e.pointerId);
  }
  document.body.style.cursor = "default";
}

slider.addEventListener("pointerup", stopDragging);
slider.addEventListener("pointercancel", stopDragging);

function updateSplitPosition(pct) {
  splitPercent = pct;
  stage.style.setProperty("--split-pos", pct);
  document.getElementById("renderLayer").style.clipPath = `inset(0 0 0 ${pct}%)`;
}

// 4. Collapsible Code Drawer & Tab Navigation
function toggleDrawer() {
  document.getElementById("codeDrawer").classList.toggle("collapsed");
}

function switchCodeTab(tabName) {
  document.querySelectorAll(".drawer-tab").forEach((btn) => {
    btn.classList.toggle("active", btn.dataset.tab === tabName);
  });

  const isHtml = tabName === "html";
  document.getElementById("htmlBlock").classList.toggle("hidden", !isHtml);
  document.getElementById("cssBlock").classList.toggle("hidden", isHtml);
}

// 5. Two-Way Live Code Editor Synchronization
const htmlBlock = document.getElementById("htmlBlock");
const cssBlock = document.getElementById("cssBlock");

function syncLiveFrame(rawHtml, rawCss) {
  const iframe = document.getElementById("renderFrame");

  const html = resolveImageReferences(rawHtml);

  iframe.srcdoc = `
    <!DOCTYPE html>
    <html>
      <head>
        <meta charset="utf-8">
        <style>
          *, *::before, *::after { box-sizing: border-box; }
          body { margin: 0; padding: 0; }
          ${rawCss || ""}
        </style>
      </head>
      <body>${html || ""}</body>
    </html>
  `;
}

function resolveImageReferences(html) {
  if (!html) {
    return html;
  }

  return html.replace(
    /(<img\b[^>]*\bsrc=["'])([^"']+)(["'][^>]*>)/gi,
    (match, prefix, src, suffix) => {
      const image = loadedImages.get(src);

      if (!image) {
        return match;
      }

      return `${prefix}${image}${suffix}`;
    }
  );
}

function handleCodeInput() {
  clearTimeout(editDebounceTimer);
  editDebounceTimer = setTimeout(() => {
    const rawHtml = htmlBlock.innerText;
    const rawCss = cssBlock.innerText;

    syncLiveFrame(rawHtml, rawCss);
    postToHost("code_edited", { html: rawHtml, css: rawCss });
  }, 150);
}

htmlBlock.addEventListener("input", handleCodeInput);
cssBlock.addEventListener("input", handleCodeInput);

// 6. WebView2 C# Host Interop API
function postToHost(action, payload = {}) {
  if (window.chrome?.webview?.postMessage) {
    window.chrome.webview.postMessage({ action, ...payload });
  } else {
    console.log(`[WebView2 Mock Post] Action: ${action}`, payload);
  }
}

function triggerOpenDesign() { postToHost("open_design_dialog"); }
function triggerRunPipeline() { postToHost("run_pipeline"); }
function triggerExportZip() { postToHost("export_zip"); }
function triggerLoadImages() { postToHost("load_images"); }
function notifyOpChange(opName, isEnabled) { postToHost("toggle_op", { opName, isEnabled }); }

function applyAction(actionType) {
  const target = document.getElementById("targetChip").innerText;
  postToHost("apply_action", { actionType, target });
}

// 7. Host Invocation Functions (Called from C# via ExecuteScriptAsync)
window.setDesignImage = function (base64OrUrl) {
  const notice = document.getElementById("emptyDesignNotice");
  const img = document.getElementById("designImage");

  if (!base64OrUrl) {
    notice.classList.remove("hidden");
    img.classList.add("hidden");
    img.src = "";
    designDimensions = { width: 0, height: 0 };
    updateStageDimensions();
    return;
  }

  notice.classList.add("hidden");
  img.classList.remove("hidden");

  img.onload = () => {
    designDimensions = {
      width: img.naturalWidth,
      height: img.naturalHeight,
    };
    updateStageDimensions();
  };

  img.onerror = () => {
    notice.classList.remove("hidden");
    img.classList.add("hidden");
    designDimensions = { width: 0, height: 0 };
    updateStageDimensions();
    console.error("Failed to load design image.");
  };

  img.src = base64OrUrl;
};

window.setRenderedContent = function (htmlString, cssString, matchPercent = null) {
  // Preserve editor buffer if the user is actively typing in a block
  if (document.activeElement !== htmlBlock) {
    htmlBlock.textContent = htmlString || "<!-- Empty -->";
  }
  if (document.activeElement !== cssBlock) {
    cssBlock.textContent = cssString || "/* Empty */";
  }

  syncLiveFrame(htmlString, cssString);

  if (matchPercent !== null) {
    document.getElementById("matchBadge").innerText = `${matchPercent}% MATCH`;
  }
};

window.addImages = function (images) {
  const container = document.getElementById("imageList");

  images.forEach((image) => {
    loadedImages.set(image.Name, image.Data);

    const item = document.createElement("div");
    item.className = "asset-item";
    item.innerText = image.Name;

    container.appendChild(item);
  });

  document.getElementById("assetCount").innerText = loadedImages.size;
};

// 8. Layer Tree & Inspector Binding
const layerTreeContainer = document.getElementById("layerTree");

layerTreeContainer.addEventListener("click", (e) => {
  const node = e.target.closest(".layer-node");
  if (!node) return;

  const el = elementsCache.get(node.dataset.id);
  if (el) selectElement(el);
});

window.setElementTree = function (elements) {
  layerTreeContainer.innerHTML = "";
  elementsCache.clear();

  if (!elements || elements.length === 0) {
    layerTreeContainer.innerHTML = '<div class="empty-layers">No elements detected yet</div>';
    return;
  }

  const fragment = document.createDocumentFragment();

  elements.forEach((el) => {
    elementsCache.set(String(el.id), el);

    const node = document.createElement("div");
    node.className = "layer-node";
    node.dataset.id = el.id;
    node.innerText = `${el.type === "container" ? "v" : "-"} ${el.id}`;
    fragment.appendChild(node);
  });

  layerTreeContainer.appendChild(fragment);
};

function selectElement(el) {
  document.getElementById("targetChip").innerText = el.id;
  document.getElementById("propWidth").value = `${el.w ?? 0}px`;
  document.getElementById("propHeight").value = `${el.h ?? 0}px`;
  document.getElementById("propX").value = `${el.x ?? 0}px`;
  document.getElementById("propY").value = `${el.y ?? 0}px`;

  document.getElementById("styleBg").value = el.styles?.["background-color"] || "";
  document.getElementById("styleBorder").value = el.styles?.["border"] || "";
  document.getElementById("styleShadow").value = el.styles?.["box-shadow"] || "";
  document.getElementById("styleRadius").value = el.styles?.["border-radius"] || "";
  document.getElementById("stylePadding").value = el.styles?.["padding"] || "";

  document.querySelectorAll(".layer-node").forEach((node) => {
    node.classList.toggle("active", node.dataset.id === String(el.id));
  });
}
