// Smoke regression for phase-02-tasks: C1 (empty/no-match state leads to Add Task) and C9 (add then delete via UI).
async (page) => {
  const out = {};
  const title = 'Smoke task ' + Date.now();
  await page.goto('http://localhost:5500/#/tasks');
  await page.waitForSelector('#task-results .task-list, #task-results .empty-state');

  await page.getByRole('searchbox', { name: 'Search' }).fill('zz-no-match-zz');
  await page.getByText('No tasks match your filters').waitFor();
  await page.locator('#task-results').getByRole('button', { name: 'Add Task', exact: true }).click();
  await page.locator('[role=dialog]').waitFor();
  out.C1 = 'empty state → dialog "' + await page.locator('[role=dialog] h2').textContent() + '"';
  await page.keyboard.press('Escape');
  await page.getByRole('button', { name: 'Clear filters' }).click();

  await page.getByRole('button', { name: '+ Add Task' }).click();
  await page.locator('#tf-title').fill(title);
  await page.locator('[role=dialog]').getByRole('button', { name: 'Add task' }).click();
  await page.locator('.task-row', { hasText: title }).waitFor();
  await page.getByRole('button', { name: 'Delete ' + title }).click();
  await page.locator('[role=dialog]').getByRole('button', { name: 'Delete', exact: true }).click();
  await page.locator('.task-row', { hasText: title }).waitFor({ state: 'detached' });
  out.C9 = 'created and deleted "' + title + '"; rows now: ' + await page.locator('.task-row').count();
  return out;
}
