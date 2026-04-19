(function () {
    "use strict";

    var root = document.querySelector("[data-auth-page]");
    if (!root) return;

    var stage = root.querySelector(".login-stage");
    var glow = root.querySelector(".login-visual__glow");
    var characters = root.querySelectorAll(".login-character");

    var bounds = { width: 1, height: 1, left: 0, top: 0 };
    var target = { x: 0.5, y: 0.5 };
    var current = { x: 0.5, y: 0.5 };
    var rafId = 0;

    function readBounds() {
        if (!stage) return;
        var rect = stage.getBoundingClientRect();
        bounds.width = Math.max(rect.width, 1);
        bounds.height = Math.max(rect.height, 1);
        bounds.left = rect.left;
        bounds.top = rect.top;
    }

    function onMouseMove(e) {
        readBounds();
        var nx = (e.clientX - bounds.left) / bounds.width;
        var ny = (e.clientY - bounds.top) / bounds.height;
        target.x = Math.min(1, Math.max(0, nx));
        target.y = Math.min(1, Math.max(0, ny));

        if (glow) {
            glow.style.left = target.x * 100 + "%";
            glow.style.top = target.y * 100 + "%";
        }

        if (!rafId) {
            rafId = window.requestAnimationFrame(tick);
        }
    }

    function tick() {
        rafId = 0;
        var ease = 0.14;
        current.x += (target.x - current.x) * ease;
        current.y += (target.y - current.y) * ease;

        var dx = current.x - 0.5;
        var dy = current.y - 0.5;

        characters.forEach(function (el, i) {
            var sign = i === 0 ? 1 : -1;
            var skew = dx * 10 * sign;
            var tx = dx * 18 * sign;
            var ty = dy * 8;
            var rot = dx * 6 * sign;
            el.style.transform =
                "translate3d(" + tx + "px," + ty + "px,0) skewX(" + skew + "deg) rotate(" + rot + "deg)";
        });

        if (Math.abs(target.x - current.x) > 0.001 || Math.abs(target.y - current.y) > 0.001) {
            rafId = window.requestAnimationFrame(tick);
        }
    }

    function onResize() {
        readBounds();
    }

    window.addEventListener("mousemove", onMouseMove, { passive: true });
    window.addEventListener("resize", onResize);

    root.querySelectorAll("[data-password-toggle]").forEach(function (toggleBtn) {
        var wrap = toggleBtn.closest(".login-password-wrap");
        var passwordInput = wrap ? wrap.querySelector("input") : null;
        if (!passwordInput) return;

        var showLabel = toggleBtn.getAttribute("data-label-show") || "Hiện";
        var hideLabel = toggleBtn.getAttribute("data-label-hide") || "Ẩn";

        toggleBtn.addEventListener("click", function () {
            var isText = passwordInput.type === "text";
            passwordInput.type = isText ? "password" : "text";
            toggleBtn.setAttribute("aria-pressed", (!isText).toString());
            toggleBtn.textContent = isText ? showLabel : hideLabel;
        });
    });

    readBounds();
    tick();
})();
