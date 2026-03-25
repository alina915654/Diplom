(function () {
    const overlay = document.getElementById('confirmModalOverlay');
    const titleElement = document.getElementById('confirmModalTitle');
    const messageElement = document.getElementById('confirmModalMessage');
    const acceptButton = document.getElementById('confirmModalAccept');

    if (!overlay || !titleElement || !messageElement || !acceptButton) {
        return;
    }

    let pendingForm = null;

    function openConfirmModal(form) {
        pendingForm = form;
        titleElement.textContent = form.dataset.confirmTitle || 'Подтверждение действия';
        messageElement.textContent = form.dataset.confirmMessage || 'Подтвердите выполнение операции.';
        acceptButton.textContent = form.dataset.confirmAccept || 'Подтвердить';

        overlay.classList.add('is-open');
        overlay.setAttribute('aria-hidden', 'false');
        document.body.classList.add('confirm-modal-open');

        window.setTimeout(function () {
            acceptButton.focus();
        }, 30);
    }

    function closeConfirmModal() {
        overlay.classList.remove('is-open');
        overlay.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('confirm-modal-open');
        pendingForm = null;
    }

    document.addEventListener('submit', function (event) {
        const form = event.target;

        if (!(form instanceof HTMLFormElement) || form.dataset.confirm !== 'true') {
            return;
        }

        if (form.dataset.confirmed === 'true') {
            delete form.dataset.confirmed;
            return;
        }

        event.preventDefault();
        openConfirmModal(form);
    });

    document.addEventListener('click', function (event) {
        const target = event.target;

        if (!(target instanceof HTMLElement)) {
            return;
        }

        if (target.closest('[data-confirm-dismiss]')) {
            closeConfirmModal();
            return;
        }

        if (target === overlay) {
            closeConfirmModal();
        }
    });

    acceptButton.addEventListener('click', function () {
        if (!pendingForm) {
            closeConfirmModal();
            return;
        }

        const form = pendingForm;
        form.dataset.confirmed = 'true';
        closeConfirmModal();

        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
        } else {
            form.submit();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && overlay.classList.contains('is-open')) {
            closeConfirmModal();
        }
    });
})();
