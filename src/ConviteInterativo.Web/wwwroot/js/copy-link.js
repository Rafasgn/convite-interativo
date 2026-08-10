document.addEventListener('click', function (e) {
    const toggle = e.target.closest('.js-toggle-link');
    if (toggle) {
        const target = document.getElementById(toggle.dataset.target);
        if (target) {
            target.hidden = false;
        }
        return;
    }

    const copyBtn = e.target.closest('.btn-copy');
    if (!copyBtn) {
        return;
    }

    const link = copyBtn.dataset.link
        ?? document.getElementById(copyBtn.dataset.linkTarget)?.textContent?.trim();
    if (!link) {
        return;
    }

    navigator.clipboard.writeText(link).then(function () {
        const original = copyBtn.textContent;
        copyBtn.textContent = 'Copiado!';
        setTimeout(function () {
            copyBtn.textContent = original;
        }, 2000);
    });
});
