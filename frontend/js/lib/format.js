/*
 * Pure formatting helpers (no DOM). Dates from the API: date-only "yyyy-MM-dd" (local calendar date)
 * and date-time ISO strings in UTC ("…Z").
 */
(function (window) {
  "use strict";

  var MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
  var WEEKDAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

  function pad(n) { return (n < 10 ? "0" : "") + n; }

  /** Local calendar date of a Date as yyyy-MM-dd. */
  function isoDate(d) { return d.getFullYear() + "-" + pad(d.getMonth() + 1) + "-" + pad(d.getDate()); }

  /** Parses "yyyy-MM-dd" as a local date (not UTC midnight). */
  function parseDate(value) {
    if (!value) return null;
    var m = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    return m ? new Date(+m[1], +m[2] - 1, +m[3]) : null;
  }

  var Fmt = {
    pad: pad,
    isoDate: isoDate,
    parseDate: parseDate,

    /** Today's local date as yyyy-MM-dd. */
    todayIso: function () { return isoDate(new Date()); },

    /** Local date shifted by n days, as yyyy-MM-dd. */
    addDaysIso: function (days, from) {
      var d = from ? new Date(from.getTime()) : new Date();
      d.setDate(d.getDate() + days);
      return isoDate(d);
    },

    /** "yyyy-MM-dd" → "Sep 24, 2026" */
    date: function (value) {
      var d = parseDate(value);
      return d ? MONTHS[d.getMonth()] + " " + d.getDate() + ", " + d.getFullYear() : "";
    },

    /** "yyyy-MM-dd" → "Today" / "Tomorrow" / "Yesterday" / "Sep 24, 2026" */
    relativeDate: function (value) {
      if (!value) return "";
      if (value === Fmt.todayIso()) return "Today";
      if (value === Fmt.addDaysIso(1)) return "Tomorrow";
      if (value === Fmt.addDaysIso(-1)) return "Yesterday";
      return Fmt.date(value);
    },

    /** ISO date-time → "Sep 24, 2026, 14:05" in local time. */
    dateTime: function (value) {
      if (!value) return "";
      var d = new Date(value);
      if (isNaN(d)) return "";
      return MONTHS[d.getMonth()] + " " + d.getDate() + ", " + d.getFullYear() + ", " + pad(d.getHours()) + ":" + pad(d.getMinutes());
    },

    /** ISO date-time → "14:05" local time. */
    time: function (value) {
      var d = new Date(value);
      return isNaN(d) ? "" : pad(d.getHours()) + ":" + pad(d.getMinutes());
    },

    /** "Thursday, Sep 24, 2026" for a Date (default now). */
    longDate: function (d) {
      d = d || new Date();
      return WEEKDAYS[d.getDay()] + ", " + MONTHS[d.getMonth()] + " " + d.getDate() + ", " + d.getFullYear();
    },

    /** ISO date-time → value for <input type="datetime-local"> (local time, minutes precision). */
    toLocalInput: function (value) {
      var d = value instanceof Date ? value : new Date(value);
      if (isNaN(d)) return "";
      return isoDate(d) + "T" + pad(d.getHours()) + ":" + pad(d.getMinutes());
    },

    /** <input type="datetime-local"> value (local) → ISO UTC string, or null. */
    fromLocalInput: function (value) {
      if (!value) return null;
      var d = new Date(value);
      return isNaN(d) ? null : d.toISOString();
    },

    /** Minutes → "1 h 30 min" / "45 min" / "2 h". */
    duration: function (minutes) {
      minutes = Math.max(0, Math.round(minutes || 0));
      var h = Math.floor(minutes / 60), m = minutes % 60;
      if (!h) return m + " min";
      return m ? h + " h " + m + " min" : h + " h";
    },

    /** 33.33 → "33%" */
    pct: function (value) { return Math.round(value || 0) + "%"; },

    /** Enum value → human label: "InProgress" → "In progress", "LearningResource" → "Learning resource". */
    enumLabel: function (value) {
      if (!value) return "";
      var words = String(value).replace(/([a-z])([A-Z])/g, "$1 $2").split(" ");
      return words.map(function (w, i) { return i === 0 ? w : w.toLowerCase(); }).join(" ");
    },

    /** Pluralize: plural(2, "task") → "2 tasks". */
    plural: function (n, word, pluralWord) {
      return n + " " + (n === 1 ? word : (pluralWord || word + "s"));
    }
  };

  window.Fmt = Fmt;
})(window);
