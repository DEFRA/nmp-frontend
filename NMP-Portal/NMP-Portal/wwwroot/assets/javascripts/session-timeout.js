(function () {
    if (!window.sessionConfig) return;

    var sessionTimeoutMinutes = window.sessionConfig.timeoutMinutes;
    var warningBeforeMinutes = window.sessionConfig.warningMinutes;
    var logoutUrl = window.sessionConfig.logoutUrl;
    var keepAliveUrl = window.sessionConfig.keepAliveUrl;

    var expireAt, warnAt;
    var countdownInterval, warningTimeout, logoutTimeout;
    var modal = document.getElementById("session-timeout-dialog");
    //var banner = document.getElementById("session-timeout-banner");
    var stayBtn = document.getElementById("stay-signed-in");
   // var stayBtnBanner = document.getElementById("stay-signed-in-banner");
    var signOutBtn = document.getElementById("sign-out");
    var focusableElements = [stayBtn, signOutBtn];

    var lastActivity = new Date().getTime();

    // ---- Reset timers ----
    function resetTimers() {
        clearInterval(countdownInterval);
        clearTimeout(warningTimeout);
        clearTimeout(logoutTimeout);

        var now = new Date().getTime();
        expireAt = now + (sessionTimeoutMinutes * 60 * 1000);
        warnAt = expireAt - (warningBeforeMinutes * 60 * 1000);

        warningTimeout = setTimeout(showWarning, warnAt - now);
        logoutTimeout = setTimeout(function () {
            window.location.href = loginUrl;
        }, expireAt - now);

        console.log("Timers reset → expire at", new Date(expireAt).toLocaleTimeString());
    }

    // ---- Focus trap (modal only) ----
    function trapFocus(e) {
        if (e.key === "Tab") {
            var focusedIndex = focusableElements.indexOf(document.activeElement);
            if (e.shiftKey) {
                if (focusedIndex === 0) {
                    e.preventDefault();
                    focusableElements[focusableElements.length - 1].focus();
                }
            } else {
                if (focusedIndex === focusableElements.length - 1) {
                    e.preventDefault();
                    focusableElements[0].focus();
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
            window.location.href = logoutUrl;
        }
    }

    // ---- Show warning (modal or banner) ----
    function showWarning() {
        //if (window.innerWidth <= 640) {
        //    banner.classList.remove("govuk-!-display-none");
        //    countdownInterval = setInterval(function () {
        //        updateCountdown("countdown-banner");
        //    }, 1000);
        //} else {
            modal.classList.remove("govuk-!-display-none");
            document.addEventListener("keydown", trapFocus);
            stayBtn.focus();
            countdownInterval = setInterval(function () {
                updateCountdown("countdown");
            }, 1000);
       /* }*/
    }

    // ---- Refresh session ----
    function refreshSession() {
        fetch(keepAliveUrl).then(() => {
            modal.classList.add("govuk-!-display-none");
            //banner.classList.add("govuk-!-display-none");
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
    stayBtn.addEventListener("click", refreshSession);
    //stayBtnBanner.addEventListener("click", refreshSession);

    // ---- Init timers on page load ----
    resetTimers();
})();