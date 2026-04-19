(function () {
    "use strict";

    var form = document.querySelector("[data-register-form]");
    if (!form) return;

    var terms = document.getElementById("register-accept-terms");

    form.addEventListener("submit", function (e) {
        if (!terms || terms.checked) return;
        e.preventDefault();
        var msg = document.createElement("div");
        msg.className = "login-alert";
        msg.setAttribute("role", "alert");
        msg.textContent = "You must accept the terms and conditions.";
        var host = form.closest(".login-card") || form.parentElement;
        if (!host) return;
        var existing = host.querySelector(".login-alert[data-client-error]");
        if (existing) existing.remove();
        msg.setAttribute("data-client-error", "true");
        host.insertBefore(msg, form);
        terms.focus();
    });
})();
