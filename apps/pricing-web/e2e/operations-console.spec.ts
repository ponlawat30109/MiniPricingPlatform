import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('http://localhost:5000/rules**', async (route) => {
    const isGet = route.request().method() === 'GET';
    await route.fulfill({ status: isGet ? 200 : 201, json: isGet ? [] : {} });
  });
  await page.route('http://localhost:8080/quotes/price', (route) =>
    route.fulfill({
      json: {
        basePrice: 100,
        surcharges: 50,
        discounts: 10,
        totalPrice: 140,
        appliedRules: ['Base rate'],
      },
    }),
  );
  await page.route('http://localhost:8080/quotes/bulk', (route) =>
    route.fulfill({ status: 202, json: { job_id: 'job-1' } }),
  );
  await page.route('http://localhost:8080/jobs/job-1', (route) =>
    route.fulfill({
      json: {
        jobId: 'job-1',
        status: 'Completed',
        results: [{ reference: 'REF-1', totalPrice: 140 }],
      },
    }),
  );
});

test('calculates a quote and navigates to rules', async ({ page }) => {
  await page.goto('/quotes');
  await expect(page.locator('#delivery-area-options option')).toHaveCount(3);
  await page.getByLabel('Weight').fill('12');
  await page.getByLabel('Area').fill('Bangkok');
  await page.getByRole('button', { name: 'Calculate price' }).click();
  await expect(page.getByText(/140/).first()).toBeVisible();
  await page.getByRole('link', { name: 'Rules' }).click();
  await expect(page).toHaveURL(/\/rules$/);
});

test('accepts a custom delivery area', async ({ page }) => {
  await page.goto('/quotes');
  await page.getByLabel('Weight').fill('250');
  await page.getByLabel('Delivery area').fill('Chiang Mai city centre');
  await page.getByRole('button', { name: 'Calculate price' }).click();
  await expect(page.getByText(/140/).first()).toBeVisible();
});

test('rule editor is keyboard dismissible', async ({ page }) => {
  await page.goto('/rules');
  await page.getByRole('button', { name: /new rule/i }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByPlaceholder('No limit')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toBeHidden();
});

test('submits a JSON bulk job and reports completion', async ({ page }) => {
  await page.goto('/jobs');
  await page.getByRole('button', { name: /submit job/i }).click();
  await expect(page.getByText('Completed')).toBeVisible();
  await expect(page.getByText(/1 result/i)).toBeVisible();
});

test('persists dark mode at a mobile viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/quotes');
  await page.getByRole('button', { name: /use dark mode/i }).click();
  await page.reload();
  await expect(page.getByRole('button', { name: /use light mode/i })).toBeVisible();
});
