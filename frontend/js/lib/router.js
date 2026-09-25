/*
 * Minimal hash router: "#/<route>?<query>". Each page module registers
 * { title, render($outlet, ctx), destroy() } and is rendered into the outlet on navigation.
 */
(function (window, $) {
  "use strict";

  function parseQuery(qs) {
    var query = {};
    (qs || "").split("&").forEach(function (pair) {
      if (!pair) return;
      var i = pair.indexOf("=");
      var key = decodeURIComponent(i < 0 ? pair : pair.slice(0, i));
      query[key] = i < 0 ? "" : decodeURIComponent(pair.slice(i + 1).replace(/\+/g, " "));
    });
    return query;
  }

  var Router = {
    routes: {},
    defaultRoute: "dashboard",
    current: null,
    currentPage: null,
    $outlet: null,

    register: function (name, page) { Router.routes[name] = page; },

    /** "#/tasks?new=1" → { name: "tasks", query: { new: "1" } } */
    parse: function (hash) {
      var raw = (hash || "").replace(/^#\/?/, "");
      var q = raw.indexOf("?");
      return { name: (q < 0 ? raw : raw.slice(0, q)).replace(/\/+$/, ""), query: parseQuery(q < 0 ? "" : raw.slice(q + 1)) };
    },

    href: function (name, query) {
      var qs = $.param(query || {});
      return "#/" + name + (qs ? "?" + qs : "");
    },

    go: function (name, query) { window.location.hash = Router.href(name, query); },

    /** Drops the query from the address bar without re-rendering (e.g. after handling ?new=1). */
    clearQuery: function () {
      if (Router.current && window.history.replaceState) window.history.replaceState(null, "", "#/" + Router.current.name);
    },

    /** Re-renders the current page. */
    refresh: function () { Router.render(); },

    render: function () {
      var route = Router.parse(window.location.hash);
      if (!route.name) {
        window.location.replace(Router.href(Router.defaultRoute));
        return;
      }
      var page = Router.routes[route.name];
      if (!page) {
        window.location.replace(Router.href("dashboard"));
        return;
      }
      if (Router.currentPage && Router.currentPage.destroy) {
        try { Router.currentPage.destroy(); } catch (e) { /* page cleanup must not block navigation */ }
      }
      Router.current = route;
      Router.currentPage = page;
      document.title = page.title + " · QuickFlow";
      $(".nav a").each(function () {
        var active = $(this).data("route") === route.name;
        if (active) $(this).attr("aria-current", "page"); else $(this).removeAttr("aria-current");
      });
      Router.$outlet.empty().attr("data-page", route.name);
      page.render(Router.$outlet, { route: route.name, query: route.query });
      Router.$outlet.closest("main").scrollTop(0);
      $(document).trigger("route:changed", [route]);
    },

    start: function ($outlet) {
      Router.$outlet = $outlet;
      $(window).on("hashchange", Router.render);
      Router.render();
    }
  };

  window.Router = Router;
})(window, jQuery);
