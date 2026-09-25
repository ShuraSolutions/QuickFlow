// Smoke regression for phase-06-dashboard: C1 (greeting + four metric cards equal GET /api/dashboard).
async (page) => {
  await page.goto('http://localhost:5500/#/dashboard');
  await page.locator('.metric-card').first().waitFor();
  const res = await page.evaluate(async () => {
    const d = await Api.dashboard.get();
    const s = d.summary;
    const expected = {
      tasks: Math.round(s.tasks.completionPercentage) + '% done',
      habits: s.habits.completedToday + ' / ' + s.habits.active,
      plans: s.plans.inProgress + ' in progress',
      learning: s.learning.inProgress + ' in progress'
    };
    const actual = Object.fromEntries([...document.querySelectorAll('.metric-card')].map(m => [m.dataset.metric, m.querySelector('.metric-value').textContent]));
    return { greeting: document.querySelector('.greeting-name').textContent, displayName: d.displayName, expected, actual };
  });
  const ok = res.greeting.endsWith(', ' + res.displayName) && JSON.stringify(res.expected) === JSON.stringify(res.actual);
  return { C1: (ok ? 'PASS ' : 'FAIL ') + JSON.stringify(res) };
}
