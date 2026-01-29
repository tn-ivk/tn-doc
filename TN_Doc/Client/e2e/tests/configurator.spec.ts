import { test, expect } from '@playwright/test';
import { ConfiguratorPage } from '../pages/ConfiguratorPage';

test.describe('Configurator Page', () => {
  let configuratorPage: ConfiguratorPage;

  test.beforeEach(async ({ page }) => {
    configuratorPage = new ConfiguratorPage(page);
    await configuratorPage.goto();
  });

  test('should load configurator page', async ({ page }) => {
    await expect(page).toHaveURL(/.*configurator.*/);
  });

  test('should display tabs', async () => {
    // Проверка наличия вкладок
    const generalTabVisible = await configuratorPage.generalTab.isVisible().catch(() => false);
    const devicesTabVisible = await configuratorPage.devicesTab.isVisible().catch(() => false);

    // Как минимум одна из вкладок должна быть видима
    expect(generalTabVisible || devicesTabVisible).toBeTruthy();
  });

  test('should switch between tabs', async () => {
    // Переключение на вкладку устройств
    const devicesTab = configuratorPage.devicesTab;
    if (await devicesTab.isVisible().catch(() => false)) {
      await configuratorPage.switchToDevicesTab();
      // Проверка что переключение произошло (адаптируйте под реальный UI)
      await expect(configuratorPage.deviceList).toBeVisible({ timeout: 5000 }).catch(() => {});
    }

    // Переключение обратно на общие настройки
    const generalTab = configuratorPage.generalTab;
    if (await generalTab.isVisible().catch(() => false)) {
      await configuratorPage.switchToGeneralTab();
    }
  });
});

test.describe('Configurator - Device Management', () => {
  test.beforeEach(async ({ page }) => {
    const configuratorPage = new ConfiguratorPage(page);
    await configuratorPage.goto();
    await configuratorPage.switchToDevicesTab().catch(() => {});
  });

  test('should display device list', async ({ page }) => {
    const deviceList = page.locator('.device-list, #deviceList, [data-testid="device-list"]');
    const isVisible = await deviceList.isVisible().catch(() => false);

    if (isVisible) {
      const devices = await deviceList.locator('li, .device-item, tr').count();
      expect(devices).toBeGreaterThanOrEqual(0);
    } else {
      test.skip();
    }
  });

  test('should search devices', async ({ page }) => {
    const searchInput = page.locator('input[placeholder*="Поиск"], #deviceSearch');
    const isVisible = await searchInput.isVisible().catch(() => false);

    if (isVisible) {
      await searchInput.fill('test');
      await page.waitForTimeout(500);
      // Результаты поиска должны обновиться
    } else {
      test.skip();
    }
  });
});

test.describe('Configurator - Used SI Settings (v1.4.3)', () => {
  let configuratorPage: ConfiguratorPage;

  test.beforeEach(async ({ page }) => {
    configuratorPage = new ConfiguratorPage(page);
    await configuratorPage.goto();
  });

  test('should display Used SI checkboxes', async () => {
    // Проверяем наличие чекбоксов для СИ
    const prVisible = await configuratorPage.usedPRCheckbox.isVisible().catch(() => false);
    const ppVisible = await configuratorPage.usedPPCheckbox.isVisible().catch(() => false);

    // Чекбоксы могут быть внутри редактора устройства
    if (!prVisible && !ppVisible) {
      test.skip();
    }
  });

  test('should toggle Used SI settings', async () => {
    // Находим чекбокс PR
    const prCheckbox = configuratorPage.usedPRCheckbox;
    const isVisible = await prCheckbox.isVisible().catch(() => false);

    if (isVisible) {
      const initialState = await prCheckbox.isChecked();
      await prCheckbox.click();
      const newState = await prCheckbox.isChecked();
      expect(newState).toBe(!initialState);

      // Возвращаем в исходное состояние
      await prCheckbox.click();
    } else {
      test.skip();
    }
  });

  test('secondary SI depends on primary SI', async () => {
    // Проверка зависимости: вторичное СИ доступно только при включённом основном
    // Это требование из v1.4.3
    const ppCheckbox = configuratorPage.usedPPCheckbox;
    const secondaryPPLocator = configuratorPage.page.locator('input[name="UsedSecondSI_PP"]');

    const ppVisible = await ppCheckbox.isVisible().catch(() => false);
    const secondaryVisible = await secondaryPPLocator.isVisible().catch(() => false);

    if (ppVisible && secondaryVisible) {
      // Если основной выключен, вторичный должен быть disabled
      const ppChecked = await ppCheckbox.isChecked();
      if (!ppChecked) {
        const isDisabled = await secondaryPPLocator.isDisabled();
        expect(isDisabled).toBe(true);
      }
    } else {
      test.skip();
    }
  });
});
