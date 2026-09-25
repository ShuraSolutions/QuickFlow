// Smoke regression for phase-03-habits: C1 (empty state leads to Add Habit) and C8 (add → complete → remove via UI).
async (page) => {
  const out = {};
  const name = 'Smoke habit ' + Date.now();
  await page.goto('http://localhost:5500/#/habits');
  await page.waitForSelector('#habit-results .grid, #habit-results .empty-state');

  await page.getByLabel('Show').selectOption('inactive');
  await page.waitForSelector('#habit-results .grid, #habit-results .empty-state');
  if (await page.locator('#habit-results .empty-state').count()) {
    await page.locator('#habit-results .empty-state').getByRole('button', { name: 'Add Habit' }).click();
    await page.locator('[role=dialog]').waitFor();
    out.C1 = 'empty state "' + await page.locator('.empty-state h2').textContent() + '" → dialog "' + await page.locator('[role=dialog] h2').textContent() + '"';
    await page.keyboard.press('Escape');
  } else {
    out.C1 = 'skipped: inactive habits exist';
  }
  await page.getByLabel('Show').selectOption('active');

  await page.getByRole('button', { name: '+ Add Habit' }).click();
  await page.locator('#hf-name').fill(name);
  await page.locator('[role=dialog]').getByRole('button', { name: 'Add habit' }).click();
  const card = page.locator('.habit-card', { hasText: name });
  await card.waitFor();
  await page.getByRole('button', { name: 'Complete ' + name + ' for today' }).click();
  await card.locator('[data-action=undo]').waitFor();
  const streak = await card.locator('.streak').textContent();
  await page.getByRole('button', { name: 'Remove ' + name }).click();
  await page.locator('[role=dialog]').getByRole('button', { name: 'Remove', exact: true }).click();
  await card.waitFor({ state: 'detached' });
  out.C8 = 'created, completed (' + streak + '), removed "' + name + '"';
  return out;
}
