(function () {
  var key = "ml-theme";
  var root = document.documentElement;
  var saved = localStorage.getItem(key);
  if (saved === "dark") root.setAttribute("data-theme", "dark");
  else if (saved === "light") root.removeAttribute("data-theme");
  else if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches)
    root.setAttribute("data-theme", "dark");
  function sync() {
    var btn = document.getElementById("themeToggle");
    if (!btn) return;
    btn.textContent = root.getAttribute("data-theme") === "dark" ? "Light" : "Dark";
  }
  document.addEventListener("DOMContentLoaded", function () {
    sync();
    var btn = document.getElementById("themeToggle");
    if (!btn) return;
    btn.addEventListener("click", function () {
      if (root.getAttribute("data-theme") === "dark") {
        root.removeAttribute("data-theme");
        localStorage.setItem(key, "light");
      } else {
        root.setAttribute("data-theme", "dark");
        localStorage.setItem(key, "dark");
      }
      sync();
    });
  });
})();
