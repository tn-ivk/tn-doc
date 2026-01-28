import { Page, Locator } from '@playwright/test';

/**
 * Базовый класс Page Object Model для TN_Doc
 */
export abstract class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Переход на страницу
   */
  abstract goto(): Promise<void>;

  /**
   * Ожидание полной загрузки страницы
   */
  async waitForLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Получение текущего URL
   */
  get currentUrl(): string {
    return this.page.url();
  }

  /**
   * Получение заголовка страницы
   */
  async getTitle(): Promise<string> {
    return await this.page.title();
  }

  /**
   * Проверка наличия элемента на странице
   */
  async isElementVisible(locator: Locator): Promise<boolean> {
    return await locator.isVisible();
  }

  /**
   * Ожидание появления элемента
   */
  async waitForElement(locator: Locator, timeout: number = 5000): Promise<void> {
    await locator.waitFor({ state: 'visible', timeout });
  }

  /**
   * Снятие скриншота страницы
   */
  async takeScreenshot(name: string): Promise<Buffer> {
    return await this.page.screenshot({
      path: `screenshots/${name}.png`,
      fullPage: true
    });
  }
}
