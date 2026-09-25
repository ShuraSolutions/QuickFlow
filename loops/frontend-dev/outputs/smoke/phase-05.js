// Smoke regression for phase-05-todo-plans: C1 (Create Plan opens the builder from the page / empty state)
// and C5 (create an in-progress plan with 2 items → toggle one → 50% with a live rest time → remove).
async (page) => {
  const out = {};
  const title = 'Smoke plan ' + Date.now();
  const pad = n => String(n).padStart(2, '0');
  const local = d => d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + 'T' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
  await page.goto('http://localhost:5500/#/plans');
  await page.waitForSelector('#plans-active .plan-list, #plans-active .empty-state');
  const empty = page.locator('#plans-active .empty-state');
  if (await empty.count()) await empty.getByRole('button', { name: 'Create Plan' }).click();
  else await page.getByRole('button', { name: '+ Create Plan' }).click();
  await page.locator('[role=dialog] .picker').first().waitFor();
  out.C1 = 'builder "' + await page.locator('[role=dialog] h2').textContent() + '" with ' + await page.locator('.picker-option').count() + ' selectable sources';

  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Start date/time').fill(local(new Date(Date.now() - 60000)));
  await page.getByLabel('End date/time').fill(local(new Date(Date.now() + 3600000)));
  const options = page.locator('.picker-option input');
  await options.nth(0).check();
  await options.nth(1).check();
  await page.locator('[role=dialog]').getByRole('button', { name: 'Create plan' }).click();
  const card = page.locator('#plans-active .plan-card', { hasText: title });
  await card.waitFor();
  const rest1 = await card.locator('.rest-time').textContent();
  // the plan already started: its start notification appears within one poll (10 s)
  const startToast = page.locator('.toast', { hasText: '"' + title + '" has started' });
  await startToast.waitFor({ timeout: 15000 });
  await card.locator('.plan-items input').first().check();
  await card.locator('.plan-pct', { hasText: '50%' }).waitFor();
  const rest2 = await card.locator('.rest-time').textContent();
  const pct = await card.locator('.plan-pct').textContent();
  await page.getByRole('button', { name: 'Remove ' + title }).click();
  await page.locator('[role=dialog]').getByRole('button', { name: 'Remove', exact: true }).click();
  await card.waitFor({ state: 'detached' });
  // removing the plan closes its start toast without acknowledging it
  await startToast.waitFor({ state: 'detached', timeout: 5000 });
  out.C5 = 'created "' + title + '", start toast shown, item toggled → ' + pct + ', rest time ' + rest1 + ' → ' + rest2 + ', removed (start toast closed)';
  return out;
}
