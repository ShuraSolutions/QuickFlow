/*
 * Dashboard: greeting, summary metric cards, today's/overdue/completed-today tasks, today's habit checklist,
 * active/upcoming plans with live rest time, learning snapshot and quick-add actions. All numbers come from
 * GET /api/dashboard; the page refreshes every 60 s and ticks rest times every second.
 */
(function (window, $) {
  "use strict";
  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  var REFRESH_MS = 60000;
  var data = null, skew = 0;
  var $root, $body, ticker = null, refresher = null, requestSeq = 0, reloadPending = false;

  function greeting(d) {
    var h = d.getHours();
    return h < 12 ? "Good morning" : h < 18 ? "Good afternoon" : "Good evening";
  }

  function metricCard(key, label, value, sub, pct, href) {
    return '<a class="card metric-card" data-metric="' + key + '" href="' + href + '">' +
      '<div class="metric-label">' + UI.esc(label) + "</div>" +
      '<div class="metric-value">' + UI.esc(value) + "</div>" +
      '<div class="metric-sub">' + UI.esc(sub) + "</div>" +
      (pct === null ? "" : UI.progress(pct, "thin", label + " progress")) + "</a>";
  }

  function metricsHtml(s) {
    return '<div class="metrics">' +
      metricCard("tasks", "Tasks", Fmt.pct(s.tasks.completionPercentage) + " done",
        s.tasks.done + " / " + s.tasks.total + " completed · " + s.tasks.dueToday + " due today · " + s.tasks.overdue + " overdue",
        s.tasks.completionPercentage, "#/tasks") +
      metricCard("habits", "Habits today", s.habits.completedToday + " / " + s.habits.active,
        Fmt.pct(s.habits.completionPercentage) + " of active habits completed", s.habits.completionPercentage, "#/habits") +
      metricCard("plans", "Plans", s.plans.inProgress + " in progress",
        s.plans.upcoming + " upcoming · " + s.plans.completed + " completed · avg " + Fmt.pct(s.plans.averageActiveCompletionPercentage) + " done",
        s.plans.averageActiveCompletionPercentage, "#/plans") +
      metricCard("learning", "Learning", s.learning.inProgress + " in progress",
        s.learning.milestonesDone + " / " + s.learning.milestonesTotal + " milestones (" + Fmt.pct(s.learning.milestoneCompletionPercentage) + ") · " +
        s.learning.milestonesCompletedLast7Days + " in last 7 days", s.learning.milestoneCompletionPercentage, "#/learning") +
      "</div>";
  }

  function taskList(key, title, tasks, emptyText, variant) {
    var html = '<section class="card panel" data-list="' + key + '"><div class="panel-head"><h2>' + UI.esc(title) + "</h2>" +
      UI.badge(String(tasks.length), variant) + "</div>";
    if (!tasks.length) return html + '<p class="empty-inline">' + emptyText + "</p></section>";
    return html + '<ul class="item-list">' + tasks.map(function (t) {
      var done = t.status === "Done";
      var name = UI.esc(t.title);
      return '<li class="' + (done ? "done" : "") + '" data-task-id="' + t.id + '">' +
        (done ? '<span class="done-mark" aria-hidden="true">✓</span>'
              : '<button type="button" class="complete-toggle" data-action="complete-task" aria-label="Complete ' + name + '">✓</button>') +
        '<span class="grow"><span class="item-title">' + name + "</span>" +
        '<span class="meta"> · ' + UI.esc(t.priority) + (t.dueDate ? " · due " + UI.esc(Fmt.relativeDate(t.dueDate)) : "") + "</span></span></li>";
    }).join("") + "</ul></section>";
  }

  function habitsPanel(habits) {
    var html = '<section class="card panel" data-list="habits"><div class="panel-head"><h2>Today\'s habits</h2>' +
      UI.badge(habits.filter(function (h) { return h.completedToday; }).length + " / " + habits.length, "green") + "</div>";
    if (!habits.length) return html + '<p class="empty-inline">No active habits. <a href="#/habits?new=1">Add a habit</a>.</p></section>';
    return html + '<ul class="item-list checklist">' + habits.map(function (h) {
      var id = "dh-" + h.id;
      return '<li class="' + (h.completedToday ? "done" : "") + '" data-habit-id="' + h.id + '">' +
        '<input type="checkbox" id="' + id + '" data-action="toggle-habit"' + (h.completedToday ? " checked" : "") + ">" +
        '<label class="grow" for="' + id + '"><span class="item-title">' + UI.esc(h.name) + '</span><span class="meta"> · ' + h.frequency +
        " · 🔥 " + h.currentStreak + "</span></label></li>";
    }).join("") + "</ul></section>";
  }

  function planRow(p) {
    var l = PlanTime.live(p, Date.now() + skew);
    var live = l.phase === "upcoming"
      ? '<span class="starts-in">Starts in <strong>' + PlanTime.humanize(l.untilStartSeconds) + "</strong></span>"
      : '<span class="rest-label">Rest</span> <span class="rest-time small-rest" data-seconds="' + l.restSeconds + '">' + PlanTime.clock(l.restSeconds) + "</span>";
    return '<li class="plan-row" data-plan-id="' + p.id + '"><div class="grow"><div class="row"><span class="priority-chip">#' + p.priorityOrder + "</span>" +
      '<a class="item-title" href="#/plans">' + UI.esc(p.title) + "</a></div>" +
      '<div class="progress-label"><span class="plan-progress-text">' + p.doneItems + " / " + p.totalItems + ' items</span><span class="plan-pct">' + Fmt.pct(p.completionPercentage) + "</span></div>" +
      UI.progress(p.completionPercentage, "", "Completion of " + p.title) + "</div>" +
      '<div class="plan-live" data-live>' + live + "</div></li>";
  }

  function plansPanel(active, upcoming) {
    var html = '<section class="card panel wide" data-list="plans"><div class="panel-head"><h2>Active plans</h2>' + UI.badge(String(active.length), "amber") + "</div>";
    html += active.length ? '<ul class="item-list plan-rows" data-group="active">' + active.map(planRow).join("") + "</ul>"
      : '<p class="empty-inline">No plan in progress. <a href="#/plans?new=1">Create a plan</a>.</p>';
    if (upcoming.length) html += '<h3 class="subhead">Upcoming</h3><ul class="item-list plan-rows" data-group="upcoming">' + upcoming.map(planRow).join("") + "</ul>";
    return html + "</section>";
  }

  function learningPanel(cards, s) {
    var html = '<section class="card panel" data-list="learning"><div class="panel-head"><h2>Learning snapshot</h2>' +
      UI.badge(Fmt.plural(s.milestonesCompletedLast7Days, "milestone") + " done in 7 days", "purple") + "</div>";
    if (!cards.length) return html + '<p class="empty-inline">No learning card in progress. <a href="#/learning?new=1">Add a learning card</a>.</p></section>';
    return html + '<ul class="item-list">' + cards.map(function (c) {
      return '<li data-card-id="' + c.id + '"><div class="grow"><div class="progress-label"><span class="item-title">' + UI.esc(c.title) + "</span>" +
        '<span class="card-progress">' + c.milestonesDone + " / " + c.milestonesTotal + " · " + Fmt.pct(c.milestoneProgressPercentage) + "</span></div>" +
        UI.progress(c.milestoneProgressPercentage, "", "Milestones of " + c.title) + "</div></li>";
    }).join("") + "</ul></section>";
  }

  function render() {
    var d = data;
    $root.find(".greeting-name").text(greeting(new Date()) + ", " + d.displayName);
    $body.html(
      metricsHtml(d.summary) +
      '<div class="dash-grid">' +
        taskList("due-today", "Today's tasks", d.tasksDueToday, 'Nothing due today. <a href="#/tasks?new=1">Add a task</a>.', "blue") +
        habitsPanel(d.habitsToday) +
        taskList("overdue", "Overdue", d.overdueTasks, "No overdue tasks. 🎉", "red") +
        taskList("completed-today", "Completed today", d.tasksCompletedToday, "Nothing completed yet today.", "green") +
        plansPanel(d.activePlans, d.upcomingPlans) +
        learningPanel(d.learningInProgress, d.summary.learning) +
      "</div>");
  }

  function load() {
    var seq = ++requestSeq;
    reloadPending = false;
    if (!data) $body.html(UI.loading("Loading your day…"));
    return Api.dashboard.get().then(function (d) {
      if (seq !== requestSeq) return;
      data = d;
      skew = PlanTime.skew(d.generatedAt, Date.now());
      render();
    }, function (err) {
      if (seq === requestSeq) $body.empty().append(UI.errorBox(err, load));
    });
  }

  function tick() {
    if (!data) return;
    data.activePlans.concat(data.upcomingPlans).forEach(function (p) {
      var $row = $body.find('.plan-row[data-plan-id="' + p.id + '"]');
      if (!$row.length) return;
      var l = PlanTime.live(p, Date.now() + skew);
      $row.find("[data-live]").html(l.phase === "upcoming"
        ? '<span class="starts-in">Starts in <strong>' + PlanTime.humanize(l.untilStartSeconds) + "</strong></span>"
        : '<span class="rest-label">Rest</span> <span class="rest-time small-rest" data-seconds="' + l.restSeconds + '">' + PlanTime.clock(l.restSeconds) + "</span>");
      var serverPhase = p.status === "NotStarted" ? "upcoming" : "active";
      if (l.phase !== serverPhase && !reloadPending) { reloadPending = true; setTimeout(load, 500); }
    });
  }

  function onAction(e) {
    var $el = $(e.currentTarget);
    var action = $el.data("action");
    if (action === "complete-task") {
      var taskId = $el.closest("li").data("task-id");
      $el.prop("disabled", true);
      return Api.tasks.complete(taskId).then(function () {
        UI.toast("Task completed.", { type: "success" });
        load();
      }, function (err) { $el.prop("disabled", false); UI.toastError(err, "Could not complete task"); });
    }
    if (action === "toggle-habit") {
      var habitId = $el.closest("li").data("habit-id");
      var checked = $el.prop("checked");
      $el.prop("disabled", true);
      var today = Fmt.todayIso();
      var call = checked ? Api.habits.complete(habitId, today) : Api.habits.uncomplete(habitId, today);
      return call.then(load, function (err) {
        if (err.status === 409) { load(); return; }
        $el.prop({ disabled: false, checked: !checked });
        UI.toastError(err, "Could not update habit");
      });
    }
  }

  App.pages.dashboard = {
    title: "Dashboard",
    render: function ($outlet) {
      $root = $('<div class="dashboard-page"></div>').appendTo($outlet);
      var $quick = $('<div class="quick-add" role="group" aria-label="Quick add">' +
        '<a class="btn btn-sm" href="#/tasks?new=1">+ Task</a><a class="btn btn-sm" href="#/habits?new=1">+ Habit</a>' +
        '<a class="btn btn-sm" href="#/learning?new=1">+ Learning card</a><a class="btn btn-sm btn-primary" href="#/plans?new=1">+ Plan</a></div>');
      var $header = UI.pageHeader("Dashboard", Fmt.longDate(), $quick);
      $header.find("h1").after('<p class="greeting-name"></p>');
      $root.append($header);
      $body = $('<div id="dashboard-body"></div>').appendTo($root);
      $body.on("click", 'button[data-action="complete-task"]', onAction);
      $body.on("change", 'input[data-action="toggle-habit"]', onAction);
      $(document).on("plan:started.dashboard", load);
      data = null;
      load();
      ticker = setInterval(tick, 1000);
      refresher = setInterval(load, REFRESH_MS);
    },
    destroy: function () {
      clearInterval(ticker);
      clearInterval(refresher);
      $(document).off(".dashboard");
      requestSeq++;
    }
  };
})(window, jQuery);
