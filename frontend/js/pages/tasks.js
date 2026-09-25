/* Tasks page: list with search/filters/sort, add/edit form, complete, archive/restore, delete. */
(function (window, $) {
  "use strict";
  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  var TITLE_MAX = 200;
  var DESCRIPTION_MAX = 2000;
  var STATUSES = ["Todo", "InProgress", "Done"];
  var PRIORITIES = ["Low", "Medium", "High"];
  var STATUS_BADGE = { Todo: "blue", InProgress: "amber", Done: "green" };
  var PRIORITY_BADGE = { Low: "", High: "red", Medium: "purple" };

  var DEFAULT_FILTERS = { search: "", status: "", priority: "", dueDate: "", overdue: false, view: "Exclude", sortBy: "CreatedAt", sortDir: "Desc" };
  var filters = $.extend({}, DEFAULT_FILTERS);

  var $root, $list, $count, requestSeq = 0, searchTimer = null;

  function hasActiveFilters() {
    return !!(filters.search || filters.status || filters.priority || filters.dueDate || filters.overdue);
  }

  function toQuery() {
    return {
      Search: filters.search.trim(),
      Status: filters.status,
      Priority: filters.priority,
      DueDate: filters.dueDate,
      Overdue: filters.overdue ? true : null,
      Archived: filters.view,
      SortBy: filters.sortBy,
      SortDir: filters.sortDir
    };
  }

  function options(values, selected, allLabel) {
    var html = allLabel ? '<option value="">' + UI.esc(allLabel) + "</option>" : "";
    return html + values.map(function (v) {
      return '<option value="' + v + '"' + (v === selected ? " selected" : "") + ">" + UI.esc(Fmt.enumLabel(v)) + "</option>";
    }).join("");
  }

  function renderToolbar() {
    var $tb = $(
      '<form class="toolbar" role="search" aria-label="Task filters">' +
        '<div class="field grow"><label for="task-search">Search</label>' +
          '<input type="search" id="task-search" name="search" placeholder="Search by title…" autocomplete="off"></div>' +
        '<div class="field"><label for="task-status">Status</label><select id="task-status" name="status">' + options(STATUSES, filters.status, "All statuses") + "</select></div>" +
        '<div class="field"><label for="task-priority">Priority</label><select id="task-priority" name="priority">' + options(PRIORITIES, filters.priority, "All priorities") + "</select></div>" +
        '<div class="field"><label for="task-due">Due date</label><input type="date" id="task-due" name="dueDate"></div>' +
        '<div class="field"><label for="task-view">Show</label><select id="task-view" name="view">' +
          '<option value="Exclude">Active</option><option value="Only">Archived</option><option value="Include">All</option></select></div>' +
        '<div class="field"><label for="task-sort">Sort by</label><select id="task-sort" name="sortBy">' +
          '<option value="CreatedAt">Created date</option><option value="DueDate">Due date</option></select></div>' +
        '<div class="field"><label for="task-dir">Order</label><select id="task-dir" name="sortDir">' +
          '<option value="Desc">Descending</option><option value="Asc">Ascending</option></select></div>' +
        '<div class="field"><span class="field-label">&nbsp;</span><label class="check"><input type="checkbox" id="task-overdue" name="overdue"> Overdue only</label></div>' +
      "</form>");
    $tb.find("#task-search").val(filters.search);
    $tb.find("#task-due").val(filters.dueDate);
    $tb.find("#task-view").val(filters.view);
    $tb.find("#task-sort").val(filters.sortBy);
    $tb.find("#task-dir").val(filters.sortDir);
    $tb.find("#task-overdue").prop("checked", filters.overdue);

    $tb.on("submit", function (e) { e.preventDefault(); });
    $tb.on("input", "#task-search", function () {
      filters.search = this.value;
      clearTimeout(searchTimer);
      searchTimer = setTimeout(load, 300);
    });
    $tb.on("change", "select, input[type=date], input[type=checkbox]", function () {
      filters[this.name] = this.type === "checkbox" ? this.checked : this.value;
      load();
    });
    return $tb;
  }

  function taskRow(t) {
    var done = t.status === "Done";
    var badges = UI.badge(Fmt.enumLabel(t.status), STATUS_BADGE[t.status]) +
      UI.badge(t.priority + " priority", PRIORITY_BADGE[t.priority]) +
      (t.isOverdue ? UI.badge("Overdue", "red") : "") +
      (t.isArchived ? UI.badge("Archived") : "");
    var due = t.dueDate ? '<span>Due ' + UI.esc(Fmt.relativeDate(t.dueDate)) + "</span>" : '<span>No due date</span>';
    var title = UI.esc(t.title);
    var $row = $(
      '<article class="task-row" data-id="' + t.id + '" aria-label="Task ' + title + '">' +
        '<button type="button" class="complete-toggle' + (done ? " done" : "") + '" data-action="complete" ' +
          (done ? 'disabled aria-label="' + title + ' is done"' : 'aria-label="Complete ' + title + '"') + ">✓</button>" +
        '<div class="task-main">' +
          '<div class="task-title' + (done ? " done" : "") + '">' + title + "</div>" +
          (t.description ? '<div class="task-desc">' + UI.esc(t.description) + "</div>" : "") +
          '<div class="task-meta">' + badges + due + "</div>" +
        "</div>" +
        '<div class="task-actions">' +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="edit" aria-label="Edit ' + title + '">Edit</button>' +
          (t.isArchived
            ? '<button type="button" class="btn btn-sm btn-ghost" data-action="restore" aria-label="Restore ' + title + '">Restore</button>'
            : '<button type="button" class="btn btn-sm btn-ghost" data-action="archive" aria-label="Archive ' + title + '">Archive</button>') +
          '<button type="button" class="btn btn-sm btn-ghost" data-action="delete" aria-label="Delete ' + title + '">Delete</button>' +
        "</div>" +
      "</article>");
    if (t.isOverdue) $row.addClass("is-overdue");
    if (t.isArchived) $row.addClass("is-archived");
    $row.data("task", t);
    return $row;
  }

  function renderList(tasks) {
    $list.empty();
    if (!tasks.length) {
      $count.text("");
      if (hasActiveFilters()) {
        $list.append(UI.emptyState({
          icon: "🔍", title: "No tasks match your filters", text: "Try a different search or clear the filters.",
          actionLabel: "Clear filters", onAction: clearFilters,
          secondaryLabel: "Add Task", onSecondary: function () { openForm(); }
        }));
      } else if (filters.view === "Only") {
        $list.append(UI.emptyState({ icon: "🗄️", title: "No archived tasks", text: "Archived tasks will appear here." }));
      } else {
        $list.append(UI.emptyState({
          icon: "📝", title: "No tasks yet", text: "Add your first task to start organizing your work.",
          actionLabel: "Add Task", onAction: function () { openForm(); }
        }));
      }
      return;
    }
    $count.text(Fmt.plural(tasks.length, "task"));
    var $ul = $('<div class="task-list" role="list"></div>');
    tasks.forEach(function (t) { $ul.append(taskRow(t).attr("role", "listitem")); });
    $list.append($ul);
  }

  function load() {
    var seq = ++requestSeq;
    if (!$list.children().length) $list.append(UI.loading("Loading tasks…"));
    return Api.tasks.list(toQuery()).then(function (tasks) {
      if (seq !== requestSeq) return;
      renderList(tasks || []);
    }, function (err) {
      if (seq !== requestSeq) return;
      $count.text("");
      $list.empty().append(UI.errorBox(err, load));
    });
  }

  function clearFilters() {
    filters = $.extend({}, DEFAULT_FILTERS, { view: filters.view, sortBy: filters.sortBy, sortDir: filters.sortDir });
    $root.find(".toolbar").replaceWith(renderToolbar());
    load();
  }

  /** Client-side rules mirroring BR-1..3; the server stays authoritative. */
  function validate(data) {
    var errors = {};
    if (!data.title) errors.title = ["Title is required."];
    else if (data.title.length > TITLE_MAX) errors.title = ["Title must be at most " + TITLE_MAX + " characters."];
    if (data.description && data.description.length > DESCRIPTION_MAX) errors.description = ["Description must be at most " + DESCRIPTION_MAX + " characters."];
    if (STATUSES.indexOf(data.status) < 0) errors.status = ["Choose a valid status."];
    if (PRIORITIES.indexOf(data.priority) < 0) errors.priority = ["Choose a valid priority."];
    return errors;
  }

  function openForm(task) {
    var editing = !!task;
    var $body = $(
      '<div class="form-grid">' +
        '<div class="field span-2"><label for="tf-title">Title</label>' +
          '<input type="text" id="tf-title" name="title" maxlength="' + TITLE_MAX + '" required>' +
          '<span class="hint">Required, up to ' + TITLE_MAX + ' characters.</span></div>' +
        '<div class="field span-2"><label for="tf-description">Description</label>' +
          '<textarea id="tf-description" name="description" maxlength="' + DESCRIPTION_MAX + '" rows="3"></textarea></div>' +
        '<div class="field"><label for="tf-status">Status</label><select id="tf-status" name="status">' + options(STATUSES, task ? task.status : "Todo") + "</select></div>" +
        '<div class="field"><label for="tf-priority">Priority</label><select id="tf-priority" name="priority">' + options(PRIORITIES, task ? task.priority : "Medium") + "</select></div>" +
        '<div class="field"><label for="tf-due">Due date</label><input type="date" id="tf-due" name="dueDate"></div>' +
      "</div>");
    if (task) {
      $body.find("#tf-title").val(task.title);
      $body.find("#tf-description").val(task.description || "");
      $body.find("#tf-due").val(task.dueDate || "");
    }
    UI.modal({
      title: editing ? "Edit task" : "Add task",
      body: $body,
      submitLabel: editing ? "Save changes" : "Add task",
      onSubmit: function ($form) {
        var data = {
          title: $.trim($form.find("#tf-title").val()),
          description: $.trim($form.find("#tf-description").val()) || null,
          status: $form.find("#tf-status").val(),
          priority: $form.find("#tf-priority").val(),
          dueDate: $form.find("#tf-due").val() || null
        };
        var errors = validate(data);
        if (Object.keys(errors).length) { UI.showErrors($form, errors); return false; }
        UI.clearErrors($form);
        var call = editing ? Api.tasks.update(task.id, data) : Api.tasks.create(data);
        return call.then(function (saved) {
          UI.toast(editing ? "Task updated." : "Task \"" + saved.title + "\" added.", { type: "success" });
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
    var task = $btn.closest(".task-row").data("task");
    if (!task) return;
    var action = $btn.data("action");
    if (action === "edit") return openForm(task);
    if (action === "delete") {
      return UI.confirm("Delete \"" + task.title + "\"? This cannot be undone.", { title: "Delete task", confirmLabel: "Delete" })
        .then(function (ok) {
          if (!ok) return;
          return Api.tasks.remove(task.id).then(function () {
            UI.toast("Task deleted.", { type: "success" });
            load();
          }, function (err) { UI.toastError(err, "Could not delete task"); });
        });
    }
    var calls = { complete: Api.tasks.complete, archive: Api.tasks.archive, restore: Api.tasks.restore };
    var messages = { complete: "Task completed.", archive: "Task archived.", restore: "Task restored." };
    if (!calls[action]) return;
    $btn.prop("disabled", true);
    calls[action](task.id).then(function () {
      UI.toast(messages[action], { type: "success" });
      load();
    }, function (err) {
      $btn.prop("disabled", false);
      UI.toastError(err, "Could not " + action + " task");
    });
  }

  App.pages.tasks = {
    title: "Tasks",
    render: function ($outlet, ctx) {
      $root = $outlet;
      var $add = $('<button type="button" class="btn btn-primary">+ Add Task</button>').on("click", function () { openForm(); });
      $root.append(UI.pageHeader("Tasks", "Create, organize and complete your tasks.", $add));
      $root.append(renderToolbar());
      $count = $('<div class="result-count" aria-live="polite"></div>').appendTo($root);
      $list = $('<div id="task-results"></div>').appendTo($root);
      $list.on("click", "[data-action]", onAction);
      load();
      if (ctx.query["new"]) {
        Router.clearQuery();
        openForm();
      }
    },
    destroy: function () { clearTimeout(searchTimer); requestSeq++; }
  };
})(window, jQuery);
