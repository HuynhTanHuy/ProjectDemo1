(function () {
    "use strict";

    var storageKey = "theme";

    function getStored() {
        var v = localStorage.getItem(storageKey);
        return v === "dark" ? "dark" : "light";
    }

    function apply(theme) {
        if (theme === "dark") {
            document.body.classList.add("dark-mode");
        } else {
            document.body.classList.remove("dark-mode");
        }
        var btn = document.getElementById("themeToggle");
        if (btn) {
            btn.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
            btn.setAttribute("title", theme === "dark" ? "Chế độ sáng" : "Chế độ tối");
            var icon = btn.querySelector(".c-theme-icon");
            if (icon) {
                icon.textContent = theme === "dark" ? "☀️" : "🌙";
            }
        }
    }

    function toggle() {
        var next = document.body.classList.contains("dark-mode") ? "light" : "dark";
        localStorage.setItem(storageKey, next);
        apply(next);
    }

    document.addEventListener("DOMContentLoaded", function () {
        apply(getStored());
        var btn = document.getElementById("themeToggle");
        if (btn) {
            btn.addEventListener("click", toggle);
        }
    });
})();
