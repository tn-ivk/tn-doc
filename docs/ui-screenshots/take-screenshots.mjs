// NOTE: Скрипт использует legacy‑селекторы (/configurator, statusbar).
// В текущем UI (Razor + Bootstrap) эти элементы отсутствуют — при использовании обновите селекторы.
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const BASE_URL = process.env.TN_DOC_BASE_URL || 'http://localhost:5000';
const OUT_DIR = path.resolve(process.cwd(), '../assets/ui');

async function ensureDir(dir) {
  await fs.promises.mkdir(dir, { recursive: true });
}

async function screenshot(page, file, opts = {}) {
  const full = path.join(OUT_DIR, file);
  await ensureDir(path.dirname(full));
  await page.screenshot({ path: full, fullPage: true, ...opts });
}

async function screenshotElement(page, selector, file) {
  const el = await page.waitForSelector(selector, { state: 'visible', timeout: 10000 });
  const full = path.join(OUT_DIR, file);
  await ensureDir(path.dirname(full));
  await el.screenshot({ path: full });
}

async function screenshotElementSafe(page, selector, file) {
  try {
    await screenshotElement(page, selector, file);
    return true;
  } catch (e) {
    const handle = await page.$(selector);
    if (!handle) return false;
    try {
      const full = path.join(OUT_DIR, file);
      await ensureDir(path.dirname(full));
      await page.evaluate((el) => {
        el.style.minHeight = el.style.minHeight || '40px';
        el.style.minWidth = el.style.minWidth || '200px';
        el.style.display = 'block';
        el.style.visibility = 'visible';
      }, handle);
      await handle.screenshot({ path: full });
      return true;
    } catch {
      return false;
    }
  }
}

async function run() {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Home / Index
  await page.goto(`${BASE_URL}/`);
  await page.waitForLoadState('networkidle');
  await screenshot(page, 'home/home-full.png');

  // Основные элементы страницы Home
  await screenshotElement(page, 'table.mainTable', 'home/mainTable.png');
  await screenshotElement(page, '.leftPanel', 'home/leftPanel.png');
  await screenshotElement(page, '.rightPanel', 'home/rightPanel.png');
  await screenshotElement(page, '#DataTable', 'home/dataTable.png');

  // Кнопка меню и dropdown
  await page.click('#MenuButton');
  await page.waitForSelector('.dropdown-menu.show, .dropdown-menu', { state: 'visible' });
  await screenshotElement(page, '.dropdown-menu', 'home/menu-dropdown.png');

  // Модалка "Справочники"
  await page.click('#MenuItemDictionaries');
  await page.waitForSelector('#modal-window.show, #modal-window .modal-content', { state: 'visible' });
  await screenshotElement(page, '#modal-window .modal-content', 'dictionaries/modal.png');
  await screenshotElement(page, '.side-menu-list', 'dictionaries/side-menu.png');
  await screenshotElement(page, '.dictionaries', 'dictionaries/content.png');
  await page.click('#modal-window .close-modal-wnd-btn');

  // Окно конфигуратора (открывается в iframe)
  await page.click('#MenuButton');
  await page.click('#MenuItemConfigurator');
  await page.waitForSelector('#configurator-window .modal-content', { state: 'visible' });
  await screenshotElement(page, '#configurator-window .modal-content', 'configurator/modal.png');
  // Содержимое iframe конфигуратора
  const frameEl = await page.waitForSelector('#configurator-iframe');
  const frame = await frameEl.contentFrame();
  if (frame) {
    await frame.waitForLoadState('domcontentloaded');
    await frame.waitForLoadState('networkidle').catch(() => {});
    await screenshot(page, 'configurator/iframe-full.png');
    // Пробуем снять основные элементы конфигуратора, если доступны
    const possibleSelectors = [
      '#app', '.device-editor', '.toolbar', '.form', '.panel', '.header', '.footer'
    ];
    for (const sel of possibleSelectors) {
      try {
        const el = await frame.waitForSelector(sel, { state: 'visible', timeout: 2000 });
        if (el) {
          const box = await el.boundingBox();
          if (box) {
            await screenshot(page, `configurator/frame-${sel.replace(/[^a-z0-9_-]/gi, '_')}.png`);
          }
        }
      } catch {}
    }
  }
  // Закрыть конфигуратор
  await page.click('#configurator-window .modal-body', { position: { x: 10, y: 10 } }).catch(() => {});
  await page.keyboard.press('Escape').catch(() => {});

  // Status bar
  await page.waitForSelector('#status-bar, #status-bar-app, .status-bar', { state: 'attached' });
  const statusSelectors = ['#status-bar', '#status-bar-app', '.status-bar'];
  for (const s of statusSelectors) {
    await screenshotElementSafe(page, s, `statusbar/${s.replace(/[^a-z0-9_-]/gi, '_')}.png`);
  }

  // PDF просмотрщик (iframe)
  try { await screenshotElement(page, 'iframe.FR', 'home/pdf-frame.png'); } catch {}

  // Страница конфигуратора напрямую
  await page.goto(`${BASE_URL}/configurator`);
  await page.waitForLoadState('networkidle');
  await screenshot(page, 'configurator/direct-full.png');

  // Финал
  await browser.close();
  console.log(`Screenshots saved to: ${OUT_DIR}`);
}

run().catch((err) => {
  console.error(err);
  process.exit(1);
});

