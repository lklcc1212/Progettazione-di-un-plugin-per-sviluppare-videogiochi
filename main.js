/* ---------- 静态类：风格管理器 ---------- */
let currentStyle = undefined;

class stylesPathsManager {
  static #stylesPathMap = {
    easyStyles: "styles/easyStyles.css",
    style1: "styles/styles1.css",
    style2: "styles/styles2.css",
  };
  /**
   * @param {string} style
   * @returns {string}
   */
  static getStylePath(style) {
    const isStyle1 = style === "style1";
    if (window.location.hash === "" || !isStyle1)
      if (style === "style1") {
        window.location.hash = localStorage.getItem("savedHash") || "#intro";
        //滑到顶部
        document
          .getElementById(window.location.hash.replace("#", ""))
          .parentElement.scrollTo(0, 0);
      } else {
        window.location.hash = "";
      }
    return this.#stylesPathMap[style];
  }
}

/* ---------- 应用已保存的数据(语言，主题和风格) ---------- */
function applySavedDatas() {
  // 风格应用
  const savedStyle = localStorage.getItem("savedStyle") || "style1";
  document.getElementById("style").href =
    stylesPathsManager.getStylePath(savedStyle);
  currentStyle = savedStyle;

  // 主题应用
  const savedTheme = localStorage.getItem("theme");
  const themeBtn = document.getElementById("theme-toggle");
  if (savedTheme === "dark") {
    document.body.classList.add("dark");
    themeBtn.textContent = "🌙";
  } else {
    document.body.classList.remove("dark");
    themeBtn.textContent = "☀️";
  }
}

applySavedDatas();

/* ---------- 暗/亮主题切换 ---------- */
const themeBtn = document.getElementById("theme-toggle");
themeBtn?.addEventListener("click", toggleTheme);

function toggleTheme() {
  const isDark = document.body.classList.toggle("dark");
  themeBtn.textContent = isDark ? "🌙" : "☀️";
  localStorage.setItem("theme", isDark ? "dark" : "light");
}

/* ---------- 显示侧边面板的按钮 ---------- */
function toggleSidebar() {
  document.getElementById("sidebar").classList.toggle("show");
  document.getElementById("translucentScreen").classList.toggle("show");
}

document
  .getElementById("translucentScreen")
  .addEventListener("click", toggleSidebar);

document
  .getElementById("showSidebarBtn")
  .addEventListener("click", toggleSidebar);

/* ---------- 侧边栏链接点击事件 ---------- */
function updateActiveLinkByHash() {
  if (currentStyle != "style1") {
    window.location.hash = "";
    return;
  }

  //如果 hash === ""，则...。（window.location.hash === "" ? "#intro" : window.location.hash）
  let currentHash = window.location.hash || "#intro";
  if (window.location.hash === "") {
    window.location.hash = currentHash;
  }

  localStorage.setItem("savedHash", currentHash);

  let found = false;
  const links = document.querySelectorAll("#sidebar a");
  for (let link of links) {
    if (link.getAttribute("href") === currentHash) {
      link.classList.add("active");
      found = true;
    } else {
      link.classList.remove("active");
    }
  }
  // for (let i = 0; i < links.length; i++) {
  //   const link = /** @type {HTMLLinkElement} */ (links[i]);
  //   if (link.getAttribute("href") === currentHash) {
  //     link.classList.add("active");
  //     found = true;
  //   } else {
  //     link.classList.remove("active");
  //   }
  // }

  if (!found) {
    window.location.hash = "#page-not-found";
  }
}

updateActiveLinkByHash();
window.addEventListener("hashchange", updateActiveLinkByHash);

/* ---------- 视频懒加载 ---------- */
const videoObserver = new IntersectionObserver((entries) => {
  entries.forEach((e) => {
    if (e.isIntersecting) {
      const video = /** @type {HTMLVideoElement} */ (e.target);
      video.src = video.dataset.src;
      video.load();
      videoObserver.unobserve(video);
    }
  });
});

document.querySelectorAll("video").forEach((v) => videoObserver.observe(v));

/* ---------- 清理器：页面卸载时断开所有监听 & 观察器 ---------- */
function disconnectObserver() {
  langObserver.disconnect();
  videoObserver.disconnect();
  window.removeEventListener("beforeunload", disconnectObserver);
}

window.addEventListener("beforeunload", disconnectObserver);

/* ---------- 切换样式按钮 ---------- */
const changeStylesBtn = document.getElementById("change-style-img");
changeStylesBtn.addEventListener("click", changeStyle);

function changeStyle() {
  if (!currentStyle) {
    currentStyle = localStorage.getItem("savedStyle") || "easyStyles";
  }

  const styles = ["easyStyles", "style1", "style2"];
  const currentIndex = styles.indexOf(currentStyle);
  const nextStyle = styles[(currentIndex + 1) % styles.length];

  const styleLink = /** @type {HTMLLinkElement} */ (
    document.getElementById("style")
  );
  styleLink.href = stylesPathsManager.getStylePath(nextStyle);
  currentStyle = nextStyle;
  localStorage.setItem("savedStyle", nextStyle);
}
