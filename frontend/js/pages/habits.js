/* Habits page: habit cards with today's completion toggle, streaks, add/edit, deactivate/activate, remove. */
(function (window, $) {
  "use strict";
  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  var NAME_MAX = 150;
  var DESCRIPTION_MAX = 2000;
  var FREQUENCIES = ["Daily", "Weekly"];

  var show = "active"; // active | inactive | all
  var $root, $summary, $list, requestSeq = 0;

  function visible(habits) {
    if (show === "all") return habits;
    return habits.filter(function (h) { return show === "active" ? h.isActive : !h.isActive; });
  }

  function renderSummary(habits) {
    var active = habits.filter(function (h) { return h.isActive; });
    var done = active.filter(function (h) { return h.completedToday; }).length;
    var pct = active.length ? done / active.length * 100 : 0;
    $summary.find(".summary-text").text("Completed today: " + done + " / " + active.length + " active");
    $summary.find(".progress-slot").html(UI.progress(pct, "success", "Habits completed today"));
  }

  function habitCard(h) {
    var name = UI.esc(h.name);
    var toggle = "";
    if (h.isActive) {
      toggle = h.completedToday
        ? '<button type="button" class="btn btn-success" data-action="undo" aria-pressed="true" aria-label="Undo today\'s completion of ' + name + '">✓ Done today · Undo</button>'
        : '<button type="button" class="btn btn-primary" data-action="complete" aria-pressed="false" aria-label="Complete ' + name + ' for today">Mark done today</button>';
    }
    var periodBadge = h.frequency === "Weekly" && h.completedThisPeriod ? UI.badge("Done this week", "green") : "";
    var $card = $(
      '<article class="card habit-card" data-id="' + h.id + '" aria-label="Habit ' + name + '">' +
        '<div class="card-head"><div class="card-title">' + name + "</div>" +
          '<div class="row">' + UI.badge(h.frequency, h.frequency === "Daily" ? "blue" : "purple") +
            (h.isActive ? "" : UI.badge("Inactive")) + periodBadge + "</div></div>" +
        (h.description ? '<div class="card-desc">' + UI.esc(h.description) + "</div>" : "") +
        '<div class="stats">' +
          '<span class="streak">🔥 Streak <strong>' + h.currentStreak + "</strong></span>" +
          "<span>Total <strong>" + h.totalCompletions + "</strong></span>" +
          "<span>Last done <strong>" + (h.lastCompletedDate ? UI.esc(Fmt.relativeDate(h.lastCompletedDate)) : "never") + "</strong></span>" +
        "</div>" +
        (toggle ? '<div>' + toggle + "</div>" : "") +
        '<div class="card-actions">' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="edit" aria-label="Edit ' + name + '">Edit</button>' +
          (h.isActive
            ? '<button type="button" class="btn btn-sm btn-ghost" data-action="deactivate" aria-label="Deactivate ' + name + '">Deactivate</button>'
            : '<button type="button" class="btn btn-sm btn-ghost" data-action="activate" aria-label="Activate ' + name + '">Activate</button>') +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="remove" aria-label="Remove ' + name + '">Remove</button>' +
        "</div>" +
      "</article>");
    if (h.completedToday) $card.addClass("is-done");
    if (!h.isActive) $card.addClass("is-inactive");
    $card.data("habit", h);
    return $card;
  }

  function renderList(habits) {
    renderSummary(habits);
    var items = visible(habits);
    $list.empty();
    if (!habits.length) {
      $list.append(UI.emptyState({
        icon: "🌱", title: "No habits yet", text: "Add a daily or weekly habit and check it off each day.",
        actionLabel: "Add Habit", onAction: function () { openForm(); }
      }));
      return;
    }
    if (!items.length) {
      $list.append(UI.emptyState({
        icon: "🔍", title: show === "inactive" ? "No inactive habits" : "No active habits",
        text: "Change the filter or add a new habit.", actionLabel: "Add Habit", onAction: function () { openForm(); }
      }));
      return;
    }
    var $grid = $('<div class="grid" role="list"></div>');
    items.forEach(function (h) { $grid.append(habitCard(h).attr("role", "listitem")); });
    $list.append($grid);
  }

  function load() {
    var seq = ++requestSeq;
    if (!$list.children().length) $list.append(UI.loading("Loading habits…"));
    return Api.habits.list().then(function (habits) {
      if (seq === requestSeq) renderList(habits || []);
    }, function (err) {
      if (seq === requestSeq) $list.empty().append(UI.errorBox(err, load));
    });
  }

  /** Client-side rules mirroring BR-6; the server stays authoritative. */
  function validate(data) {
    var errors = {};
    if (!data.name) errors.name = ["Name is required."];
    else if (data.name.length > NAME_MAX) errors.name = ["Name must be at most " + NAME_MAX + " characters."];
    if (data.description && data.description.length > DESCRIPTION_MAX) errors.description = ["Description must be at most " + DESCRIPTION_MAX + " characters."];
    if (FREQUENCIES.indexOf(data.frequency) < 0) errors.frequency = ["Choose Daily or Weekly."];
    return errors;
  }

  function openForm(habit) {
    var editing = !!habit;
    var freq = habit ? habit.frequency : "Daily";
    var $body = $(
      '<div class="form-grid">' +
        '<div class="field span-2"><label for="hf-name">Name</label>' +
          '<input type="text" id="hf-name" name="name" maxlength="' + NAME_MAX + '" required>' +
          '<span class="hint">Required, up to ' + NAME_MAX + ' characters.</span></div>' +
        '<div class="field span-2"><label for="hf-description">Description</label>' +
          '<textarea id="hf-description" name="description" maxlength="' + DESCRIPTION_MAX + '" rows="3"></textarea></div>' +
        '<div class="field"><label for="hf-frequency">Frequency</label><select id="hf-frequency" name="frequency">' +
          FREQUENCIES.map(function (f) { return '<option value="' + f + '"' + (f === freq ? " selected" : "") + ">" + f + "</option>"; }).join("") +
        "</select></div>" +
      "</div>");
    if (habit) {
      $body.find("#hf-name").val(habit.name);
      $body.find("#hf-description").val(habit.description || "");
    }
    UI.modal({
      title: editing ? "Edit habit" : "Add habit",
      body: $body,
      submitLabel: editing ? "Save changes" : "Add habit",
      onSubmit: function ($form) {
        var data = {
          name: $.trim($form.find("#hf-name").val()),
          description: $.trim($form.find("#hf-description").val()) || null,
          frequency: $form.find("#hf-frequency").val()
        };
        var errors = validate(data);
        if (Object.keys(errors).length) { UI.showErrors($form, errors); return false; }
        UI.clearErrors($form);
        var call = editing ? Api.habits.update(habit.id, data) : Api.habits.create(data);
        return call.then(function (saved) {
          UI.toast(editing ? "Habit updated." : "Habit \"" + saved.name + "\" added.", { type: "success" });
          load();
        }, function (err) {
          UI.showServerError($form, err);
          throw err;
        });
      }
    });
  }

  function onAction(e) {
    var $btn = $(e.currentTarget);
    var habit = $btn.closest(".habit-card").data("habit");
    if (!habit) return;
    var action = $btn.data("action");
    var today = Fmt.todayIso();
    if (action === "edit") return openForm(habit);
    if (action === "remove") {
      return UI.confirm("Remove \"" + habit.name + "\" and its completion history? This cannot be undone.", { title: "Remove habit", confirmLabel: "Remove" })
        .then(function (ok) {
          if (!ok) return;
          return Api.habits.remove(habit.id).then(function () {
            UI.toast("Habit removed.", { type: "success" });
            load();
          }, function (err) { UI.toastError(err, "Could not remove habit"); });
        });
    }
    var call, message;
    if (action === "complete") { call = Api.habits.complete(habit.id, today); message = "Nice! \"" + habit.name + "\" done for today."; }
    else if (action === "undo") { call = Api.habits.uncomplete(habit.id, today); message = "Today's completion removed."; }
    else if (action === "deactivate") { call = Api.habits.deactivate(habit.id); message = "Habit deactivated."; }
    else if (action === "activate") { call = Api.habits.activate(habit.id); message = "Habit activated."; }
    else return;
    $btn.prop("disabled", true);
    call.then(function () {
      UI.toast(message, { type: "success" });
      load();
    }, function (err) {
      if (err.status === 409) UI.toast("\"" + habit.name + "\" is already completed for today.", { type: "warning" });
      else UI.toastError(err, "Could not update habit");
      load();
    });
  }

  App.pages.habits = {
    title: "Habits",
    render: function ($outlet, ctx) {
      $root = $outlet;
      var $add = $('<button type="button" class="btn btn-primary">+ Add Habit</button>').on("click", function () { openForm(); });
      $root.append(UI.pageHeader("Habits", "Build and track recurring habits.", $add));
      $summary = $(
        '<div class="summary-bar"><span class="summary-text" aria-live="polite"></span><div class="progress-slot" style="flex:1;min-width:160px"></div>' +
        '<div class="field"><label for="habit-show">Show</label><select id="habit-show">' +
          '<option value="active">Active</option><option value="inactive">Inactive</option><option value="all">All</option></select></div></div>');
      $summary.find("#habit-show").val(show).on("change", function () { show = this.value; load(); });
      $root.append($summary);
      $list = $('<div id="habit-results"></div>').appendTo($root);
      $list.on("click", "[data-action]", onAction);
      load();
      if (ctx.query["new"]) {
        Router.clearQuery();
        openForm();
      }
    },
    destroy: function () { requestSeq++; }
  };
})(window, jQuery);
