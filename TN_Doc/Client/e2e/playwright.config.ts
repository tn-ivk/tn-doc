import { defineConfig, devices } from '@playwright/test';

/**
 * Конфигурация Playwright для E2E тестов TN_Doc
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Максимальное время на один тест */
  timeout: 30 * 1000,
  /* Глобальный таймаут для expect */
  expect: {
    timeout: 5000
  },
  /* Запуск тестов параллельно */
  fullyParallel: true,
  /* Не падать при первой ошибке в CI */
  forbidOnly: !!process.env.CI,
  /* Количество повторов при падении */
  retries: process.env.CI ? 2 : 0,
  /* Количество параллельных воркеров */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter для вывода результатов */
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list']
  ],
  /* Общие настройки для всех проектов */
  use: {
    /* Базовый URL для всех тестов */
    baseURL: process.env.BASE_URL || 'http://localhost:38509',

    /* Трассировка при первом повторе */
    trace: 'on-first-retry',

    /* Скриншоты при падении */
    screenshot: 'only-on-failure',

    /* Видео при падении */
    video: 'on-first-retry',
  },

  /* Конфигурация проектов для разных браузеров */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    /* Мобильные устройства */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
  ],

  /* Запуск локального сервера перед тестами (опционально) */
  // webServer: {
  //   command: 'dotnet run --project ../../TN_Doc.csproj',
  //   url: 'http://localhost:38509',
  //   reuseExistingServer: !process.env.CI,
  //   timeout: 120 * 1000,
  // },
});
