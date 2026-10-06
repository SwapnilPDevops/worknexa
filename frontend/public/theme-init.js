// Runs before first paint so the page does not flash the wrong theme. Kept as a file so the CSP can stay script-src 'self'.
try {
  var preference = localStorage.getItem("worknexa-theme") || "system";
  var dark =
    preference === "dark" ||
    (preference !== "light" && window.matchMedia("(prefers-color-scheme: dark)").matches);
  document.documentElement.dataset.theme = dark ? "dark" : "light";
  document.documentElement.dataset.themePreference = preference;
} catch (error) {}
