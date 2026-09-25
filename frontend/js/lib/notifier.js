/*
 * Global plan-start notifier (FR-08). Polls GET /api/plans/notifications — plans whose start time
 * was reached, not yet ended and not yet acknowledged — and shows one sticky toast per plan.
 * Dismissing the toast acknowledges it (POST /api/plans/{id}/notifications/ack) so it is not shown again.
 * Triggers "plan:started" on document so open pages can highlight/refresh the plan.
 */
(function (window, $) {
  "use strict";

  var POLL_MS = 10000;
  var shown = {};   // plan id → toast handle (this page session)
  var timer = null;

  function acknowledge(planId) {
    return Api.plans.ack(planId).then(function () {
      $(document).trigger("plan:acknowledged", [planId]);
    }, function (err) { UI.toastError(err, "Could not dismiss the notification"); });
  }

  var forgotten = {}; // plan id → true when its toast was closed because the plan was removed
  var soonShown = {}; // plan id → true once its "starting soon" notice was shown

  function notify(plan) {
    if (shown[plan.id] || forgotten[plan.id]) return;
    var rest = plan.restTimeSeconds != null ? " — " + PlanTime.humanize(plan.restTimeSeconds) + " left" : "";
    shown[plan.id] = UI.toast("\"" + plan.title + "\" has started" + rest + ".", {
      type: "warning",
      title: "⏰ Plan started",
      id: "plan-start-" + plan.id,
      timeout: 0,
      actions: [
        { label: "View plan", onClick: function () { Router.go("plans", { highlight: plan.id }); } },
        { label: "Dismiss" }
      ],
      onClose: function () { if (!forgotten[plan.id]) acknowledge(plan.id); }
    });
    $(document).trigger("plan:started", [plan]);
  }

  /** "Starting soon" notice for not-yet-started plans within the configured lead minutes (0 = off). */
  function pollSoon() {
    var lead = window.Prefs ? Prefs.leadMinutes() : 0;
    if (!lead) return Promise.resolve([]);
    return Api.plans.list({ status: "NotStarted" }).then(function (plans) {
      (plans || []).forEach(function (p) {
        if (soonShown[p.id] || p.secondsUntilStart == null || p.secondsUntilStart > lead * 60) return;
        soonShown[p.id] = true;
        UI.toast("\"" + p.title + "\" starts in " + PlanTime.humanize(p.secondsUntilStart) + ".", {
          type: "info", title: "⏳ Starting soon", id: "plan-soon-" + p.id, timeout: 15000
        });
      });
      return plans || [];
    }, function () { return []; });
  }

  function poll() {
    if (Notifier.isEnabled && !Notifier.isEnabled()) return Promise.resolve([]);
    pollSoon();
    return Api.plans.notifications().then(function (plans) {
      // A plan whose items are all done (status Completed) needs no "has started" reminder.
      (plans || []).filter(function (p) { return p.status !== "Completed"; }).forEach(notify);
      return plans || [];
    }, function () { return []; });
  }

  var Notifier = {
    /** Optional gate (set by settings): return false to suppress notifications. */
    isEnabled: null,
    poll: poll,
    /** Closes a removed plan's toast without acknowledging it (the plan no longer exists). */
    forget: function (planId) {
      forgotten[planId] = true;
      if (shown[planId]) shown[planId].close();
    },
    start: function () {
      if (timer) return;
      $(document).on("settings:changed.notifier", function () { poll(); });
      poll();
      timer = setInterval(poll, POLL_MS);
    },
    stop: function () { clearInterval(timer); timer = null; }
  };

  window.Notifier = Notifier;
})(window, jQuery);
