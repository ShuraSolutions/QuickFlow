/*
 * QuickFlow API client. One method per operation of docs/api/swagger.json.
 * Every HTTP call of the app goes through request(), which returns a native Promise
 * resolving to the parsed JSON body (or null for 204) and rejecting with an ApiError.
 */
(function (window, $) {
  "use strict";

  var baseUrl = (window.APP_CONFIG && window.APP_CONFIG.apiBaseUrl || "").replace(/\/+$/, "");

  /** Error carrying the RFC 7807 ProblemDetails fields of a failed response. */
  function ApiError(status, problem, fallback) {
    problem = problem || {};
    this.name = "ApiError";
    this.status = status;
    this.title = problem.title || null;
    this.detail = problem.detail || null;
    this.errors = problem.errors || {};
    this.message = problem.detail || problem.title || fallback || ("Request failed (" + status + ")");
  }
  ApiError.prototype = Object.create(Error.prototype);
  ApiError.prototype.constructor = ApiError;

  function queryString(query) {
    if (!query) return "";
    var parts = [];
    Object.keys(query).forEach(function (key) {
      var value = query[key];
      if (value === undefined || value === null || value === "") return;
      parts.push(encodeURIComponent(key) + "=" + encodeURIComponent(String(value)));
    });
    return parts.length ? "?" + parts.join("&") : "";
  }

  function parse(text) {
    if (!text) return null;
    try { return JSON.parse(text); } catch (e) { return null; }
  }

  function request(method, path, options) {
    options = options || {};
    var hasBody = options.body !== undefined;
    return new Promise(function (resolve, reject) {
      $.ajax({
        url: baseUrl + path + queryString(options.query),
        method: method,
        dataType: "text",
        contentType: hasBody ? "application/json" : undefined,
        data: hasBody ? JSON.stringify(options.body) : undefined,
        headers: { Accept: "application/json" },
        timeout: 15000
      }).done(function (text) {
        resolve(parse(text));
      }).fail(function (xhr, textStatus) {
        if (xhr.status === 0) {
          reject(new ApiError(0, null, "Cannot reach the API at " + baseUrl + " (" + (textStatus || "network error") + ")."));
          return;
        }
        reject(new ApiError(xhr.status, parse(xhr.responseText), xhr.statusText));
      });
    });
  }

  function id(value) { return encodeURIComponent(String(value)); }

  var Api = {
    baseUrl: baseUrl,
    ApiError: ApiError,
    request: request,

    /** GET /health */
    health: function () { return request("GET", "/health"); },

    /** GET /api/dashboard */
    dashboard: {
      get: function () { return request("GET", "/api/dashboard"); }
    },

    tasks: {
      /** GET /api/tasks — query: Search, Status, Priority, DueDate, DueFrom, DueTo, Overdue, Archived, SortBy, SortDir */
      list: function (query) { return request("GET", "/api/tasks", { query: query }); },
      get: function (taskId) { return request("GET", "/api/tasks/" + id(taskId)); },
      /** body: CreateTaskRequest { title, description, status, priority, dueDate } */
      create: function (body) { return request("POST", "/api/tasks", { body: body }); },
      /** body: UpdateTaskRequest { title, description, status, priority, dueDate } */
      update: function (taskId, body) { return request("PUT", "/api/tasks/" + id(taskId), { body: body }); },
      remove: function (taskId) { return request("DELETE", "/api/tasks/" + id(taskId)); },
      complete: function (taskId) { return request("POST", "/api/tasks/" + id(taskId) + "/complete"); },
      archive: function (taskId) { return request("POST", "/api/tasks/" + id(taskId) + "/archive"); },
      restore: function (taskId) { return request("POST", "/api/tasks/" + id(taskId) + "/restore"); }
    },

    habits: {
      /** GET /api/habits — query: isActive */
      list: function (query) { return request("GET", "/api/habits", { query: query }); },
      get: function (habitId) { return request("GET", "/api/habits/" + id(habitId)); },
      /** body: HabitRequest { name, description, frequency } */
      create: function (body) { return request("POST", "/api/habits", { body: body }); },
      update: function (habitId, body) { return request("PUT", "/api/habits/" + id(habitId), { body: body }); },
      remove: function (habitId) { return request("DELETE", "/api/habits/" + id(habitId)); },
      deactivate: function (habitId) { return request("POST", "/api/habits/" + id(habitId) + "/deactivate"); },
      activate: function (habitId) { return request("POST", "/api/habits/" + id(habitId) + "/activate"); },
      completions: function (habitId) { return request("GET", "/api/habits/" + id(habitId) + "/completions"); },
      /** POST /api/habits/{id}/completions — body: HabitCompletionRequest { date } (yyyy-MM-dd, default today) */
      complete: function (habitId, date) {
        return request("POST", "/api/habits/" + id(habitId) + "/completions", { body: { date: date || null } });
      },
      /** DELETE /api/habits/{id}/completions/{date} */
      uncomplete: function (habitId, date) {
        return request("DELETE", "/api/habits/" + id(habitId) + "/completions/" + id(date));
      }
    },

    learning: {
      /** GET /api/learning-cards — query: status */
      list: function (query) { return request("GET", "/api/learning-cards", { query: query }); },
      get: function (cardId) { return request("GET", "/api/learning-cards/" + id(cardId)); },
      /** body: LearningCardRequest { title, description, status } */
      create: function (body) { return request("POST", "/api/learning-cards", { body: body }); },
      update: function (cardId, body) { return request("PUT", "/api/learning-cards/" + id(cardId), { body: body }); },
      remove: function (cardId) { return request("DELETE", "/api/learning-cards/" + id(cardId)); },
      /** body: CreateMilestoneRequest { title, targetDate } */
      addMilestone: function (cardId, body) {
        return request("POST", "/api/learning-cards/" + id(cardId) + "/milestones", { body: body });
      },
      /** body: UpdateMilestoneRequest { title, isDone, targetDate, clearTargetDate } */
      updateMilestone: function (cardId, milestoneId, body) {
        return request("PATCH", "/api/learning-cards/" + id(cardId) + "/milestones/" + id(milestoneId), { body: body });
      },
      removeMilestone: function (cardId, milestoneId) {
        return request("DELETE", "/api/learning-cards/" + id(cardId) + "/milestones/" + id(milestoneId));
      },
      /** body: CreateNoteRequest { text } */
      addNote: function (cardId, body) {
        return request("POST", "/api/learning-cards/" + id(cardId) + "/notes", { body: body });
      },
      removeNote: function (cardId, noteId) {
        return request("DELETE", "/api/learning-cards/" + id(cardId) + "/notes/" + id(noteId));
      }
    },

    plans: {
      /** GET /api/plans — query: status */
      list: function (query) { return request("GET", "/api/plans", { query: query }); },
      history: function () { return request("GET", "/api/plans/history"); },
      notifications: function () { return request("GET", "/api/plans/notifications"); },
      ack: function (planId) { return request("POST", "/api/plans/" + id(planId) + "/notifications/ack"); },
      get: function (planId) { return request("GET", "/api/plans/" + id(planId)); },
      /** body: PlanRequest { title, estimatedDurationMinutes, startDateTime, endDateTime, priorityOrder, items[{sourceType, sourceId}] } */
      create: function (body) { return request("POST", "/api/plans", { body: body }); },
      update: function (planId, body) { return request("PUT", "/api/plans/" + id(planId), { body: body }); },
      remove: function (planId) { return request("DELETE", "/api/plans/" + id(planId)); },
      /** PATCH /api/plans/{id}/items/{itemId} — body: UpdatePlanItemRequest { isDone } */
      setItemDone: function (planId, itemId, isDone) {
        return request("PATCH", "/api/plans/" + id(planId) + "/items/" + id(itemId), { body: { isDone: !!isDone } });
      }
    },

    settings: {
      get: function () { return request("GET", "/api/settings"); },
      /** body: UpdateSettingsRequest { displayName, email, planStartNotificationsEnabled, notificationLeadMinutes, defaultView, theme } */
      update: function (body) { return request("PUT", "/api/settings", { body: body }); }
    }
  };

  window.Api = Api;
})(window, jQuery);
