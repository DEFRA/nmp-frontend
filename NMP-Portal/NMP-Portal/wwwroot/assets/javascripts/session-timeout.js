(function () {
    if (!window.sessionConfig) return;

    const SESSION_LENGTH = window.sessionConfig.timeoutMinutes * 60 * 1000;
    const WARNING_TIME = window.sessionConfig.warningMinutes * 60 * 1000; // Show warning 3 minutes before expiry
    const REFRESH_URL = window.sessionConfig.keepAliveUrl;
    const SIGNOUT_URL = window.sessionConfig.logoutUrl;
    const STORAGE_KEY = 'govuk-last-activity';
    const CHANNEL_NAME = 'govuk-session';
    const DIALOG = document.getElementById('session-timeout-dialog');
    const STAY_SIGNED_IN_BUTTON = document.getElementById('stay-signed-in');
    const SIGNOUT_BUTTON = document.getElementById("sign-out");
    const FOCUSABLE_ELEMENTS = [STAY_SIGNED_IN_BUTTON, SIGNOUT_BUTTON];
    const DISPLAY_NONE_CLASS = 'govuk-!-display-none';
    const UNAUTHORISED_STATUS_CODE = 401;
    const SESSION_EXPIRED = 419;

    let warningTimer, expiryTimer, countdownInterval;
    let channel = null;

    // Try BroadcastChannel first
    try {
        channel = new BroadcastChannel(CHANNEL_NAME);
        channel.onmessage = (e) => {
            switch (e.data.type) {
                case 'activity':
                    resetTimers();
                    break;
                case 'refresh':
                    hideDialog();      // 🔹 auto-close dialog in other tabs
                    resetTimers();
                    break;
                case 'expire':
                    expireSession();
                    break;
            }
        };
    } catch (err) {
        console.warn('BroadcastChannel not supported, using localStorage fallback');
    }

    // Fallback (storage event for Safari/IE)
    window.addEventListener('storage', function (e) {
        if (e.key === STORAGE_KEY) {
            resetTimers();
        }
        if (e.key === 'govuk-session-refresh') {
            hideDialog();
        }
    });

    // Reset timers
    function resetTimers() {
        clearTimeout(warningTimer);
        clearTimeout(expiryTimer);
        const last = parseInt(localStorage.getItem(STORAGE_KEY) || Date.now());
        const since = Date.now() - last;
        const warnDelay = Math.max(0, SESSION_LENGTH - WARNING_TIME - since);
        const expireDelay = Math.max(0, SESSION_LENGTH - since);
        warningTimer = setTimeout(showDialog, warnDelay);
        expiryTimer = setTimeout(expireSession, expireDelay);
    }

    // Record user activity
    function recordActivity() {
        const now = Date.now();
        localStorage.setItem(STORAGE_KEY, now);
        if (channel) {
            channel.postMessage({ type: 'activity', timestamp: now });
        }
        resetTimers();
    }

    // GOV.UK dialog controls
    function showDialog() {
        DIALOG.classList.remove(DISPLAY_NONE_CLASS);
        document.addEventListener('keydown', trapFocus);
        STAY_SIGNED_IN_BUTTON.focus();
        startCountdown(WARNING_TIME / 1000);
    }

    function hideDialog() {
        DIALOG.classList.add(DISPLAY_NONE_CLASS);
        clearInterval(countdownInterval);
        document.removeEventListener('keydown', trapFocus);

    }

    function startCountdown(seconds) {
        let remaining = seconds;
        //updateCountdown(remaining);
        countdownInterval = setInterval(() => {
            remaining--;
            //updateCountdown(remaining);
            if (remaining <= 0) {
                clearInterval(countdownInterval);
                expireSession();
            }
        }, 1000);
    }
    function updateCountdown(seconds) {
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        // countdownEl.textContent = `${mins} minute${mins !== 1 ? 's' : ''} ${secs} second${secs !== 1 ? 's' : ''}`;
    }
    function keepAlive() {
        fetch(REFRESH_URL).then(response => {
            if (response.ok) {
                // Session refreshed successfully
                hideDialog();
                recordActivity();

                // Inform all tabs
                if (channel) {
                    channel.postMessage({ type: 'refresh' });
                } else {
                    localStorage.setItem('govuk-session-refresh', Date.now());
                }
            } else if (response.status === UNAUTHORISED_STATUS_CODE || response.status === SESSION_EXPIRED) {
                // Token or session expired
                console.warn('Session expired on server. Signing out.');
                expireSession();
            } else {
                console.error('Unexpected response', response.status);
                expireSession();
            }
        })
            .catch(error => {
                console.error('KeepAlive failed', error);
                expireSession();
            });
    }

    // ---- Focus trap (modal only) ----
    function trapFocus(e) {
        if (e.key === "Tab") {
            var focusedIndex = FOCUSABLE_ELEMENTS.indexOf(document.activeElement);
            if (e.shiftKey) {
                if (focusedIndex === 0) {
                    e.preventDefault();
                    FOCUSABLE_ELEMENTS[FOCUSABLE_ELEMENTS.length - 1].focus();
                }
            } else {
                if (focusedIndex === FOCUSABLE_ELEMENTS.length - 1) {
                    e.preventDefault();
                    FOCUSABLE_ELEMENTS[0].focus();
                }
            }
        }
    }

    function expireSession() {
        if (channel) channel.postMessage({ type: 'expire' });
        //hideDialog();
        window.location.href = SIGNOUT_URL;
    }

    function userActivityHandler() {
        // Only record activity if the timeout dialog is hidden
        if (DIALOG.classList.contains(DISPLAY_NONE_CLASS)) {
            recordActivity();
        }
    }

    // Hook up events   
    function bindActivityListeners() {
        ['click', 'keypress', 'mousemove', 'scroll'].forEach(eventType => {
            window.addEventListener(eventType, () => {
                userActivityHandler();
            });
        });
    }

    STAY_SIGNED_IN_BUTTON.addEventListener('click', keepAlive);
    bindActivityListeners();

    // Initialise timers on load
    recordActivity();

})();