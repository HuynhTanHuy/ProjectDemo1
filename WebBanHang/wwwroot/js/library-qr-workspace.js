/* global Html5Qrcode */
(function () {
    let cfg = {};
    let invSessionId = null;
    let html5Qr = null;
    let scanTargetId = null;

    function postForm(url, body) {
        const params = new URLSearchParams();
        params.append('__RequestVerificationToken', cfg.afToken);
        for (const k in body) {
            if (body[k] !== undefined && body[k] !== null) {
                params.append(k, body[k]);
            }
        }
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
            body: params.toString()
        }).then(r => r.json());
    }

    function show(elId, html, isError) {
        const el = document.getElementById(elId);
        if (!el) return;
        el.className = 'mt-2 small ' + (isError ? 'text-danger' : 'text-success');
        el.innerHTML = html;
    }

    async function stopScanner() {
        if (html5Qr) {
            try {
                await html5Qr.stop();
                await html5Qr.clear();
            } catch (e) { /* ignore */ }
            html5Qr = null;
        }
        const host = document.getElementById('qrReaderHost');
        if (host) host.innerHTML = '';
    }

    function openModal() {
        const modal = new bootstrap.Modal(document.getElementById('qrScanModal'));
        modal.show();
    }

    async function startScannerFor(targetInputId) {
        scanTargetId = targetInputId;
        await stopScanner();
        openModal();
        const hostId = 'qrReaderHost';
        document.getElementById(hostId).innerHTML = '';
        html5Qr = new Html5Qrcode(hostId);
        const config = { fps: 10, qrbox: { width: 220, height: 220 } };
        await html5Qr.start(
            { facingMode: 'environment' },
            config,
            (decodedText) => {
                const input = document.getElementById(scanTargetId);
                if (input) input.value = decodedText.trim();
                stopScanner();
                const inst = bootstrap.Modal.getInstance(document.getElementById('qrScanModal'));
                if (inst) inst.hide();
            },
            () => { }
        );
    }

    function bindScanButtons() {
        document.querySelectorAll('[data-scan-target]').forEach(btn => {
            btn.addEventListener('click', async () => {
                const id = btn.getAttribute('data-scan-target');
                if (btn.disabled) return;
                try {
                    await startScannerFor(id);
                } catch (e) {
                    alert('Không mở được camera: ' + (e.message || e));
                }
            });
        });
    }

    function init(config) {
        cfg = config;

        document.getElementById('qrScanModal').addEventListener('hidden.bs.modal', () => {
            stopScanner();
        });

        bindScanButtons();

        document.getElementById('btnLookup').addEventListener('click', async () => {
            const payload = document.getElementById('lookupInput').value.trim();
            const res = await postForm(cfg.urls.lookup, { payload });
            if (!res.success) {
                show('lookupResult', res.message || 'Lỗi', true);
                document.getElementById('lookupQrPreview').classList.add('d-none');
                return;
            }
            const d = res.data;
            const lines = [
                '<strong>' + escapeHtml(d.bookTitle) + '</strong>',
                'Mã bản sao: ' + escapeHtml(d.copyCode),
                'Tác giả: ' + escapeHtml(d.authorName || '—'),
                'Trạng thái: ' + escapeHtml(d.copyStatus),
                'Kệ (hệ thống): ' + escapeHtml(d.shelfLocation || '—'),
                'Người mượn: ' + escapeHtml(d.borrowedByFullName || d.borrowedByUserName || '—'),
                d.dueDateUtc ? ('Hạn trả: ' + escapeHtml(d.dueDateUtc)) : ''
            ].filter(Boolean).join('<br/>');
            show('lookupResult', lines, false);
            document.getElementById('regenCopyId').value = d.bookCopyId;
            const dl = document.getElementById('btnDownloadQr');
            dl.href = cfg.urls.downloadQr + '?id=' + encodeURIComponent(d.bookCopyId);
            dl.style.display = 'inline-block';
            const prev = document.getElementById('lookupQrPreview');
            const img = document.getElementById('lookupQrImg');
            if (d.qrImageRelativeUrl) {
                img.src = d.qrImageRelativeUrl;
                img.onerror = function () { prev.classList.add('d-none'); };
                prev.classList.remove('d-none');
            } else {
                prev.classList.add('d-none');
            }
        });

        document.getElementById('btnBorrowQr').addEventListener('click', async () => {
            const memberPayload = document.getElementById('borrowMemberInput').value.trim();
            const copyPayload = document.getElementById('borrowCopyInput').value.trim();
            const res = await postForm(cfg.urls.borrow, { memberPayload, copyPayload });
            show('borrowResult', res.success ? ('Đã tạo phiếu mượn #' + res.data) : (res.message || res.code), !res.success);
        });

        document.getElementById('btnReturnQr').addEventListener('click', async () => {
            const copyPayload = document.getElementById('returnCopyInput').value.trim();
            const res = await postForm(cfg.urls.returnBook, { copyPayload });
            show('returnResult', res.success ? 'Đã ghi nhận trả sách.' : (res.message || res.code), !res.success);
        });

        document.getElementById('btnSyncAll').addEventListener('click', async () => {
            const res = await postForm(cfg.urls.syncAll, {});
            alert(res.message || (res.success ? 'Xong' : 'Lỗi'));
        });

        document.getElementById('btnRegenQr').addEventListener('click', async () => {
            const id = document.getElementById('regenCopyId').value;
            if (!id) return;
            const res = await postForm(cfg.urls.regenQr, { bookCopyId: id });
            if (res.success && res.data && res.data.qrImageRelativePath) {
                alert('Đã tạo lại QR. Đường dẫn: ' + res.data.qrImageRelativePath);
                const dl = document.getElementById('btnDownloadQr');
                dl.href = cfg.urls.downloadQr + '?id=' + encodeURIComponent(id);
                dl.style.display = 'inline-block';
            } else {
                alert(res.message || 'Lỗi');
            }
        });

        document.getElementById('btnInvStart').addEventListener('click', async () => {
            const note = document.getElementById('invNote').value.trim();
            const res = await postForm(cfg.urls.invStart, { note });
            if (!res.success) {
                alert(res.message || res.code);
                return;
            }
            invSessionId = res.data;
            document.getElementById('invSessionLabel').textContent = 'Phiên #' + invSessionId;
            ['invScanInput', 'invShelfObserved', 'btnInvScan', 'btnInvComplete', 'btnScanInv'].forEach(id => {
                const el = document.getElementById(id);
                if (el) el.disabled = false;
            });
            document.querySelector('[data-scan-target="invScanInput"]').disabled = false;
            document.getElementById('invLog').textContent = '';
            document.getElementById('invReport').innerHTML = '';
        });

        document.getElementById('btnInvScan').addEventListener('click', async () => {
            if (!invSessionId) return;
            const copyPayload = document.getElementById('invScanInput').value.trim();
            const observedShelf = document.getElementById('invShelfObserved').value.trim();
            const res = await postForm(cfg.urls.invScan, { sessionId: invSessionId, copyPayload, observedShelf });
            const log = document.getElementById('invLog');
            const line = (res.success ? '[OK] ' : '[ERR] ') + (res.data?.copyCode || copyPayload) + ' ' + (res.message || '') + '\n';
            log.textContent = line + log.textContent;
            if (res.data?.wrongShelf) {
                log.textContent = '[CẢNH BÁO KỆ] ' + (res.data.copyCode) + '\n' + log.textContent;
            }
        });

        document.getElementById('btnInvComplete').addEventListener('click', async () => {
            if (!invSessionId) return;
            const res = await postForm(cfg.urls.invComplete, { sessionId: invSessionId });
            if (!res.success) {
                alert(res.message || res.code);
                return;
            }
            const d = res.data;
            let html = '<p>Đã quét: ' + d.scannedCount + ' / ' + d.totalCopiesInLibrary + '</p>';
            if (d.missingCopyCodes && d.missingCopyCodes.length) {
                html += '<p class="text-danger"><strong>Chưa quét (thiếu):</strong><br/>' +
                    d.missingCopyCodes.map(escapeHtml).join(', ') + '</p>';
            }
            if (d.wrongShelfLines && d.wrongShelfLines.length) {
                html += '<p class="text-warning"><strong>Sai vị trí:</strong><br/>' +
                    d.wrongShelfLines.map(escapeHtml).join('<br/>') + '</p>';
            }
            document.getElementById('invReport').innerHTML = html;
        });
    }

    function escapeHtml(s) {
        if (!s) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    window.libraryQrWorkspace = { init };
})();
