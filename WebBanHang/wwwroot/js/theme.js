(function () {
    "use strict";

    var storageKey = "theme";

    function resolveTheme() {
        var stored = localStorage.getItem(storageKey);
        if (stored === "dark" || stored === "light") {
            return stored;
        }
        var htmlTheme = document.documentElement.getAttribute("data-theme");
        if (htmlTheme === "dark" || htmlTheme === "light") {
            return htmlTheme;
        }
        return document.body.classList.contains("dark-mode") ? "dark" : "light";
    }

    function apply(theme) {
        if (theme !== "dark" && theme !== "light") {
            return;
        }

        document.documentElement.setAttribute("data-theme", theme);

        if (theme === "dark") {
            document.body.classList.add("dark-mode");
        } else {
            document.body.classList.remove("dark-mode");
        }

        var btn = document.getElementById("themeToggle");
        if (!btn) {
            return;
        }

        btn.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
        btn.setAttribute("title", theme === "dark" ? "Chế độ sáng" : "Chế độ tối");
        btn.setAttribute("aria-label", theme === "dark" ? "Bật chế độ sáng" : "Bật chế độ tối");

        var icon = btn.querySelector(".c-theme-icon");
        if (icon) {
            icon.textContent = theme === "dark" ? "\u2600\uFE0F" : "\uD83C\uDF19";
        }
    }

    function setTheme(theme) {
        if (theme !== "dark" && theme !== "light") {
            return;
        }
        localStorage.setItem(storageKey, theme);
        apply(theme);
    }

    function loadTheme() {
        apply(resolveTheme());
    }

    function toggleTheme() {
        setTheme(resolveTheme() === "dark" ? "light" : "dark");
    }

    window.AppTheme = {
        getTheme: resolveTheme,
        setTheme: setTheme,
        toggleTheme: toggleTheme,
        loadTheme: loadTheme
    };

    document.addEventListener("DOMContentLoaded", function () {
        loadTheme();
        var btn = document.getElementById("themeToggle");
        if (btn) {
            btn.addEventListener("click", toggleTheme);
        }
    });
})();
