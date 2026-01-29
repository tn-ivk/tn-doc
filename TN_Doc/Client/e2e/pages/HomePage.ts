import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object для главной страницы TN_Doc
 */
export class HomePage extends BasePage {
  // Locators
  readonly statusBar: Locator;
  readonly deviceSelect: Locator;
  readonly documentList: Locator;
  readonly refreshButton: Locator;
  readonly navMenu: Locator;

  constructor(page: Page) {
    super(page);

    this.statusBar = page.locator('#statusbar-container');
    this.deviceSelect = page.locator('#deviceSelect, select[name="device"]');
    this.documentList = page.locator('.document-list, #documents');
    this.refreshButton = page.locator('button:has-text("Обновить"), .refresh-btn');
    this.navMenu = page.locator('nav, .navbar, #navigation');
  }

  async goto(): Promise<void> {
    await this.page.goto('/');
    await this.waitForLoad();
  }

  /**
   * Выбор устройства из списка
   */
  async selectDevice(deviceId: number): Promise<void> {
    await this.deviceSelect.selectOption({ value: deviceId.toString() });
    await this.page.waitForTimeout(500); // Ожидание загрузки данных устройства
  }

  /**
   * Получение списка доступных устройств
   */
  async getDeviceOptions(): Promise<string[]> {
    const options = await this.deviceSelect.locator('option').allTextContents();
    return options;
  }

  /**
   * Клик по кнопке обновления
   */
  async clickRefresh(): Promise<void> {
    await this.refreshButton.click();
    await this.waitForLoad();
  }

  /**
   * Проверка отображения StatusBar
   */
  async isStatusBarVisible(): Promise<boolean> {
    return await this.statusBar.isVisible();
  }

  /**
   * Переход к документу по имени
   */
  async goToDocument(documentName: string): Promise<void> {
    await this.page.click(`text=${documentName}`);
    await this.waitForLoad();
  }

  /**
   * Переход в конфигуратор
   */
  async goToConfigurator(): Promise<void> {
    await this.page.goto('/configurator');
    await this.waitForLoad();
  }
}
