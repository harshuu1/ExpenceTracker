/**
 * PaisaFlow — Core JavaScript Engine
 * "Expense entry < 5 seconds. One tap to add. One glance to understand."
 */

document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initHapticsAndAudio();
    initAddExpenseModal();
    initQuickAddBar();
    initHideBalance();
    initPwa();
});

// 1. Audio and Haptic Feedback
let audioCtx = null;
function playHapticBeep(success = true) {
    try {
        if (navigator.vibrate) {
            navigator.vibrate(success ? [12, 40, 12] : [50, 100, 50]);
        }
        if (!audioCtx) {
            audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        }
        if (audioCtx.state === 'suspended') {
            audioCtx.resume();
        }
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(success ? 880 : 330, audioCtx.currentTime);
        gain.gain.setValueAtTime(0.08, audioCtx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.15);
        osc.connect(gain);
        gain.connect(audioCtx.destination);
        osc.start();
        osc.stop(audioCtx.currentTime + 0.15);
    } catch (e) {
        // Audio/vibrate not allowed or unsupported
    }
}

// 2. Dynamic Island Toast Notification
function showToast(message, icon = 'bi-check2-circle') {
    let toast = document.getElementById('iosToast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'iosToast';
        toast.className = 'ios-toast';
        document.body.appendChild(toast);
    }
    toast.innerHTML = `<i class="bi ${icon}" style="color: var(--accent-green);"></i> <span>${message}</span>`;
    toast.classList.add('show');
    playHapticBeep(true);

    setTimeout(() => {
        toast.classList.remove('show');
    }, 2800);
}

// 3. Add Expense Bottom Sheet Modal (< 5 seconds flow)
function initAddExpenseModal() {
    const modal = document.getElementById('addExpenseModal');
    const openBtns = document.querySelectorAll('[data-open-add-expense]');
    const closeBtns = document.querySelectorAll('[data-close-modal]');
    const amountInput = document.getElementById('sheetAmountInput');
    const categoryChips = document.querySelectorAll('.chip-item');
    const selectedCategoryInput = document.getElementById('selectedCategoryId');
    const form = document.getElementById('addExpenseForm');
    const txTypeInput = document.getElementById('selectedTxType');
    const segmentBtns = document.querySelectorAll('.segment-btn');

    openBtns.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            if (modal) {
                modal.classList.add('show');
                setTimeout(() => amountInput?.focus(), 250);
            }
        });
    });

    closeBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            modal?.classList.remove('show');
        });
    });

    if (modal) {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.classList.remove('show');
            }
        });
    }

    // Category chips selection
    categoryChips.forEach(chip => {
        chip.addEventListener('click', () => {
            categoryChips.forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            const catId = chip.getAttribute('data-cat-id');
            if (selectedCategoryInput) selectedCategoryInput.value = catId;
            playHapticBeep(true);
        });
    });

    // Segmented Expense vs Income
    segmentBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            segmentBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const type = btn.getAttribute('data-type');
            if (txTypeInput) txTypeInput.value = type;
            playHapticBeep(true);
        });
    });

    // Smart Suggestion on amount input
    amountInput?.addEventListener('input', () => {
        const val = parseFloat(amountInput.value);
        if (!isNaN(val) && val > 0) {
            // Smart defaults: ₹150 -> food, ₹120 -> auto/transport
            if (val <= 300) {
                const foodChip = document.querySelector('.chip-item[data-cat-name="Food"]');
                if (foodChip && !document.querySelector('.chip-item.active')) {
                    foodChip.click();
                }
            }
        }
    });

    // AJAX Form submission for instantaneous response
    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const amt = parseFloat(amountInput.value);
        if (isNaN(amt) || amt <= 0) {
            alert('Please enter a valid amount');
            amountInput?.focus();
            return;
        }

        const formData = new FormData(form);
        const submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Saving...';
        }

        try {
            const resp = await fetch('/Transactions/Create', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });

            const res = await resp.json();
            if (res.success) {
                modal?.classList.remove('show');
                form.reset();
                showToast(res.message || 'Saved successfully!');
                setTimeout(() => window.location.reload(), 400);
            } else {
                alert(res.message || 'Error saving transaction');
            }
        } catch (err) {
            form.submit(); // fallback to normal submit
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = 'SAVE';
            }
        }
    });
}

// 4. Smart Quick Add Parser ("500 lunch UPI")
function initQuickAddBar() {
    const quickInput = document.getElementById('quickAddInput');
    const quickForm = document.getElementById('quickAddForm');
    const previewBox = document.getElementById('quickAddPreview');

    let debounceTimer;
    quickInput?.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        const text = quickInput.value.trim();
        if (text.length < 2) {
            if (previewBox) previewBox.style.display = 'none';
            return;
        }

        debounceTimer = setTimeout(async () => {
            try {
                const resp = await fetch(`/Transactions/ParsePreview?text=${encodeURIComponent(text)}`);
                const data = await resp.json();
                if (data.success && previewBox) {
                    previewBox.style.display = 'block';
                    previewBox.innerHTML = `✨ <strong>₹${data.amount}</strong> &bull; ${data.categoryIcon} ${data.categoryName} &bull; 💳 ${data.paymentMethod} ${data.note ? `&bull; "${data.note}"` : ''}`;
                }
            } catch (e) { }
        }, 200);
    });

    quickForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const text = quickInput.value.trim();
        if (!text) return;

        const token = quickForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        formData.append('input', text);
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const resp = await fetch('/Dashboard/QuickAdd', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });

            const res = await resp.json();
            if (res.success) {
                quickInput.value = '';
                if (previewBox) previewBox.style.display = 'none';
                showToast(res.message);
                setTimeout(() => window.location.reload(), 500);
            } else {
                alert(res.message || 'Could not parse input. Try "500 lunch UPI"');
            }
        } catch (e) {
            quickForm.submit();
        }
    });
}

// 5. Hide Balance Mode
function initHideBalance() {
    const toggleBtn = document.getElementById('toggleHideBalanceBtn');
    if (!toggleBtn) return;

    toggleBtn.addEventListener('click', async () => {
        try {
            const resp = await fetch('/Dashboard/ToggleHideBalance', { method: 'POST' });
            const data = await resp.json();
            if (data.success) {
                const balanceEl = document.querySelector('.balance-amount');
                if (balanceEl) {
                    if (data.hideBalance) {
                        balanceEl.dataset.actual = balanceEl.textContent;
                        balanceEl.textContent = '₹••••••';
                        balanceEl.classList.add('masked-balance');
                    } else {
                        balanceEl.textContent = balanceEl.dataset.actual || '₹23,580';
                        balanceEl.classList.remove('masked-balance');
                    }
                }
                playHapticBeep(true);
            }
        } catch (e) { }
    });
}

// 6. Theme Switching
function initTheme() {
    const themeBtn = document.getElementById('themeToggleBtn');
    const currentTheme = localStorage.getItem('paisaflow_theme') || 'dark';
    document.documentElement.setAttribute('data-theme', currentTheme);

    themeBtn?.addEventListener('click', () => {
        const theme = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('paisaflow_theme', theme);
        playHapticBeep(true);
    });
}

// 7. PWA Service Worker Registration
function initPwa() {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/sw.js').catch(() => {});
    }
}
