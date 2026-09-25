/*
 * App bootstrap: registers the page modules, starts the router and the API status indicator.
 */
(function (window, $) {
  "use strict";

  var App = window.App = window.App || {};
  App.pages = App.pages || {};

  function checkApi() {
    var $status = $("#api-status");
    Api.health().then(function (h) {
      var ok = h && h.status === "Healthy";
      $status.attr("data-state", ok ? "ok" : "down").find(".label").text(ok ? "API connected" : "API degraded");
    }, function () {
      $status.attr("data-state", "down").find(".label").text("API unreachable");
    });
  }

  $(function () {
    ["dashboard", "tasks", "habits", "learning", "plans", "settings"].forEach(function (name) {
      if (App.pages[name]) Router.register(name, App.pages[name]);
    });
    Notifier.isEnabled = Prefs.notificationsEnabled;
    // Settings decide the theme, the default view and whether plan notifications are shown.
    Prefs.load().then(function () {
      Router.start($("#page"));
      Notifier.start();
    });
    checkApi();
    setInterval(checkApi, 30000);
  });
})(window, jQuery);
