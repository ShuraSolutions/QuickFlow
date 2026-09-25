/*
 * Application preferences from GET /api/settings, applied app-wide: theme (data-theme on <html>),
 * default view (route used when the app opens without a hash) and the plan notification gate.
 */
(function (window, $) {
  "use strict";

  var VIEW_ROUTES = { Dashboard: "dashboard", Tasks: "tasks", Habits: "habits", LearningResources: "learning", TodoPlans: "plans", Settings: "settings" };
  var THEME_KEY = "quickflow.theme";
  var media = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;

  var Prefs = {
    VIEW_ROUTES: VIEW_ROUTES,
    current: null,

    /** Resolves Light/Dark/System to the concrete theme and sets data-theme on <html>. */
    applyTheme: function (theme) {
      var dark = theme === "Dark" || (theme === "System" && media && media.matches);
      document.documentElement.setAttribute("data-theme", dark ? "dark" : "light");
      try { window.localStorage.setItem(THEME_KEY, theme); } catch (e) { /* storage unavailable: theme still applied */ }
    },

    /** Applies a SettingsResponse to the running app. */
    apply: function (settings) {
      Prefs.current = settings;
      Prefs.applyTheme(settings.theme);
      Router.defaultRoute = VIEW_ROUTES[settings.defaultView] || "dashboard";
      $(document).trigger("settings:changed", [settings]);
    },

    /** Loads settings once at startup; the app still starts (with defaults) if this fails. */
    load: function () {
      return Api.settings.get().then(Prefs.apply, function () { return null; });
    },

    notificationsEnabled: function () { return !Prefs.current || Prefs.current.planStartNotificationsEnabled; },
    leadMinutes: function () { return Prefs.current ? Prefs.current.notificationLeadMinutes : 0; }
  };

  // Paint the last known theme immediately to avoid a flash before settings load.
  try {
    var cached = window.localStorage.getItem(THEME_KEY);
    if (cached) Prefs.applyTheme(cached);
  } catch (e) { /* ignore */ }
  if (media && media.addEventListener) {
    media.addEventListener("change", function () { if (Prefs.current && Prefs.current.theme === "System") Prefs.applyTheme("System"); });
  }

  window.Prefs = Prefs;
})(window, jQuery);
