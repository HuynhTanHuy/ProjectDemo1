/* global Html5Qrcode */
(function () {
    let activeScanner = null;

    function postForm(url, afToken, fields) {
        const params = new URLSearchParams();
        params.append('__RequestVerificationToken', afToken);
        for (const key in fields) {
            if (fields[key] !== undefined && fields[key] !== null) {
                params.append(key, fields[key]);
            }
        }
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
            body: params.toString()
        }).then(function (r) { return r.json(); });
    }

    function escapeHtml(s) {
        if (!s) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function formatDueDate(iso) {
        if (!iso) return '';
        try {
            const d = new Date(iso);
            return d.toLocaleDateString('vi-VN');
        } catch (e) {
            return iso;
        }
    }

    function renderDetail(root, data) {
        const panel = root.querySelector('.qr-copy-detail');
        if (!panel || !data) {
            return;
        }

        const cover = panel.querySelector('.qr-detail-cover');
        const title = panel.querySelector('.qr-detail-title');
        const meta = panel.querySelector('.qr-detail-meta');
        const code = panel.querySelector('.qr-detail-code');
        const shelf = panel.querySelector('.qr-detail-shelf');
        const status = panel.querySelector('.qr-detail-status');
        const dueLabel = panel.querySelector('.qr-detail-due-label');
        const due = panel.querySelector('.qr-detail-due');
        const productLink = panel.querySelector('.qr-detail-product-link');

        title.textContent = data.bookTitle || '—';
        const metaParts = [data.authorName, data.categoryName].filter(Boolean);
        meta.textContent = metaParts.length ? metaParts.join(' · ') : '—';
        code.textContent = data.copyCode || '—';
        shelf.textContent = data.shelfLocation || '—';
        status.textContent = data.copyStatus || '—';

        if (data.bookImageUrl) {
            cover.src = data.bookImageUrl;
            cover.classList.remove('d-none');
        } else {
            cover.removeAttribute('src');
            cover.classList.add('d-none');
        }

        if (data.isBorrowedByMe && data.dueDateUtc) {
            dueLabel.classList.remove('d-none');
            due.classList.remove('d-none');
            due.textContent = formatDueDate(data.dueDateUtc);
        } else {
            dueLabel.classList.add('d-none');
            due.classList.add('d-none');
            due.textContent = '';
        }

        if (data.productUrl) {
            productLink.href = data.productUrl;
            productLink.classList.remove('d-none');
        } else {
            productLink.classList.add('d-none');
        }

        panel.classList.remove('d-none');
    }

    function hideDetail(root) {
        const panel = root.querySelector('.qr-copy-detail');
        if (panel) {
            panel.classList.add('d-none');
        }
    }

    async function lookupCopy(root, copyPayload, msgEl) {
        const lookupUrl = root.dataset.lookupUrl;
        const afToken = root.dataset.afToken;
        if (!lookupUrl || !copyPayload) {
            return null;
        }

        const res = await postForm(lookupUrl, afToken, { copyPayload: copyPayload });
        if (!res.success) {
            hideDetail(root);
            if (msgEl) {
                msgEl.className = 'small mt-2 text-danger';
                msgEl.textContent = res.message || res.code || 'Không tìm thấy bản sao.';
            }
            return null;
        }

        renderDetail(root, res.data);
        if (msgEl) {
            msgEl.className = 'small mt-2 text-muted';
            msgEl.textContent = res.data.isBorrowedByMe
                ? 'Đây là bản sao bạn đang mượn.'
                : 'Đã tìm thấy thông tin bản sao.';
        }
        return res.data;
    }

    async function stopScanner(host) {
        if (activeScanner) {
            try {
                await activeScanner.stop();
                await activeScanner.clear();
            } catch (e) { /* ignore */ }
            activeScanner = null;
        }
        if (host) {
            host.innerHTML = '';
            host.classList.add('d-none');
        }
    }

    function initPanel(root) {
        const returnUrl = root.dataset.returnUrl;
        const afToken = root.dataset.afToken;
        const reloadOnSuccess = root.dataset.reloadOnSuccess === 'true';
        const input = root.querySelector('.qr-return-input');
        const scanBtn = root.querySelector('.qr-return-scan');
        const lookupBtn = root.querySelector('.qr-return-lookup');
        const submitBtn = root.querySelector('.qr-return-submit');
        const msg = root.querySelector('.qr-return-msg');
        const camHost = root.querySelector('.qr-return-cam-host');

        if (!returnUrl || !afToken || !input || !submitBtn || !msg) {
            return;
        }

        async function runLookup() {
            const value = input.value.trim();
            if (!value) {
                msg.className = 'small mt-2 text-danger';
                msg.textContent = 'Vui lòng nhập hoặc quét mã QR trên bản sao sách.';
                hideDetail(root);
                return null;
            }
            return lookupCopy(root, value, msg);
        }

        if (lookupBtn) {
            lookupBtn.addEventListener('click', runLookup);
        }

        submitBtn.addEventListener('click', async function () {
            const value = input.value.trim();
            if (!value) {
                msg.className = 'small mt-2 text-danger';
                msg.textContent = 'Vui lòng nhập hoặc quét mã QR trên bản sao sách.';
                return;
            }
            submitBtn.disabled = true;
            try {
                const res = await postForm(returnUrl, afToken, { copyPayload: value });
                msg.className = 'small mt-2 ' + (res.success ? 'text-success' : 'text-danger');
                msg.textContent = res.success
                    ? 'Đã ghi nhận trả sách.'
                    : (res.message || res.code || 'Lỗi');
                if (res.success) {
                    input.value = '';
                    hideDetail(root);
                    if (reloadOnSuccess) {
                        setTimeout(function () { window.location.reload(); }, 900);
                    }
                }
            } finally {
                submitBtn.disabled = false;
            }
        });

        if (scanBtn && camHost) {
            scanBtn.addEventListener('click', async function () {
                if (typeof Html5Qrcode === 'undefined') {
                    alert('Thư viện quét QR chưa được tải.');
                    return;
                }
                await stopScanner(camHost);
                camHost.classList.remove('d-none');
                if (!camHost.id) {
                    camHost.id = 'qrReturnCam_' + Math.random().toString(36).slice(2);
                }
                activeScanner = new Html5Qrcode(camHost.id);
                try {
                    await activeScanner.start(
                        { facingMode: 'environment' },
                        { fps: 10, qrbox: { width: 200, height: 200 } },
                        async function (text) {
                            input.value = text.trim();
                            await stopScanner(camHost);
                            await runLookup();
                        },
                        function () { }
                    );
                } catch (e) {
                    await stopScanner(camHost);
                    alert('Không mở được camera: ' + (e.message || e));
                }
            });
        }
    }

    function initAll() {
        document.querySelectorAll('.book-copy-qr-return').forEach(initPanel);
    }

    window.bookCopyQrReturn = { initAll: initAll };
})();
