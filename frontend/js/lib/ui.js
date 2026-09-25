/*
 * Shared presentation helpers: escaping, loading/error/empty states, badges, progress bars,
 * toasts, modals, confirmation dialogs and ProblemDetails → form error mapping.
 */
(function (window, $) {
  "use strict";

  var ESC = { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" };

  function esc(value) {
    return value === null || value === undefined ? "" : String(value).replace(/[&<>"']/g, function (c) { return ESC[c]; });
  }

  var modalStack = [];

  $(document).on("keydown.ui", function (e) {
    if (e.key === "Escape" && modalStack.length) modalStack[modalStack.length - 1].close();
  });

  var UI = {
    esc: esc,

    /** Page header with title, optional subtitle and action buttons. */
    pageHeader: function (title, subtitle, $actions) {
      var $h = $('<header class="page-header"><div><h1></h1></div><div class="page-actions"></div></header>');
      $h.find("h1").text(title);
      if (subtitle) $h.children("div").first().append($('<p class="subtitle"></p>').text(subtitle));
      if ($actions) $h.find(".page-actions").append($actions);
      return $h;
    },

    loading: function (text) {
      return $('<div class="loading" role="status"><span class="spinner"></span><span></span></div>')
        .find("span:last").text(text || "Loading…").end();
    },

    /** Error panel for a failed load, with an optional retry. */
    errorBox: function (err, onRetry) {
      var $box = $('<div class="error-box" role="alert"><span></span></div>');
      $box.find("span").text((err && err.message) || "Something went wrong.");
      if (onRetry) $('<button type="button" class="btn btn-sm">Retry</button>').on("click", onRetry).appendTo($box);
      return $box;
    },

    /** Empty state guiding the user to the "add" action. */
    emptyState: function (opts) {
      var $e = $('<div class="empty-state"><div class="empty-icon" aria-hidden="true"></div><h2></h2><p></p></div>');
      $e.find(".empty-icon").text(opts.icon || "✨");
      $e.find("h2").text(opts.title);
      $e.find("p").text(opts.text || "");
      if (opts.actionLabel) {
        $('<button type="button" class="btn btn-primary"></button>').text(opts.actionLabel)
          .on("click", opts.onAction).appendTo($e);
      }
      if (opts.secondaryLabel) {
        $('<button type="button" class="btn" style="margin-left:8px"></button>').text(opts.secondaryLabel)
          .on("click", opts.onSecondary).appendTo($e);
      }
      return $e;
    },

    badge: function (text, variant) {
      return '<span class="badge' + (variant ? " badge-" + variant : "") + '">' + esc(text) + "</span>";
    },

    /** Progress bar markup; pct 0-100. */
    progress: function (pct, cls, label) {
      var v = Math.max(0, Math.min(100, pct || 0));
      return '<div class="progress ' + (cls || "") + '" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="' +
        Math.round(v) + '"' + (label ? ' aria-label="' + esc(label) + '"' : "") + '><span style="width:' + v + '%"></span></div>';
    },

    /**
     * Toast notification. opts: { type: info|success|error|warning, title, timeout (ms, 0 = sticky), actions: [{label, onClick}] }
     * Returns { $el, close }.
     */
    toast: function (message, opts) {
      opts = opts || {};
      var $wrap = $("#toasts");
      var type = opts.type || "info";
      var $t = $('<div class="toast" role="' + (type === "error" ? "alert" : "status") + '"><div class="toast-body"></div>' +
        '<button type="button" class="icon-btn" aria-label="Dismiss notification">✕</button></div>').addClass(type);
      if (opts.id) $t.attr("data-toast-id", opts.id);
      var $body = $t.find(".toast-body");
      if (opts.title) $('<div class="toast-title"></div>').text(opts.title).appendTo($body);
      $("<div></div>").text(message).appendTo($body);
      var closed = false;
      function close() {
        if (closed) return;
        closed = true;
        $t.remove();
        if (opts.onClose) opts.onClose();
      }
      if (opts.actions && opts.actions.length) {
        var $actions = $('<div class="toast-actions"></div>').appendTo($body);
        opts.actions.forEach(function (a) {
          $('<button type="button" class="btn btn-sm"></button>').text(a.label).on("click", function () {
            if (a.onClick) a.onClick();
            close();
          }).appendTo($actions);
        });
      }
      $t.find(".icon-btn").on("click", close);
      $wrap.append($t);
      var timeout = opts.timeout === undefined ? 4000 : opts.timeout;
      if (timeout) setTimeout(close, timeout);
      return { $el: $t, close: close };
    },

    /** Shows a failed operation as an error toast. */
    toastError: function (err, prefix) {
      return UI.toast((prefix ? prefix + ": " : "") + ((err && err.message) || "Request failed."), { type: "error", timeout: 6000 });
    },

    /**
     * Modal dialog. opts: { title, body ($el or html), submitLabel, cancelLabel, danger, wide, onSubmit($modal) → Promise|false }
     * The body is wrapped in a <form>; submit runs onSubmit and closes on resolve.
     */
    modal: function (opts) {
      var titleId = "modal-title-" + Date.now();
      var $backdrop = $('<div class="modal-backdrop"></div>');
      var $modal = $('<form class="modal" role="dialog" aria-modal="true" novalidate>' +
        '<div class="modal-header"><h2></h2><button type="button" class="icon-btn" data-close aria-label="Close">✕</button></div>' +
        '<div class="modal-body"></div>' +
        '<div class="modal-footer"><button type="button" class="btn" data-close></button>' +
        '<button type="submit" class="btn btn-primary"></button></div></form>');
      if (opts.wide) $modal.addClass("wide");
      $modal.attr("aria-labelledby", titleId).find("h2").attr("id", titleId).text(opts.title);
      $modal.find(".modal-body").append(opts.body);
      $modal.find(".modal-footer [data-close]").text(opts.cancelLabel || "Cancel");
      var $submit = $modal.find('[type="submit"]').text(opts.submitLabel || "Save");
      if (opts.danger) $submit.removeClass("btn-primary").addClass("btn-danger");
      $backdrop.append($modal).appendTo("body");

      var previousFocus = document.activeElement;
      var api = {
        $el: $modal,
        close: function () {
          $backdrop.remove();
          modalStack = modalStack.filter(function (m) { return m !== api; });
          if (opts.onClose) opts.onClose();
          if (previousFocus && document.body.contains(previousFocus)) previousFocus.focus();
        }
      };
      modalStack.push(api);

      $modal.on("click", "[data-close]", api.close);
      $backdrop.on("mousedown", function (e) { if (e.target === $backdrop[0]) api.close(); });
      $modal.on("submit", function (e) {
        e.preventDefault();
        if (!opts.onSubmit) { api.close(); return; }
        var result = opts.onSubmit($modal);
        if (result === false) return;
        $submit.prop("disabled", true);
        Promise.resolve(result).then(function (ok) {
          if (ok === false) { $submit.prop("disabled", false); return; }
          api.close();
        }, function () {
          $submit.prop("disabled", false);
        });
      });

      setTimeout(function () {
        var $first = $modal.find(".modal-body").find("input, select, textarea").filter(":visible").first();
        ($first.length ? $first : $submit).trigger("focus");
      }, 0);
      return api;
    },

    /** Confirmation dialog → Promise<boolean>. */
    confirm: function (message, opts) {
      opts = opts || {};
      return new Promise(function (resolve) {
        var answered = false;
        UI.modal({
          title: opts.title || "Please confirm",
          body: $("<p></p>").text(message),
          submitLabel: opts.confirmLabel || "Confirm",
          danger: opts.danger !== false,
          onSubmit: function () { answered = true; resolve(true); },
          onClose: function () { if (!answered) resolve(false); }
        });
      });
    },

    /** Removes inline field errors and the form-level error of a form. */
    clearErrors: function ($form) {
      $form.find(".field-error").remove();
      $form.find(".form-error").remove();
      $form.find('[aria-invalid="true"]').removeAttr("aria-invalid");
    },

    /**
     * Shows validation errors. errors: { fieldName: ["msg", …] } (client or ProblemDetails `errors`).
     * Keys are matched case-insensitively to elements with a matching [name]; the rest go to a form-level box.
     */
    showErrors: function ($form, errors, generalMessage) {
      UI.clearErrors($form);
      var unmatched = [];
      Object.keys(errors || {}).forEach(function (key) {
        var messages = [].concat(errors[key]);
        var $input = $form.find("[name]").filter(function () {
          return this.name.toLowerCase() === key.toLowerCase().replace(/^\$\./, "");
        }).first();
        if ($input.length) {
          var errId = ($input.attr("id") || $input.attr("name")) + "-error";
          $input.attr({ "aria-invalid": "true", "aria-describedby": errId });
          $('<div class="field-error"></div>').attr("id", errId).text(messages.join(" "))
            .appendTo($input.closest(".field").length ? $input.closest(".field") : $input.parent());
        } else {
          unmatched = unmatched.concat(messages);
        }
      });
      if (generalMessage) unmatched.unshift(generalMessage);
      if (unmatched.length) {
        $('<div class="form-error" role="alert"></div>').text(unmatched.join(" "))
          .prependTo($form.find(".modal-body").length ? $form.find(".modal-body") : $form);
      }
      $form.find('[aria-invalid="true"]').first().trigger("focus");
    },

    /** Maps a server ApiError onto the form (400 field errors) or a form-level message. */
    showServerError: function ($form, err) {
      var hasFields = err && err.errors && Object.keys(err.errors).length;
      UI.showErrors($form, hasFields ? err.errors : {}, hasFields ? null : (err && err.message) || "Request failed.");
    }
  };

  window.UI = UI;
})(window, jQuery);
