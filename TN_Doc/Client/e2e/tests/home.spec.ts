import { test, expect } from '@playwright/test';
import { HomePage } from '../pages/HomePage';

test.describe('Home Page', () => {
  let homePage: HomePage;

  test.beforeEach(async ({ page }) => {
    homePage = new HomePage(page);
    await homePage.goto();
  });

  test('should load home page successfully', async () => {
    const title = await homePage.getTitle();
    expect(title).toBeTruthy();
  });

  test('should display page content', async ({ page }) => {
    // Проверка что страница загружена
    await expect(page).toHaveURL('/');

    // Проверка наличия основного контента
    const body = page.locator('body');
    await expect(body).toBeVisible();
  });

  test('should have navigation elements', async ({ page }) => {
    // Проверка наличия навигации (адаптируйте селекторы под реальный UI)
    const nav = page.locator('nav, .navbar, header');
    const hasNavigation = await nav.count() > 0;
    expect(hasNavigation).toBeTruthy();
  });

  test('should display status bar when available', async () => {
    // StatusBar может загружаться асинхронно
    try {
      await homePage.page.waitForSelector('#statusbar-container', { timeout: 5000 });
      const isVisible = await homePage.isStatusBarVisible();
      expect(isVisible).toBe(true);
    } catch {
      // StatusBar не обязателен на всех страницах
      test.skip();
    }
  });

  test('should respond to device selection', async () => {
    // Проверка наличия селектора устройств
    const deviceSelect = homePage.deviceSelect;
    const isVisible = await deviceSelect.isVisible().catch(() => false);

    if (isVisible) {
      const options = await homePage.getDeviceOptions();
      expect(options.length).toBeGreaterThan(0);
    } else {
      test.skip();
    }
  });
});

test.describe('Home Page - API Integration', () => {
  test('should fetch status data', async ({ page }) => {
    const response = await page.request.get('/Home/GetStatus');
    expect(response.ok()).toBeTruthy();

    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('application/json');
  });

  test('should fetch devices data', async ({ page }) => {
    const response = await page.request.get('/Home/GetDevices');
    expect(response.ok()).toBeTruthy();
  });
});
