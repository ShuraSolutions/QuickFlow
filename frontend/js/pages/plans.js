/*
 * Todo Plans page: plan builder (pick existing tasks, habits, learning cards), active/upcoming list with
 * live rest time and progress, per-item done toggles, edit/remove, start highlight, and history.
 */
(function (window, $) {
  "use strict";
  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  var TITLE_MAX = 200;
  var TICK_MS = 1000;
  var RESYNC_MS = 30000;
  var SOURCE_LABEL = { Task: "Task", Habit: "Habit", LearningResource: "Learning" };
  var SOURCE_BADGE = { Task: "blue", Habit: "purple", LearningResource: "amber" };
  var STATUS_BADGE = { NotStarted: "", InProgress: "amber", Completed: "green" };

  var plans = [], history = [], skew = 0;
  var highlight = {}; // plan id → highlight expiry (ms)
  var $root, $active, $history, ticker = null, resync = null, reloadPending = false, requestSeq = 0;

  function now() { return Date.now() + skew; }
  function byId(id) { return plans.filter(function (p) { return p.id === id; })[0]; }

  function windowText(p) {
    var s = new Date(p.startDateTime), e = new Date(p.endDateTime);
    var sameDay = Fmt.isoDate(s) === Fmt.isoDate(e);
    return Fmt.dateTime(p.startDateTime) + " → " + (sameDay ? Fmt.time(p.endDateTime) : Fmt.dateTime(p.endDateTime));
  }

  function liveHtml(p) {
    var l = PlanTime.live(p, now());
    if (l.phase === "upcoming") {
      return '<div class="starts-in">Starts in <strong class="until-start">' + PlanTime.humanize(l.untilStartSeconds) + "</strong></div>";
    }
    if (l.phase === "active" || l.phase === "ended") {
      return '<div class="rest"><span class="rest-label">Rest time</span>' +
        '<span class="rest-time" data-seconds="' + l.restSeconds + '">' + PlanTime.clock(l.restSeconds) + "</span></div>" +
        '<div class="progress-label"><span>Time elapsed</span><span>' + Math.round(l.elapsedPct) + "%</span></div>" +
        UI.progress(l.elapsedPct, "time thin", "Time elapsed");
    }
    return "";
  }

  function itemsHtml(p, editable) {
    if (!p.items.length) return '<p class="empty-inline">This plan has no items left (their sources were deleted).</p>';
    return '<ul class="item-list plan-items" aria-label="Items of ' + UI.esc(p.title) + '">' + p.items.map(function (it) {
      var id = "pi-" + p.id + "-" + it.id;
      var title = it.sourceTitle ? UI.esc(it.sourceTitle) : "<em>(deleted)</em>";
      return '<li class="' + (it.isDone ? "done" : "") + '" data-item-id="' + it.id + '">' +
        (editable
          ? '<input type="checkbox" id="' + id + '" data-action="toggle-item"' + (it.isDone ? " checked" : "") + ">"
          : '<span aria-hidden="true">' + (it.isDone ? "✓" : "○") + "</span>") +
        '<label class="grow" for="' + id + '">' + UI.badge(SOURCE_LABEL[it.sourceType], SOURCE_BADGE[it.sourceType]) +
        ' <span class="item-title">' + title + "</span></label></li>";
    }).join("") + "</ul>";
  }

  function progressHtml(p) {
    return '<div><div class="progress-label"><span class="plan-progress-text">' + p.doneItems + " / " + p.totalItems + ' items done</span>' +
      '<span class="plan-pct">' + Fmt.pct(p.completionPercentage) + "</span></div>" +
      UI.progress(p.completionPercentage, p.completionPercentage >= 100 ? "success" : "", "Completion of " + p.title) + "</div>";
  }

  function planCard(p) {
    var title = UI.esc(p.title);
    var starting = highlight[p.id] && highlight[p.id] > Date.now();
    var $c = $(
      '<article class="card plan-card" data-id="' + p.id + '" aria-label="Plan ' + title + '">' +
        '<div class="card-head"><div class="row"><span class="priority-chip" title="Priority order">#' + p.priorityOrder + "</span>" +
          '<div class="card-title">' + title + "</div></div>" +
          '<div class="row">' + (starting ? UI.badge("⏰ Started", "red") : "") + '<span class="plan-status">' + UI.badge(Fmt.enumLabel(p.status), STATUS_BADGE[p.status]) + "</span></div></div>" +
        '<div class="stats"><span>🗓 ' + UI.esc(windowText(p)) + "</span><span>⏱ Est. " + UI.esc(Fmt.duration(p.estimatedDurationMinutes)) + "</span></div>" +
        '<div class="live" data-live>' + liveHtml(p) + "</div>" +
        progressHtml(p) +
        itemsHtml(p, true) +
        '<div class="card-actions"><span class="spacer"></span>' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="edit" aria-label="Edit ' + title + '">Edit</button>' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="remove" aria-label="Remove ' + title + '">Remove</button>' +
        "</div>" +
      "</article>");
    if (starting) $c.addClass("is-starting");
    $c.data("plan", p);
    return $c;
  }

  function historyCard(p) {
    var title = UI.esc(p.title);
    var reason = p.totalItems > 0 && p.doneItems >= p.totalItems ? "All items done" : "Ended";
    var $c = $(
      '<article class="card plan-card is-history" data-id="' + p.id + '" aria-label="Completed plan ' + title + '">' +
        '<div class="card-head"><div class="card-title">' + title + "</div>" +
          '<div class="row"><span class="plan-status">' + UI.badge("Completed", "green") + "</span></div></div>" +
        '<div class="stats"><span>🗓 ' + UI.esc(windowText(p)) + '</span><span>' + reason + "</span></div>" +
        progressHtml(p) +
        itemsHtml(p, false) +
        '<div class="card-actions"><span class="spacer"></span>' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="remove" aria-label="Remove ' + title + '">Remove</button></div>' +
      "</article>");
    $c.data("plan", p);
    return $c;
  }

  function render() {
    var active = plans.filter(function (p) { return p.status !== "Completed"; });
    $active.empty();
    $history.empty();
    if (!active.length && !history.length) {
      $root.find(".history-section").attr("hidden", true);
      $active.append(UI.emptyState({
        icon: "🗓️", title: "No plans yet",
        text: "Build a time-boxed plan from your existing tasks, habits and learning resources.",
        actionLabel: "Create Plan", onAction: function () { openBuilder(); }
      }));
      return;
    }
    $root.find(".history-section").removeAttr("hidden");
    if (active.length) {
      var $list = $('<div class="plan-list" role="list"></div>');
      active.forEach(function (p) { $list.append(planCard(p).attr("role", "listitem")); });
      $active.append($list);
    } else {
      $active.append(UI.emptyState({ icon: "✅", title: "No active or upcoming plans", text: "Everything is done. Create a new plan to keep going.", actionLabel: "Create Plan", onAction: function () { openBuilder(); } }));
    }
    if (history.length) {
      var $h = $('<div class="grid" role="list"></div>');
      history.forEach(function (p) { $h.append(historyCard(p).attr("role", "listitem")); });
      $history.append($h);
    } else {
      $history.append('<p class="empty-inline">Completed plans and how much of each was done will appear here.</p>');
    }
  }

  function load() {
    var seq = ++requestSeq;
    reloadPending = false;
    if (!$active.children().length) $active.append(UI.loading("Loading plans…"));
    return Promise.all([Api.plans.list(), Api.plans.history()]).then(function (res) {
      if (seq !== requestSeq) return;
      plans = res[0] || [];
      history = res[1] || [];
      var sample = plans[0] || history[0];
      if (sample && sample.serverTime) skew = PlanTime.skew(sample.serverTime, Date.now());
      render();
    }, function (err) {
      if (seq === requestSeq) $active.empty().append(UI.errorBox(err, load));
    });
  }

  /** Every second: re-render live sections; reload when a plan crosses its start or end. */
  function tick() {
    $active.find(".plan-card[data-id]").each(function () {
      var p = byId($(this).data("id"));
      if (!p) return;
      var phase = PlanTime.live(p, now()).phase;
      var serverPhase = p.status === "NotStarted" ? "upcoming" : "active";
      $(this).find("[data-live]").html(liveHtml(p));
      if (phase !== serverPhase && !reloadPending) {
        reloadPending = true;
        setTimeout(load, 500);
      }
    });
  }

  function replacePlan(updated) {
    plans = plans.map(function (p) { return p.id === updated.id ? updated : p; });
    var $old = $active.find('.plan-card[data-id="' + updated.id + '"]');
    $old.replaceWith(planCard(updated).attr("role", "listitem"));
  }

  // ---------- Builder ----------

  function sourceList(kind, items, selected, label, emptyHref) {
    var html = '<fieldset class="picker" data-kind="' + kind + '"><legend>' + label + ' <span class="muted small picker-count"></span></legend>';
    if (!items.length) {
      html += '<p class="empty-inline">None yet — <a href="' + emptyHref + '">add one</a>.</p>';
    } else {
      html += items.map(function (it) {
        var key = kind + ":" + it.id;
        var id = "pick-" + kind + "-" + it.id;
        return '<label class="check picker-option" for="' + id + '"><input type="checkbox" id="' + id + '" value="' + key + '"' +
          (selected[key] ? " checked" : "") + "> <span>" + UI.esc(it.label) + "</span>" + (it.hint ? ' <span class="muted small">' + UI.esc(it.hint) + "</span>" : "") + "</label>";
      }).join("");
    }
    return html + "</fieldset>";
  }

  function validate(d) {
    var e = {};
    if (!d.title) e.title = ["Title is required."];
    else if (d.title.length > TITLE_MAX) e.title = ["Title must be at most " + TITLE_MAX + " characters."];
    if (!(d.estimatedDurationMinutes > 0)) e.estimatedDurationMinutes = ["Estimated duration must be greater than 0."];
    if (!d.startDateTime) e.startDateTime = ["Start date/time is required."];
    if (!d.endDateTime) e.endDateTime = ["End date/time is required."];
    else if (d.startDateTime && Date.parse(d.endDateTime) <= Date.parse(d.startDateTime)) e.endDateTime = ["End must be after the start."];
    if (!(d.priorityOrder >= 0) || Math.floor(d.priorityOrder) !== d.priorityOrder) e.priorityOrder = ["Priority order must be a whole number, 0 or greater."];
    if (!d.items.length) e.items = ["Select at least one task, habit or learning resource."];
    return e;
  }

  function openBuilder(plan) {
    var editing = !!plan;
    Promise.all([Api.tasks.list({ SortBy: "DueDate", SortDir: "Asc" }), Api.habits.list({ isActive: true }), Api.learning.list()]).then(function (res) {
      var selected = {};
      if (plan) plan.items.forEach(function (it) { selected[it.sourceType + ":" + it.sourceId] = true; });
      var tasks = res[0].map(function (t) { return { id: t.id, label: t.title, hint: t.status === "Done" ? "(done)" : (t.dueDate ? "due " + Fmt.relativeDate(t.dueDate) : "") }; });
      var habits = res[1].map(function (h) { return { id: h.id, label: h.name, hint: h.frequency }; });
      var cards = res[2].map(function (c) { return { id: c.id, label: c.title, hint: Fmt.enumLabel(c.status) }; });
      // Keep referenced sources that are no longer listed (e.g. archived tasks) selectable when editing.
      if (plan) plan.items.forEach(function (it) {
        var list = it.sourceType === "Task" ? tasks : it.sourceType === "Habit" ? habits : cards;
        if (!list.some(function (x) { return x.id === it.sourceId; })) list.push({ id: it.sourceId, label: it.sourceTitle || "(deleted)", hint: "currently in plan" });
      });

      var start = plan ? new Date(plan.startDateTime) : new Date();
      var end = plan ? new Date(plan.endDateTime) : new Date(start.getTime() + 60 * 60000);
      var minutes = plan ? plan.estimatedDurationMinutes : 60;
      var nextPriority = plans.concat(history).reduce(function (m, p) { return Math.max(m, p.priorityOrder); }, 0) + 1;

      var $body = $(
        '<div class="form-grid">' +
          '<div class="field span-2"><label for="pf-title">Title</label><input type="text" id="pf-title" name="title" maxlength="' + TITLE_MAX + '" placeholder="e.g. Deep work block"></div>' +
          '<div class="field"><span class="field-label" id="pf-duration-label">Estimated duration</span><div class="row" style="flex-wrap:nowrap">' +
            '<input type="number" id="pf-hours" name="estimatedDurationMinutes" min="0" max="999" step="1" aria-labelledby="pf-duration-label pf-hours-unit">' +
            '<span id="pf-hours-unit">h</span>' +
            '<input type="number" id="pf-minutes" min="0" max="59" step="1" aria-labelledby="pf-duration-label pf-minutes-unit"><span id="pf-minutes-unit">min</span></div></div>' +
          '<div class="field"><label for="pf-priority">Priority order</label><input type="number" id="pf-priority" name="priorityOrder" min="0" step="1">' +
            '<span class="hint">Lower numbers come first.</span></div>' +
          '<div class="field"><label for="pf-start">Start date/time</label><input type="datetime-local" id="pf-start" name="startDateTime" step="1"></div>' +
          '<div class="field"><label for="pf-end">End date/time</label><input type="datetime-local" id="pf-end" name="endDateTime" step="1"></div>' +
          '<div class="field span-2"><span class="field-label">Items <span class="muted small" id="pf-selected">0 selected</span></span>' +
            '<input type="hidden" name="items"><div class="pickers">' +
            sourceList("Task", tasks, selected, "Tasks", "#/tasks?new=1") +
            sourceList("Habit", habits, selected, "Habits", "#/habits?new=1") +
            sourceList("LearningResource", cards, selected, "Learning resources", "#/learning?new=1") +
          "</div></div>" +
        "</div>");
      $body.find("#pf-title").val(plan ? plan.title : "");
      $body.find("#pf-hours").val(Math.floor(minutes / 60));
      $body.find("#pf-minutes").val(minutes % 60);
      $body.find("#pf-priority").val(plan ? plan.priorityOrder : nextPriority);
      $body.find("#pf-start").val(Fmt.toLocalInput(start));
      $body.find("#pf-end").val(Fmt.toLocalInput(end));

      function updateCount() {
        var n = $body.find(".picker input:checked").length;
        $body.find("#pf-selected").text(n + " selected");
        $body.find(".picker").each(function () {
          var k = $(this).find("input:checked").length;
          $(this).find(".picker-count").text(k ? "(" + k + ")" : "");
        });
      }
      $body.on("change", ".picker input", updateCount);
      var modal = null;
      $body.on("click", ".picker a", function () { if (modal) modal.close(); });
      updateCount();

      modal = UI.modal({
        title: editing ? "Edit plan" : "Create plan",
        wide: true,
        body: $body,
        submitLabel: editing ? "Save changes" : "Create plan",
        onSubmit: function ($form) {
          var hours = parseInt($form.find("#pf-hours").val(), 10) || 0;
          var mins = parseInt($form.find("#pf-minutes").val(), 10) || 0;
          var priority = $form.find("#pf-priority").val();
          var data = {
            title: $.trim($form.find("#pf-title").val()),
            estimatedDurationMinutes: hours * 60 + mins,
            startDateTime: Fmt.fromLocalInput($form.find("#pf-start").val()),
            endDateTime: Fmt.fromLocalInput($form.find("#pf-end").val()),
            priorityOrder: priority === "" ? NaN : Number(priority),
            items: $form.find(".picker input:checked").map(function () {
              var parts = this.value.split(":");
              return { sourceType: parts[0], sourceId: Number(parts[1]) };
            }).get()
          };
          var errors = validate(data);
          if (Object.keys(errors).length) { UI.showErrors($form, errors); return false; }
          UI.clearErrors($form);
          var call = editing ? Api.plans.update(plan.id, data) : Api.plans.create(data);
          return call.then(function (saved) {
            UI.toast(editing ? "Plan updated." : "Plan \"" + saved.title + "\" created.", { type: "success" });
            load();
          }, function (err) {
            UI.showServerError($form, err);
            throw err;
          });
        }
      });
    }, function (err) { UI.toastError(err, "Could not load tasks, habits and learning resources"); });
  }

  // ---------- Actions ----------

  function onAction(e) {
    var $el = $(e.currentTarget);
    var plan = $el.closest(".plan-card").data("plan");
    if (!plan) return;
    var action = $el.data("action");
    if (action === "edit") return openBuilder(plan);
    if (action === "remove") {
      return UI.confirm("Remove the plan \"" + plan.title + "\"? Its tasks, habits and learning resources are kept.", { title: "Remove plan", confirmLabel: "Remove" })
        .then(function (ok) {
          if (!ok) return;
          return Api.plans.remove(plan.id).then(function () {
            Notifier.forget(plan.id);
            UI.toast("Plan removed.", { type: "success" });
            load();
          }, function (err) { UI.toastError(err, "Could not remove plan"); });
        });
    }
    if (action === "toggle-item") {
      var itemId = $el.closest("li").data("item-id");
      var isDone = $el.prop("checked");
      $el.prop("disabled", true);
      return Api.plans.setItemDone(plan.id, itemId, isDone).then(function (updated) {
        if (updated.status === "Completed") {
          UI.toast("🎉 Plan \"" + updated.title + "\" completed!", { type: "success" });
          load();
        } else {
          replacePlan(updated);
          $active.find("#pi-" + plan.id + "-" + itemId).trigger("focus");
        }
        $(document).trigger("plan:changed", [updated]);
      }, function (err) {
        $el.prop({ disabled: false, checked: !isDone });
        UI.toastError(err, "Could not update item");
      });
    }
  }

  function onStarted(e, plan) {
    highlight[plan.id] = Date.now() + 60000;
    load();
  }

  App.pages.plans = {
    title: "Todo Plans",
    render: function ($outlet, ctx) {
      $root = $('<div class="plans-page"></div>').appendTo($outlet);
      var $add = $('<button type="button" class="btn btn-primary">+ Create Plan</button>').on("click", function () { openBuilder(); });
      $root.append(UI.pageHeader("Todo Plans", "Time-boxed plans built from your tasks, habits and learning.", $add));
      $root.append(
        '<section class="section" aria-labelledby="plans-active-h"><div class="section-header"><h2 id="plans-active-h">Active &amp; upcoming</h2>' +
          '<span class="muted small">Ordered by priority · rest time updates live</span></div><div id="plans-active"></div></section>' +
        '<section class="section history-section" aria-labelledby="plans-history-h"><div class="section-header"><h2 id="plans-history-h">Completed / history</h2></div>' +
          '<div id="plans-history"></div></section>');
      $active = $root.find("#plans-active");
      $history = $root.find("#plans-history");
      $root.on("click", ".plan-card [data-action]:not(input)", onAction);
      $root.on("change", '.plan-card input[data-action="toggle-item"]', onAction);
      $(document).on("plan:started.plans", onStarted);
      if (ctx.query.highlight) {
        highlight[Number(ctx.query.highlight)] = Date.now() + 60000;
        Router.clearQuery();
      }
      load().then(function () {
        var $h = $active.find(".plan-card.is-starting").first();
        if ($h.length && $h[0].scrollIntoView) $h[0].scrollIntoView({ block: "center" });
      });
      ticker = setInterval(tick, TICK_MS);
      resync = setInterval(load, RESYNC_MS);
      if (ctx.query["new"]) {
        Router.clearQuery();
        openBuilder();
      }
    },
    destroy: function () {
      clearInterval(ticker);
      clearInterval(resync);
      $(document).off(".plans");
      requestSeq++;
    }
  };
})(window, jQuery);
