// Smoke regression for phase-07-settings: C1 (the settings form shows the values of GET /api/settings).
async (page) => {
  await page.goto('http://localhost:5500/#/settings');
  await page.waitForFunction(() => document.querySelector('#sf-name') && document.querySelector('#sf-name').value !== '');
  const res = await page.evaluate(async () => {
    const s = await Api.settings.get();
    const api = { displayName: s.displayName, email: s.email || '', planStartNotificationsEnabled: s.planStartNotificationsEnabled, notificationLeadMinutes: s.notificationLeadMinutes, defaultView: s.defaultView, theme: s.theme };
    const form = { displayName: document.querySelector('#sf-name').value, email: document.querySelector('#sf-email').value, planStartNotificationsEnabled: document.querySelector('#sf-notify').checked, notificationLeadMinutes: Number(document.querySelector('#sf-lead').value), defaultView: document.querySelector('#sf-view').value, theme: document.querySelector('#sf-theme').value };
    return { api, form, dataTheme: document.documentElement.getAttribute('data-theme') };
  });
  const ok = JSON.stringify(res.api) === JSON.stringify(res.form);
  return { C1: (ok ? 'PASS ' : 'FAIL ') + JSON.stringify(res) };
}
