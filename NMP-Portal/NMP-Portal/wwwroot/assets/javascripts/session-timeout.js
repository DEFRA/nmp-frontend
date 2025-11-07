(function () {
    if (!window.sessionConfig) return;

    const SESSION_LENGTH = window.sessionConfig.timeoutMinutes * 60 * 1000;
    const WARNING_TIME = window.sessionConfig.warningMinutes * 60 * 1000; // Show warning 3 minutes before expiry
    const REFRESH_URL = window.sessionConfig.keepAliveUrl;
    const SIGNOUT_URL = window.sessionConfig.logoutUrl;
    const STORAGE_KEY = 'govuk-last-activity';
    const CHANNEL_NAME = 'govuk-session';
    const DIALOG = document.getElementById('session-timeout-dialog');
    //const COUNTDOWN_ELEMENT = document.getElementById('timeout-countdown');
    const STAY_SIGNED_IN_BUTTON = document.getElementById('stay-signed-in');
    const SIGNOUT_BUTTON = document.getElementById("sign-out");
    const FOCUSABLE_ELEMENTS = [STAY_SIGNED_IN_BUTTON, SIGNOUT_BUTTON];


    var expireAt, warnAt;
    var countdownInterval, warningTimeout, logoutTimeout;
             
    
    

    var lastActivity = new Date().getTime();

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
        if (e.key === STORAGE_KEY) resetTimers();
        if (e.key === 'govuk-session-refresh') hideDialog();
    });


    // Record user activity
    function recordActivity() {
        const now = Date.now();
        localStorage.setItem(STORAGE_KEY, now);
        if (channel) channel.postMessage({ type: 'activity', timestamp: now });
        resetTimers();
    }

    // ---- Reset timers ----
    function resetTimers() {
        clearInterval(countdownInterval);
        clearTimeout(warningTimeout);
        clearTimeout(logoutTimeout);

        var now = new Date().getTime();
        expireAt = now + (SESSION_LENGTH);
        warnAt = expireAt - (WARNING_TIME);

        warningTimeout = setTimeout(showWarning, warnAt - now);
        logoutTimeout = setTimeout(function () {
            window.location.href = SIGNOUT_URL;
        }, expireAt - now);

        console.log("Timers reset → expire at", new Date(expireAt).toLocaleTimeString());
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

    function formatTime(seconds) {
        var minutes = Math.floor(seconds / 60);
        var secs = seconds % 60;

        if (minutes >= 5) {
            // Keep it simple for longer times
            return minutes + " minute" + (minutes > 1 ? "s" : "");
        }
        else if (minutes > 0) {
            // Show minutes + seconds if under 5 minutes
            return minutes + " minute" + (minutes > 1 ? "s " : " ") +
                (secs > 0 ? secs + " second" + (secs > 1 ? "s" : "") : "");
        }
        else {
            // Less than 1 minute → seconds only
            return secs + " second" + (secs !== 1 ? "s" : "");
        }
    }

    function updateCountdown(elementId) {
        var remainingMs = expireAt - new Date().getTime();
        var remainingSeconds = Math.max(0, Math.floor(remainingMs / 1000));
        //document.getElementById(elementId).innerText = formatTime(remainingSeconds);

        if (remainingSeconds <= 0) {
            clearInterval(countdownInterval);
            window.location.href = SIGNOUT_URL;
        }
    }

    // ---- Show warning (modal or banner) ----
    function showWarning() {       
        DIALOG.classList.remove("govuk-!-display-none");
            document.addEventListener("keydown", trapFocus);
        STAY_SIGNED_IN_BUTTON.focus();
            countdownInterval = setInterval(function () {
                updateCountdown("countdown");
            }, 1000);
      
    }

    // ---- Refresh session ----
    function refreshSession() {
        fetch(REFRESH_URL).then(() => {
            DIALOG.classList.add("govuk-!-display-none");            
            document.removeEventListener("keydown", trapFocus);

            resetTimers(); // 🔥 critical fix
            console.log("Session refreshed at " + new Date().toLocaleTimeString());
        });
    }

    // ---- User activity detection ----
    function activityDetected() {
        lastActivity = new Date().getTime();
        console.log("Last Activity at :" + new Date().toLocaleTimeString());
    }

    ['click', 'mousemove', 'keydown', 'scroll'].forEach(function (evt) {
        document.addEventListener(evt, activityDetected);
    });

    // ---- Auto refresh if user active ----
    setInterval(function () {
        var now = new Date().getTime();
        var inactiveMs = now - lastActivity;
        if (inactiveMs < 2 * 60 * 1000) {
            refreshSession();
        }
    }, 5 * 60 * 1000);

    // ---- Button actions ----
    STAY_SIGNED_IN_BUTTON.addEventListener("click", refreshSession);
    //STAY_SIGNED_IN_BUTTON.addEventListener('click', keepAlive);
    function userActivityHandler() {
        // Only record activity if the timeout dialog is hidden
        if (DIALOG.classList.contains('govuk-!-display-none')) {
            recordActivity();
        }
    }
    function bindActivityListeners() {
        ['click', 'keypress', 'mousemove', 'scroll'].forEach(eventType => {
            window.addEventListener(eventType, () => {
                if (DIALOG.classList.contains('govuk-!-display-none')) {
                    userActivityHandler();
                }
            });
        });
    }
    
    bindActivityListeners();

    resetTimers();
})();