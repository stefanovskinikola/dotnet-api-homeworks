"use strict";

(() => {
  const $ = (id) => document.getElementById(id);
  const tokenKey = "loto3000.token";
  const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" });
  const state = {
    token: readStoredToken(), user: null, session: null, selected: new Set(),
    submitting: false, drawing: false, ready: false, expiryTimer: null, authVersion: 0
  };

  class ApiError extends Error {
    constructor(message, status) {
      super(message);
      this.name = "ApiError";
      this.status = status;
    }
  }

  function readStoredToken() {
    try { return localStorage.getItem(tokenKey); } catch { return null; }
  }

  function expiresAt(token) {
    try {
      const payload = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
      const bytes = Uint8Array.from(atob(payload.padEnd(Math.ceil(payload.length / 4) * 4, "=")), (value) => value.charCodeAt(0));
      const expiry = JSON.parse(new TextDecoder().decode(bytes)).exp;
      return typeof expiry === "number" && Number.isFinite(expiry) ? expiry * 1000 : 0;
    } catch { return 0; }
  }

  function showNotice(message, kind = "danger") {
    $("notice").className = `alert alert-${kind} d-flex justify-content-between align-items-start gap-3`;
    $("notice").setAttribute("role", kind === "danger" ? "alert" : "status");
    $("notice-text").textContent = message;
    $("notice").hidden = false;
  }

  function resetIdentity(token = null) {
    clearTimeout(state.expiryTimer);
    state.authVersion++;
    state.token = token;
    state.user = null;
    state.selected.clear();
    $("my-tickets-body").replaceChildren();
    $("ticket-success").hidden = true;
    $("draw-result").hidden = true;
    $("draw-winners-body").replaceChildren();
    renderAuth();
  }

  function logout(message = "You have been logged out.", view = "play") {
    try { localStorage.removeItem(tokenKey); } catch { /* In-memory credentials must still be cleared. */ }
    resetIdentity();
    navigate(view);
    showNotice(message, "info");
  }

  function scheduleExpiry() {
    clearTimeout(state.expiryTimer);
    if (!state.token) return;
    const delay = expiresAt(state.token) - Date.now();
    if (delay <= 0) {
      logout("Your sign-in has expired. Please log in again.", "auth");
      return;
    }
    state.expiryTimer = setTimeout(() => logout("Your sign-in has expired. Please log in again.", "auth"), Math.min(delay + 20, 2147483647));
  }

  async function api(path, { method = "GET", body, authorized = false } = {}) {
    const headers = { Accept: "application/json" };
    const requestToken = state.token;
    if (authorized) {
      if (!requestToken || expiresAt(requestToken) <= Date.now()) {
        logout("Please log in to continue.", "auth");
        throw new ApiError("Please log in to continue.", 401);
      }
      headers.Authorization = `Bearer ${requestToken}`;
    }
    if (body !== undefined) headers["Content-Type"] = "application/json";
    let response;
    let data;
    try {
      response = await fetch(`/api${path}`, {
        method, headers, body: body === undefined ? undefined : JSON.stringify(body),
        credentials: "omit", cache: "no-store", signal: AbortSignal.timeout(30000)
      });
      const text = await response.text();
      data = text && response.headers.get("content-type")?.includes("json") ? JSON.parse(text) : null;
    } catch (error) {
      const timedOut = error.name === "TimeoutError" || error.name === "AbortError";
      throw new Error(timedOut
        ? "The request timed out. It may have completed; refresh before retrying."
        : "Cannot reach the server. Check the connection and refresh before retrying; requests are never automatically resubmitted.");
    }
    if (!response.ok) {
      if (response.status === 401 && authorized && state.token === requestToken) {
        logout("Your sign-in is no longer valid. Please log in again.", "auth");
      }
      const validation = data?.errors ? Object.values(data.errors).flat().join(" ") : null;
      const fallback = response.status === 403 ? "This action requires administrator access."
        : response.status === 401 ? "Please log in again." : `The request failed (${response.status}).`;
      throw new ApiError(validation || data?.detail || data?.title || fallback, response.status);
    }
    return data;
  }

  function renderNumbers(container, numbers, small = false) {
    container.replaceChildren();
    numbers.forEach((number) => {
      const ball = document.createElement("span");
      ball.className = small ? "number-ball small-ball" : "number-ball";
      ball.textContent = number;
      container.append(ball);
    });
  }

  function emptyTable(body, columns, message) {
    body.replaceChildren();
    const row = body.insertRow();
    const cell = row.insertCell();
    cell.colSpan = columns;
    cell.className = "empty-state";
    cell.textContent = message;
  }

  function textCell(row, text) {
    const cell = row.insertCell();
    cell.textContent = text;
    return cell;
  }

  function numberCell(row, numbers) {
    const container = document.createElement("div");
    container.className = "number-row";
    renderNumbers(container, numbers, true);
    row.insertCell().append(container);
  }

  function renderWinners(body, winners) {
    if (!winners.length) {
      emptyTable(body, 5, "No winning tickets yet. Check back after the next draw!");
      return;
    }
    body.replaceChildren();
    winners.forEach((winner) => {
      const row = body.insertRow();
      textCell(row, `#${winner.sessionNumber}`);
      textCell(row, winner.winnerFullName);
      numberCell(row, winner.winningNumbers);
      const badge = document.createElement("span");
      badge.className = "prize-badge";
      badge.textContent = winner.prizeName;
      row.insertCell().append(badge);
      textCell(row, dateFormatter.format(new Date(winner.drawDate)));
    });
  }

  function renderSelection() {
    $("selection-count").textContent = `${state.selected.size} / 7 selected`;
    renderNumbers($("selected-numbers"), [...state.selected].sort((a, b) => a - b));
    if (!state.selected.size) $("selected-numbers").textContent = "Your lucky numbers will appear here.";
    $("number-grid").querySelectorAll("button").forEach((button) => {
      const selected = state.selected.has(Number(button.dataset.number));
      button.setAttribute("aria-pressed", String(selected));
      button.disabled = state.submitting || (state.selected.size === 7 && !selected);
    });
    $("quick-pick").disabled = state.submitting;
    $("clear-picks").disabled = state.submitting || !state.selected.size;
    $("submit-ticket").disabled = state.submitting || state.selected.size !== 7 || !state.user || !state.session;
    $("submit-ticket").textContent = state.submitting ? "Submitting…" : "Confirm ticket";
  }

  function renderAuth() {
    const loggedIn = Boolean(state.user);
    $("auth-nav").hidden = loggedIn;
    $("account-label").hidden = !loggedIn;
    $("logout-button").hidden = !loggedIn;
    $("admin-nav").hidden = state.user?.role !== "Admin";
    $("login-prompt").hidden = loggedIn;
    $("my-tickets-section").hidden = !loggedIn;
    $("account-label").textContent = loggedIn ? `${state.user.username} · ${state.user.role}` : "";
    $("initiate-draw").disabled = state.drawing || !state.session || state.user?.role !== "Admin";
    renderSelection();
  }

  function renderSession(session) {
    state.session = session;
    $("session-label").textContent = session ? `Session #${session.sessionNumber} · ${session.status}` : "Session unavailable";
    $("admin-session").textContent = session ? `#${session.sessionNumber}` : "—";
    $("admin-ticket-count").textContent = session?.ticketCount ?? "—";
    $("admin-started").textContent = session ? dateFormatter.format(new Date(session.startTime)) : "—";
    $("initiate-draw").disabled = state.drawing || !session || state.user?.role !== "Admin";
    renderSelection();
  }

  async function loadSession() {
    try { renderSession(await api("/draws/current-session")); }
    catch (error) { renderSession(null); throw error; }
  }

  async function loadWinners() {
    renderWinners($("winners-body"), await api("/winners"));
  }

  async function loadTickets() {
    if (!state.user) return;
    const token = state.token;
    const tickets = await api("/tickets/my-tickets", { authorized: true });
    if (token !== state.token || !state.user) return;
    const body = $("my-tickets-body");
    if (!tickets.length) {
      emptyTable(body, 4, "No tickets yet. Pick seven numbers to enter the active session.");
      return;
    }
    body.replaceChildren();
    tickets.forEach((ticket) => {
      const row = body.insertRow();
      textCell(row, `Ticket #${ticket.id} / Session #${ticket.sessionNumber}`);
      numberCell(row, ticket.numbers);
      textCell(row, ticket.matchedCount === null ? "Active · Awaiting draw" : `${ticket.matchedCount} matches · ${ticket.prizeName}`);
      textCell(row, dateFormatter.format(new Date(ticket.submittedAt)));
    });
  }

  function navigate(view) {
    if (location.hash === `#${view}`) void route();
    else location.hash = view;
  }

  async function route() {
    if (!state.ready) return;
    let view = location.hash.slice(1);
    if (!["play", "winners", "admin", "auth"].includes(view)) view = "play";
    if (view === "admin" && state.user?.role !== "Admin") view = state.user ? "play" : "auth";
    if (view === "auth" && state.user) view = "play";
    if (location.hash !== `#${view}`) history.replaceState(null, "", `#${view}`);
    document.querySelectorAll(".view").forEach((section) => { section.hidden = section.id !== `view-${view}`; });
    document.querySelectorAll("[data-view]").forEach((link) => {
      if (link.dataset.view === view) link.setAttribute("aria-current", "page");
      else link.removeAttribute("aria-current");
    });
    $("nav-links").classList.remove("open");
    $("nav-toggle").setAttribute("aria-expanded", "false");
    try {
      if (view === "play") await Promise.all([loadSession(), loadTickets()]);
      if (view === "winners") await loadWinners();
      if (view === "admin") await loadSession();
    } catch (error) { showNotice(error.message); }
  }

  async function restoreAuth() {
    if (!state.token) return;
    if (expiresAt(state.token) <= Date.now()) {
      logout("Your sign-in has expired. Please log in again.", "auth");
      return;
    }
    const version = state.authVersion;
    try {
      const user = await api("/auth/me", { authorized: true });
      if (version !== state.authVersion) return;
      state.user = user;
      scheduleExpiry();
      renderAuth();
    } catch (error) {
      if (version !== state.authVersion) return;
      if (error.status === 400 || error.status === 401) logout("Please log in again.", "auth");
      else showNotice(error.message);
    }
  }

  async function submitAuth(event, endpoint) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const button = form.querySelector('button[type="submit"]');
    if (button.disabled) return;
    const body = Object.fromEntries(new FormData(form));
    button.disabled = true;
    form.setAttribute("aria-busy", "true");
    try {
      const response = await api(`/auth/${endpoint}`, { method: "POST", body });
      try { localStorage.setItem(tokenKey, response.token); }
      catch { throw new Error("Enable local storage for this site so your sign-in can be saved securely."); }
      state.authVersion++;
      state.token = response.token;
      state.user = response.user;
      scheduleExpiry();
      renderAuth();
      form.reset();
      navigate("play");
      showNotice(`Welcome, ${response.user.firstName}! Your next lucky ticket is waiting.`, "success");
    } catch (error) { showNotice(error.message); }
    finally { button.disabled = false; form.setAttribute("aria-busy", "false"); }
  }

  function showAuthTab(register) {
    $("login-panel").hidden = register;
    $("register-panel").hidden = !register;
    [$("login-tab"), $("register-tab")].forEach((tab, index) => {
      const selected = Boolean(index) === register;
      tab.classList.toggle("active", selected);
      tab.setAttribute("aria-selected", String(selected));
      tab.tabIndex = selected ? 0 : -1;
    });
  }

  // Rejection sampling avoids modulo bias; this is a player convenience, never the official draw RNG.
  function randomInt(maximum) {
    const random = new Uint32Array(1);
    const limit = Math.floor(0x100000000 / maximum) * maximum;
    do { crypto.getRandomValues(random); } while (random[0] >= limit);
    return random[0] % maximum;
  }

  function quickPick() {
    const pool = Array.from({ length: 37 }, (_, index) => index + 1);
    for (let index = 0; index < 7; index++) {
      const other = index + randomInt(pool.length - index);
      [pool[index], pool[other]] = [pool[other], pool[index]];
    }
    state.selected = new Set(pool.slice(0, 7));
    $("ticket-success").hidden = true;
    renderSelection();
  }

  // UI constraints improve feedback; the server independently validates every number and the session.
  async function submitTicket(event) {
    event.preventDefault();
    if (state.submitting || state.selected.size !== 7 || !state.user || !state.session) return;
    state.submitting = true;
    renderSelection();
    const token = state.token;
    try {
      const ticket = await api("/tickets", {
        method: "POST", authorized: true,
        body: { sessionId: state.session.id, numbers: [...state.selected].sort((a, b) => a - b) }
      });
      if (token !== state.token) return;
      $("ticket-success-text").textContent = `Ticket #${ticket.id}, session #${ticket.sessionNumber}. ${ticket.message}`;
      renderNumbers($("confirmed-numbers"), ticket.numbers);
      $("ticket-success").hidden = false;
      $("notice").hidden = true;
      state.selected.clear();
      await Promise.all([loadSession(), loadTickets()]);
    } catch (error) {
      showNotice(error.message);
      try { await loadSession(); } catch { /* Keep the actionable submission error visible. */ }
    } finally { state.submitting = false; renderSelection(); }
  }

  async function initiateDraw() {
    if (state.drawing || state.user?.role !== "Admin" || !state.session) return;
    // Capture the displayed session once so a delayed request cannot accidentally draw its successor.
    const sessionId = state.session.id;
    if (!window.confirm(`Draw session #${state.session.sessionNumber} with ${state.session.ticketCount} ticket(s)? This cannot be undone.`)) return;
    const token = state.token;
    state.drawing = true;
    $("initiate-draw").disabled = true;
    $("initiate-draw").textContent = "Drawing…";
    try {
      const result = await api("/draws/initiate", { method: "POST", authorized: true, body: { sessionId } });
      if (state.token !== token || state.user?.role !== "Admin") return;
      $("draw-result-title").textContent = `Results · Session #${result.sessionNumber}`;
      renderNumbers($("drawn-numbers"), result.drawnNumbers);
      $("draw-summary").textContent = `${result.ticketCount} tickets checked. ${result.winners.length} winning tickets. Session #${result.nextSession.sessionNumber} is now open.`;
      renderWinners($("draw-winners-body"), result.winners);
      $("draw-result").hidden = false;
      renderSession(result.nextSession);
      showNotice("The draw is complete and all results have been saved.", "success");
    } catch (error) {
      showNotice(error.message);
      try { await loadSession(); } catch { /* Never automatically repeat a draw request. */ }
    } finally {
      state.drawing = false;
      $("initiate-draw").textContent = "Initiate draw";
      renderAuth();
    }
  }

  function bindEvents() {
    for (let number = 1; number <= 37; number++) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "number-button";
      button.dataset.number = number;
      button.textContent = number;
      button.setAttribute("aria-label", `Number ${number}`);
      button.setAttribute("aria-pressed", "false");
      button.addEventListener("click", () => {
        if (state.selected.has(number)) state.selected.delete(number);
        else if (state.selected.size < 7) state.selected.add(number);
        $("ticket-success").hidden = true;
        renderSelection();
      });
      $("number-grid").append(button);
    }
    $("quick-pick").addEventListener("click", quickPick);
    $("clear-picks").addEventListener("click", () => { state.selected.clear(); renderSelection(); });
    $("ticket-form").addEventListener("submit", submitTicket);
    $("login-form").addEventListener("submit", (event) => void submitAuth(event, "login"));
    $("register-form").addEventListener("submit", (event) => void submitAuth(event, "register"));
    $("login-tab").addEventListener("click", () => showAuthTab(false));
    $("register-tab").addEventListener("click", () => showAuthTab(true));
    [$("login-tab"), $("register-tab")].forEach((tab) => tab.addEventListener("keydown", (event) => {
      if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
      event.preventDefault();
      const register = tab.id === "login-tab";
      showAuthTab(register);
      $(register ? "register-tab" : "login-tab").focus();
    }));
    $("logout-button").addEventListener("click", () => logout());
    $("initiate-draw").addEventListener("click", () => void initiateDraw());
    $("dismiss-notice").addEventListener("click", () => { $("notice").hidden = true; });
    $("nav-toggle").addEventListener("click", () => {
      const open = $("nav-links").classList.toggle("open");
      $("nav-toggle").setAttribute("aria-expanded", String(open));
    });
    [["refresh-tickets", loadTickets], ["refresh-winners", loadWinners], ["refresh-session", loadSession]].forEach(([id, load]) => {
      $(id).addEventListener("click", async () => {
        $(id).disabled = true;
        try { await load(); } catch (error) { showNotice(error.message); }
        finally { $(id).disabled = false; }
      });
    });
    window.addEventListener("hashchange", () => void route());
    window.addEventListener("storage", async (event) => {
      if (event.key !== tokenKey && event.key !== null) return;
      resetIdentity(readStoredToken());
      await restoreAuth();
      await route();
    });
  }

  async function initialize() {
    bindEvents();
    renderAuth();
    emptyTable($("winners-body"), 5, "Loading the winners board…");
    await restoreAuth();
    state.ready = true;
    await route();
    setInterval(async () => {
      if (document.hidden || state.submitting || state.drawing) return;
      try { await loadSession(); } catch { /* Background refresh must not replace an action's status message. */ }
    }, 30000);
  }

  void initialize().catch((error) => showNotice(error.message));
})();
