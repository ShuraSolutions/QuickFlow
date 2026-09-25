/* Learning Resources page: card grid, add/edit/remove cards, expandable milestones and notes. */
(function (window, $) {
  "use strict";
  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  var TITLE_MAX = 200;
  var DESCRIPTION_MAX = 2000;
  var MILESTONE_MAX = 200;
  var NOTE_MAX = 4000;
  var STATUSES = ["NotStarted", "InProgress", "Completed"];
  var STATUS_BADGE = { NotStarted: "", InProgress: "amber", Completed: "green" };

  var statusFilter = "";
  var expanded = {}; // card id → true
  var $root, $list, requestSeq = 0;

  function statusOptions(selected, allLabel) {
    return (allLabel ? '<option value="">' + allLabel + "</option>" : "") + STATUSES.map(function (s) {
      return '<option value="' + s + '"' + (s === selected ? " selected" : "") + ">" + Fmt.enumLabel(s) + "</option>";
    }).join("");
  }

  function milestoneItem(card, m) {
    var title = UI.esc(m.title);
    var id = "ms-" + m.id;
    return '<li class="' + (m.isDone ? "done" : "") + '" data-milestone-id="' + m.id + '">' +
      '<input type="checkbox" id="' + id + '" data-action="toggle-milestone"' + (m.isDone ? " checked" : "") + ">" +
      '<label class="grow" for="' + id + '"><span class="item-title">' + title + "</span>" +
        (m.targetDate ? '<span class="meta"> · target ' + UI.esc(Fmt.date(m.targetDate)) + "</span>" : "") +
        (m.isDone && m.completedAt ? '<span class="meta"> · done ' + UI.esc(Fmt.dateTime(m.completedAt)) + "</span>" : "") +
      "</label>" +
      '<button type="button" class="icon-btn danger" data-action="remove-milestone" aria-label="Remove milestone ' + title + '">✕</button></li>';
  }

  function noteItem(n) {
    return '<li data-note-id="' + n.id + '"><div class="grow"><div class="note-text">' + UI.esc(n.text) + "</div>" +
      '<div class="meta note-time">' + UI.esc(Fmt.dateTime(n.createdAt)) + "</div></div>" +
      '<button type="button" class="icon-btn danger" data-action="remove-note" aria-label="Remove note ' + UI.esc(n.text.slice(0, 40)) + '">✕</button></li>';
  }

  function detailsHtml(c) {
    var milestones = c.milestones.length
      ? '<ul class="item-list milestones" aria-label="Milestones of ' + UI.esc(c.title) + '">' + c.milestones.map(function (m) { return milestoneItem(c, m); }).join("") + "</ul>"
      : '<p class="empty-inline">No milestones yet — break this resource into steps below.</p>';
    var notes = c.notes.length
      ? '<ul class="item-list notes" aria-label="Notes of ' + UI.esc(c.title) + '">' + c.notes.map(noteItem).join("") + "</ul>"
      : '<p class="empty-inline">No notes yet — jot down what you learn below.</p>';
    return '<div class="details">' +
      "<section><h3>Milestones</h3>" + milestones +
        '<form class="inline-form" data-form="milestone" novalidate>' +
          '<input type="text" name="title" maxlength="' + MILESTONE_MAX + '" placeholder="New milestone…" aria-label="New milestone title">' +
          '<input type="date" name="targetDate" aria-label="Milestone target date">' +
          '<button type="submit" class="btn btn-sm">Add milestone</button></form></section>' +
      "<section><h3>Notes</h3>" + notes +
        '<form class="inline-form" data-form="note" novalidate>' +
          '<textarea name="text" maxlength="' + NOTE_MAX + '" placeholder="Write a note…" aria-label="New note text"></textarea>' +
          '<button type="submit" class="btn btn-sm">Add note</button></form></section>' +
    "</div>";
  }

  function cardEl(c) {
    var title = UI.esc(c.title);
    var open = !!expanded[c.id];
    var $card = $(
      '<article class="card learning-card" data-id="' + c.id + '" aria-label="Learning card ' + title + '">' +
        '<div class="card-head"><div class="card-title">' + title + "</div>" + UI.badge(Fmt.enumLabel(c.status), STATUS_BADGE[c.status]) + "</div>" +
        (c.description ? '<div class="card-desc">' + UI.esc(c.description) + "</div>" : "") +
        '<div><div class="progress-label"><span class="milestone-count">' + c.milestonesDone + " / " + c.milestonesTotal + " milestones</span>" +
          '<span class="milestone-pct">' + Fmt.pct(c.milestoneProgressPercentage) + "</span></div>" +
          UI.progress(c.milestoneProgressPercentage, c.milestoneProgressPercentage >= 100 ? "success" : "", "Milestone progress of " + c.title) + "</div>" +
        '<div class="stats"><span class="notes-count">📝 ' + Fmt.plural(c.notesCount, "note") + "</span><span>Added " + UI.esc(Fmt.dateTime(c.createdAt)) + "</span></div>" +
        (open ? detailsHtml(c) : "") +
        '<div class="card-actions">' +
          '<button type="button" class="btn btn-sm" data-action="expand" aria-expanded="' + open + '" aria-label="' + (open ? "Hide" : "Show") + " milestones and notes of " + title + '">' +
            (open ? "Hide details" : "Milestones & notes") + "</button>" +
          '<span class="spacer"></span>' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="edit" aria-label="Edit ' + title + '">Edit</button>' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="remove" aria-label="Remove ' + title + '">Remove</button>' +
        "</div>" +
      "</article>");
    if (open) $card.addClass("is-expanded");
    $card.data("card", c);
    return $card;
  }

  function renderList(cards) {
    $list.empty();
    if (!cards.length) {
      $list.append(statusFilter
        ? UI.emptyState({ icon: "🔍", title: "No " + Fmt.enumLabel(statusFilter).toLowerCase() + " cards", text: "Change the filter or add a new learning card.", actionLabel: "Add Learning Card", onAction: function () { openForm(); } })
        : UI.emptyState({ icon: "📚", title: "No learning cards yet", text: "Add a course, book or topic you are learning and track it with milestones and notes.", actionLabel: "Add Learning Card", onAction: function () { openForm(); } }));
      return;
    }
    var $grid = $('<div class="grid" role="list"></div>');
    cards.forEach(function (c) { $grid.append(cardEl(c).attr("role", "listitem")); });
    $list.append($grid);
  }

  function load() {
    var seq = ++requestSeq;
    if (!$list.children().length) $list.append(UI.loading("Loading learning cards…"));
    return Api.learning.list({ status: statusFilter }).then(function (cards) {
      if (seq === requestSeq) renderList(cards || []);
    }, function (err) {
      if (seq === requestSeq) $list.empty().append(UI.errorBox(err, load));
    });
  }

  /** Re-fetches one card and swaps it in place (keeps other cards and scroll position). */
  function refreshCard(cardId, focusSelector) {
    return Api.learning.get(cardId).then(function (c) {
      var $old = $list.find('.learning-card[data-id="' + cardId + '"]');
      var $new = cardEl(c).attr("role", "listitem");
      $old.replaceWith($new);
      if (focusSelector) $new.find(focusSelector).first().trigger("focus");
    }, function (err) { UI.toastError(err, "Could not refresh card"); });
  }

  function validateCard(data) {
    var errors = {};
    if (!data.title) errors.title = ["Title is required."];
    else if (data.title.length > TITLE_MAX) errors.title = ["Title must be at most " + TITLE_MAX + " characters."];
    if (data.description && data.description.length > DESCRIPTION_MAX) errors.description = ["Description must be at most " + DESCRIPTION_MAX + " characters."];
    if (STATUSES.indexOf(data.status) < 0) errors.status = ["Choose a valid status."];
    return errors;
  }

  function openForm(card) {
    var editing = !!card;
    var $body = $(
      '<div class="form-grid">' +
        '<div class="field span-2"><label for="lf-title">Title</label>' +
          '<input type="text" id="lf-title" name="title" maxlength="' + TITLE_MAX + '" required placeholder="e.g. Clean Code (book)">' +
          '<span class="hint">Required, up to ' + TITLE_MAX + ' characters.</span></div>' +
        '<div class="field span-2"><label for="lf-description">Description / source</label>' +
          '<textarea id="lf-description" name="description" maxlength="' + DESCRIPTION_MAX + '" rows="3" placeholder="Link, author, course platform…"></textarea></div>' +
        '<div class="field"><label for="lf-status">Status</label><select id="lf-status" name="status">' + statusOptions(card ? card.status : "NotStarted") + "</select></div>" +
      "</div>");
    if (card) {
      $body.find("#lf-title").val(card.title);
      $body.find("#lf-description").val(card.description || "");
    }
    UI.modal({
      title: editing ? "Edit learning card" : "Add learning card",
      body: $body,
      submitLabel: editing ? "Save changes" : "Add card",
      onSubmit: function ($form) {
        var data = {
          title: $.trim($form.find("#lf-title").val()),
          description: $.trim($form.find("#lf-description").val()) || null,
          status: $form.find("#lf-status").val()
        };
        var errors = validateCard(data);
        if (Object.keys(errors).length) { UI.showErrors($form, errors); return false; }
        UI.clearErrors($form);
        var call = editing ? Api.learning.update(card.id, data) : Api.learning.create(data);
        return call.then(function (saved) {
          UI.toast(editing ? "Learning card updated." : "Learning card \"" + saved.title + "\" added.", { type: "success" });
          load();
        }, function (err) {
          UI.showServerError($form, err);
          throw err;
        });
      }
    });
  }

  function onCardAction(e) {
    var $btn = $(e.currentTarget);
    var $card = $btn.closest(".learning-card");
    var card = $card.data("card");
    if (!card) return;
    var action = $btn.data("action");

    if (action === "expand") {
      expanded[card.id] = !expanded[card.id];
      var $new = cardEl(card).attr("role", "listitem");
      $card.replaceWith($new);
      $new.find('[data-action="expand"]').trigger("focus");
      return;
    }
    if (action === "edit") return openForm(card);
    if (action === "remove") {
      return UI.confirm("Remove \"" + card.title + "\" with its milestones and notes? This cannot be undone.", { title: "Remove learning card", confirmLabel: "Remove" })
        .then(function (ok) {
          if (!ok) return;
          return Api.learning.remove(card.id).then(function () {
            delete expanded[card.id];
            UI.toast("Learning card removed.", { type: "success" });
            load();
          }, function (err) { UI.toastError(err, "Could not remove card"); });
        });
    }
    if (action === "toggle-milestone") {
      var msId = $btn.closest("li").data("milestone-id");
      var isDone = $btn.prop("checked");
      $btn.prop("disabled", true);
      return Api.learning.updateMilestone(card.id, msId, { isDone: isDone }).then(function () {
        refreshCard(card.id, '[data-milestone-id="' + msId + '"] input');
      }, function (err) {
        $btn.prop({ disabled: false, checked: !isDone });
        UI.toastError(err, "Could not update milestone");
      });
    }
    if (action === "remove-milestone") {
      return Api.learning.removeMilestone(card.id, $btn.closest("li").data("milestone-id")).then(function () {
        UI.toast("Milestone removed.", { type: "success" });
        refreshCard(card.id);
      }, function (err) { UI.toastError(err, "Could not remove milestone"); });
    }
    if (action === "remove-note") {
      return Api.learning.removeNote(card.id, $btn.closest("li").data("note-id")).then(function () {
        UI.toast("Note removed.", { type: "success" });
        refreshCard(card.id);
      }, function (err) { UI.toastError(err, "Could not remove note"); });
    }
  }

  function onInlineSubmit(e) {
    e.preventDefault();
    var $form = $(e.currentTarget);
    var card = $form.closest(".learning-card").data("card");
    var kind = $form.data("form");
    UI.clearErrors($form);
    var call;
    if (kind === "milestone") {
      var title = $.trim($form.find('[name="title"]').val());
      if (!title) { UI.showErrors($form, { title: ["Milestone title is required."] }); return; }
      if (title.length > MILESTONE_MAX) { UI.showErrors($form, { title: ["Milestone title must be at most " + MILESTONE_MAX + " characters."] }); return; }
      call = Api.learning.addMilestone(card.id, { title: title, targetDate: $form.find('[name="targetDate"]').val() || null });
    } else {
      var text = $.trim($form.find('[name="text"]').val());
      if (!text) { UI.showErrors($form, { text: ["Note text is required."] }); return; }
      if (text.length > NOTE_MAX) { UI.showErrors($form, { text: ["Note must be at most " + NOTE_MAX + " characters."] }); return; }
      call = Api.learning.addNote(card.id, { text: text });
    }
    var $submit = $form.find('[type="submit"]').prop("disabled", true);
    call.then(function () {
      UI.toast(kind === "milestone" ? "Milestone added." : "Note added.", { type: "success" });
      refreshCard(card.id, '[data-form="' + kind + '"] ' + (kind === "milestone" ? 'input[name="title"]' : "textarea"));
    }, function (err) {
      $submit.prop("disabled", false);
      UI.showServerError($form, err);
    });
  }

  App.pages.learning = {
    title: "Learning Resources",
    render: function ($outlet, ctx) {
      $root = $outlet;
      var $add = $('<button type="button" class="btn btn-primary">+ Add Learning Card</button>').on("click", function () { openForm(); });
      $root.append(UI.pageHeader("Learning Resources", "Courses, books and topics you are learning.", $add));
      var $tb = $('<div class="toolbar"><div class="field"><label for="learning-status">Status</label><select id="learning-status">' +
        statusOptions(statusFilter, "All statuses") + "</select></div></div>");
      $tb.find("select").on("change", function () { statusFilter = this.value; load(); });
      $root.append($tb);
      $list = $('<div id="learning-results"></div>').appendTo($root);
      $list.on("click", "[data-action]:not(input)", onCardAction);
      $list.on("change", 'input[data-action="toggle-milestone"]', onCardAction);
      $list.on("submit", "form.inline-form", onInlineSubmit);
      load();
      if (ctx.query["new"]) {
        Router.clearQuery();
        openForm();
      }
    },
    destroy: function () { requestSeq++; }
  };
})(window, jQuery);
