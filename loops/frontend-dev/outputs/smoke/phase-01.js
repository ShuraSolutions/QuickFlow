async (page) => {
  const out = {};
  await page.goto('http://localhost:5500/');
  await page.waitForSelector('#page h1');
  await page.waitForFunction(() => document.querySelector('#api-status').dataset.state !== 'unknown');
  out.C1 = await page.evaluate(() => ({
    hash: location.hash,
    links: [...document.querySelectorAll('.nav a')].map(a => a.textContent.trim()),
    h1: document.querySelector('#page h1').textContent
  }));
  out.C4 = await page.evaluate(() => document.querySelector('#api-status .label').textContent);
  out.C2 = [];
  for (const name of ['Tasks', 'Habits', 'Learning Resources', 'Todo Plans', 'Settings', 'Dashboard']) {
    await page.getByRole('link', { name, exact: true }).click();
    await page.waitForFunction(n => document.querySelector('#page h1')?.textContent === n, name);
    out.C2.push(await page.evaluate(() => location.hash + ' ' + document.querySelector('#page h1').textContent + ' active=' +
      [...document.querySelectorAll('.nav a[aria-current=page]')].map(a => a.textContent.trim()).join(',')));
  }
  return out;
}
