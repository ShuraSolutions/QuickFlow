// Smoke regression for phase-04-learning: C1 (an empty state leads to Add Learning Card) and C9 (add → milestone → remove via UI).
async (page) => {
  const out = {};
  const title = 'Smoke card ' + Date.now();
  await page.goto('http://localhost:5500/#/learning');
  await page.waitForSelector('#learning-results .grid, #learning-results .empty-state');

  out.C1 = 'no empty state found';
  for (const status of ['', 'NotStarted', 'InProgress', 'Completed']) {
    await page.locator('#learning-status').selectOption(status);
    await page.waitForTimeout(300);
    await page.waitForSelector('#learning-results .grid, #learning-results .empty-state');
    if (await page.locator('#learning-results .empty-state').count()) {
      await page.locator('#learning-results .empty-state').getByRole('button', { name: 'Add Learning Card' }).click();
      await page.locator('[role=dialog]').waitFor();
      out.C1 = 'filter "' + (status || 'All') + '": "' + await page.locator('.empty-state h2').textContent() + '" → dialog "' + await page.locator('[role=dialog] h2').textContent() + '"';
      await page.keyboard.press('Escape');
      break;
    }
  }
  await page.locator('#learning-status').selectOption('');

  await page.getByRole('button', { name: '+ Add Learning Card' }).click();
  await page.locator('#lf-title').fill(title);
  await page.locator('[role=dialog]').getByRole('button', { name: 'Add card' }).click();
  const card = page.locator('.learning-card', { hasText: title });
  await card.waitFor();
  await page.getByRole('button', { name: 'Show milestones and notes of ' + title }).click();
  await card.getByLabel('New milestone title').fill('Smoke step');
  await card.getByRole('button', { name: 'Add milestone' }).click();
  await card.locator('.milestones li', { hasText: 'Smoke step' }).waitFor();
  const count = await card.locator('.milestone-count').textContent();
  await page.getByRole('button', { name: 'Remove ' + title }).click();
  await page.locator('[role=dialog]').getByRole('button', { name: 'Remove', exact: true }).click();
  await card.waitFor({ state: 'detached' });
  out.C9 = 'created (' + count + '), removed "' + title + '"';
  return out;
}
