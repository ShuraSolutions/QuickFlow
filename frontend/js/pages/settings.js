/* Settings page: profile (display name, email) and preferences (notifications, lead minutes, default view, theme). */
(function (window, $) {
  "use strict";
  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  var NAME_MAX = 100;
  var EMAIL_MAX = 200;
  var LEAD_MAX = 1440;
  var EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
  var VIEWS = [["Dashboard", "Dashboard"], ["Tasks", "Tasks"], ["Habits", "Habits"], ["LearningResources", "Learning Resources"], ["TodoPlans", "Todo Plans"], ["Settings", "Settings"]];
  var THEMES = [["Light", "Light"], ["Dark", "Dark"], ["System", "Match system"]];

  var $form, requestSeq = 0;

  function options(list) {
    return list.map(function (o) { return '<option value="' + o[0] + '">' + UI.esc(o[1]) + "</option>"; }).join("");
  }

  function fill(s) {
    $form.find("#sf-name").val(s.displayName);
    $form.find("#sf-email").val(s.email || "");
    $form.find("#sf-notify").prop("checked", s.planStartNotificationsEnabled);
    $form.find("#sf-lead").val(s.notificationLeadMinutes);
    $form.find("#sf-view").val(s.defaultView);
    $form.find("#sf-theme").val(s.theme);
    $form.find(".updated-at").text("Last saved " + Fmt.dateTime(s.updatedAt));
  }

  /** Client-side rules mirroring the backend; the server stays authoritative. */
  function validate(d) {
    var e = {};
    if (!d.displayName) e.displayName = ["Display name is required."];
    else if (d.displayName.length > NAME_MAX) e.displayName = ["Display name must be at most " + NAME_MAX + " characters."];
    if (d.email && d.email.length > EMAIL_MAX) e.email = ["Email must be at most " + EMAIL_MAX + " characters."];
    else if (d.email && !EMAIL_PATTERN.test(d.email)) e.email = ["Email must be a valid email address."];
    if (!(d.notificationLeadMinutes >= 0 && d.notificationLeadMinutes <= LEAD_MAX) || Math.floor(d.notificationLeadMinutes) !== d.notificationLeadMinutes) {
      e.notificationLeadMinutes = ["Lead time must be a whole number between 0 and " + LEAD_MAX + " minutes."];
    }
    return e;
  }

  function onSubmit(e) {
    e.preventDefault();
    var lead = $form.find("#sf-lead").val();
    var data = {
      displayName: $.trim($form.find("#sf-name").val()),
      email: $.trim($form.find("#sf-email").val()),
      planStartNotificationsEnabled: $form.find("#sf-notify").prop("checked"),
      notificationLeadMinutes: lead === "" ? NaN : Number(lead),
      defaultView: $form.find("#sf-view").val(),
      theme: $form.find("#sf-theme").val()
    };
    var errors = validate(data);
    if (Object.keys(errors).length) { UI.showErrors($form, errors); return; }
    UI.clearErrors($form);
    var $btn = $form.find('[type="submit"]').prop("disabled", true);
    Api.settings.update(data).then(function (saved) {
      $btn.prop("disabled", false);
      Prefs.apply(saved);
      fill(saved);
      UI.toast("Settings saved.", { type: "success" });
    }, function (err) {
      $btn.prop("disabled", false);
      UI.showServerError($form, err);
    });
  }

  App.pages.settings = {
    title: "Settings",
    render: function ($outlet) {
      var seq = ++requestSeq;
      $outlet.append(UI.pageHeader("Settings", "Profile and application preferences."));
      $form = $(
        '<form class="card settings-form" novalidate aria-label="Settings">' +
          '<section><h2>Profile</h2><div class="form-grid">' +
            '<div class="field"><label for="sf-name">Display name</label><input type="text" id="sf-name" name="displayName" maxlength="' + NAME_MAX + '" autocomplete="name">' +
              '<span class="hint">Used in the dashboard greeting.</span></div>' +
            '<div class="field"><label for="sf-email">Email</label><input type="email" id="sf-email" name="email" maxlength="' + EMAIL_MAX + '" autocomplete="email" placeholder="you@example.com"></div>' +
          "</div></section>" +
          '<section><h2>Preferences</h2><div class="form-grid">' +
            '<div class="field span-2"><label class="check"><input type="checkbox" id="sf-notify" name="planStartNotificationsEnabled"> Notify me when a plan\'s start time is reached</label></div>' +
            '<div class="field"><label for="sf-lead">"Starting soon" notice (minutes before start)</label><input type="number" id="sf-lead" name="notificationLeadMinutes" min="0" max="' + LEAD_MAX + '" step="1">' +
              '<span class="hint">0 turns the notice off. Up to ' + LEAD_MAX + ' minutes.</span></div>' +
            '<div class="field"><label for="sf-view">Default view</label><select id="sf-view" name="defaultView">' + options(VIEWS) + "</select>" +
              '<span class="hint">Page shown when you open QuickFlow.</span></div>' +
            '<div class="field"><label for="sf-theme">Theme</label><select id="sf-theme" name="theme">' + options(THEMES) + "</select></div>" +
          "</div></section>" +
          '<div class="row form-actions"><button type="submit" class="btn btn-primary">Save settings</button><span class="muted small updated-at"></span></div>' +
        "</form>");
      $form.on("submit", onSubmit);
      var $loading = UI.loading("Loading settings…");
      $outlet.append($loading);
      Api.settings.get().then(function (s) {
        if (seq !== requestSeq) return;
        $loading.replaceWith($form);
        fill(s);
      }, function (err) {
        if (seq === requestSeq) $loading.replaceWith(UI.errorBox(err, function () { Router.refresh(); }));
      });
    },
    destroy: function () { requestSeq++; }
  };
})(window, jQuery);
