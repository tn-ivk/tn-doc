import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object для страницы Configurator
 */
export class ConfiguratorPage extends BasePage {
  // Tabs
  readonly generalTab: Locator;
  readonly devicesTab: Locator;

  // General Settings
  readonly exportSettings: Locator;
  readonly securitySettings: Locator;
  readonly elisSettings: Locator;

  // Device List
  readonly deviceList: Locator;
  readonly deviceSearchInput: Locator;
  readonly addDeviceButton: Locator;

  // Device Editor
  readonly deviceEditor: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  // Used SI (Используемые СИ)
  readonly usedPRCheckbox: Locator;
  readonly usedPPCheckbox: Locator;
  readonly usedPVLCheckbox: Locator;
  readonly usedPVSCheckbox: Locator;

  constructor(page: Page) {
    super(page);

    // Tabs
    this.generalTab = page.locator('[data-tab="general"], button:has-text("Общие")');
    this.devicesTab = page.locator('[data-tab="devices"], button:has-text("Устройства")');

    // General Settings
    this.exportSettings = page.locator('.export-settings, #exportSettings');
    this.securitySettings = page.locator('.security-settings, #securitySettings');
    this.elisSettings = page.locator('.elis-settings, #elisSettings');

    // Device List
    this.deviceList = page.locator('.device-list, #deviceList');
    this.deviceSearchInput = page.locator('input[placeholder*="Поиск"], #deviceSearch');
    this.addDeviceButton = page.locator('button:has-text("Добавить")');

    // Device Editor
    this.deviceEditor = page.locator('.device-editor, #deviceEditor');
    this.saveButton = page.locator('button:has-text("Сохранить"), .save-btn');
    this.cancelButton = page.locator('button:has-text("Отмена"), .cancel-btn');

    // Used SI checkboxes
    this.usedPRCheckbox = page.locator('input[name="UsedPR"], #usedPR');
    this.usedPPCheckbox = page.locator('input[name="UsedPP"], #usedPP');
    this.usedPVLCheckbox = page.locator('input[name="UsedPVL"], #usedPVL');
    this.usedPVSCheckbox = page.locator('input[name="UsedPVS"], #usedPVS');
  }

  async goto(): Promise<void> {
    await this.page.goto('/configurator');
    await this.waitForLoad();
  }

  /**
   * Переключение на вкладку "Общие"
   */
  async switchToGeneralTab(): Promise<void> {
    await this.generalTab.click();
    await this.page.waitForTimeout(300);
  }

  /**
   * Переключение на вкладку "Устройства"
   */
  async switchToDevicesTab(): Promise<void> {
    await this.devicesTab.click();
    await this.page.waitForTimeout(300);
  }

  /**
   * Поиск устройства по имени
   */
  async searchDevice(query: string): Promise<void> {
    await this.deviceSearchInput.fill(query);
    await this.page.waitForTimeout(300);
  }

  /**
   * Выбор устройства из списка по индексу
   */
  async selectDeviceByIndex(index: number): Promise<void> {
    await this.deviceList.locator(`li, .device-item`).nth(index).click();
    await this.page.waitForTimeout(300);
  }

  /**
   * Сохранение изменений
   */
  async saveChanges(): Promise<void> {
    await this.saveButton.click();
    await this.waitForLoad();
  }

  /**
   * Отмена изменений
   */
  async cancelChanges(): Promise<void> {
    await this.cancelButton.click();
  }

  /**
   * Установка флага "Используемые СИ"
   */
  async setUsedSI(siType: 'PR' | 'PP' | 'PVL' | 'PVS', enabled: boolean): Promise<void> {
    const checkbox = {
      'PR': this.usedPRCheckbox,
      'PP': this.usedPPCheckbox,
      'PVL': this.usedPVLCheckbox,
      'PVS': this.usedPVSCheckbox
    }[siType];

    if (enabled) {
      await checkbox.check();
    } else {
      await checkbox.uncheck();
    }
  }

  /**
   * Проверка состояния флага "Используемые СИ"
   */
  async isUsedSIChecked(siType: 'PR' | 'PP' | 'PVL' | 'PVS'): Promise<boolean> {
    const checkbox = {
      'PR': this.usedPRCheckbox,
      'PP': this.usedPPCheckbox,
      'PVL': this.usedPVLCheckbox,
      'PVS': this.usedPVSCheckbox
    }[siType];

    return await checkbox.isChecked();
  }
}
