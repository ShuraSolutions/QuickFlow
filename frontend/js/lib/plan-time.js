/*
 * Pure plan time helpers (no DOM, no network): live rest-time / countdown computation from a
 * server plan snapshot, and formatting. The server stays authoritative for status; these helpers
 * only interpolate between refreshes so the countdown ticks every second.
 */
(function (window) {
  "use strict";

  function pad(n) { return (n < 10 ? "0" : "") + n; }

  var PlanTime = {
    /**
     * Clock skew (ms) between the server and this browser, from a response's serverTime and
     * the local time the response was received. correctedNow = Date.now() + skew.
     */
    skew: function (serverTimeIso, receivedAtMs) {
      var server = Date.parse(serverTimeIso);
      return isNaN(server) ? 0 : server - receivedAtMs;
    },

    /**
     * Live state of a plan at nowMs (already skew-corrected).
     * phase: "upcoming" (before start), "active" (start ≤ now < end, not completed),
     *        "ended" (end passed but snapshot not yet refreshed), "completed" (server status Completed).
     */
    live: function (plan, nowMs) {
      var start = Date.parse(plan.startDateTime);
      var end = Date.parse(plan.endDateTime);
      var window = Math.max(1, end - start);
      var elapsedPct = Math.max(0, Math.min(100, (nowMs - start) / window * 100));
      if (plan.status === "Completed") {
        return { phase: "completed", restSeconds: null, untilStartSeconds: null, elapsedPct: elapsedPct };
      }
      if (nowMs < start) {
        return { phase: "upcoming", restSeconds: null, untilStartSeconds: Math.ceil((start - nowMs) / 1000), elapsedPct: 0 };
      }
      if (nowMs < end) {
        return { phase: "active", restSeconds: Math.ceil((end - nowMs) / 1000), untilStartSeconds: null, elapsedPct: elapsedPct };
      }
      return { phase: "ended", restSeconds: 0, untilStartSeconds: null, elapsedPct: 100 };
    },

    /** Seconds → "HH:MM:SS", or "Nd HH:MM:SS" when a day or more. Negative → 00:00:00. */
    clock: function (seconds) {
      seconds = Math.max(0, Math.floor(seconds || 0));
      var d = Math.floor(seconds / 86400);
      var h = Math.floor(seconds % 86400 / 3600);
      var m = Math.floor(seconds % 3600 / 60);
      var s = seconds % 60;
      return (d ? d + "d " : "") + pad(h) + ":" + pad(m) + ":" + pad(s);
    },

    /** Seconds → short human text: "45 s", "5 min 3 s", "2 h 5 min", "3 d 4 h". */
    humanize: function (seconds) {
      seconds = Math.max(0, Math.floor(seconds || 0));
      var d = Math.floor(seconds / 86400);
      var h = Math.floor(seconds % 86400 / 3600);
      var m = Math.floor(seconds % 3600 / 60);
      var s = seconds % 60;
      if (d) return d + " d" + (h ? " " + h + " h" : "");
      if (h) return h + " h" + (m ? " " + m + " min" : "");
      if (m) return m + " min" + (s ? " " + s + " s" : "");
      return s + " s";
    },

    /** Done items / total items × 100 (0 when there are no items). */
    completionPct: function (done, total) {
      return total > 0 ? Math.round(done / total * 10000) / 100 : 0;
    }
  };

  window.PlanTime = PlanTime;
})(window);
