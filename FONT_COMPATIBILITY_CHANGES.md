# Изменения совместимости шрифтов

## Проблема
Шрифт Roboto не входит в стандартный набор шрифтов Windows и Astra Linux. Приложение работает без интернета, поэтому загрузка шрифта невозможна.

## Решение
Заменен Roboto на системные шрифты с правильным fallback:

### Измененные файлы:

1. **TN_Doc/wwwroot/css/material3.css** (строка 27)
   - Было: `'Roboto', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', Arial, sans-serif`
   - Стало: `'Segoe UI', 'PT Astra Sans', -apple-system, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif`

2. **TN_Doc/Views/Shared/_Layout.cshtml** (строки 7-9)
   - Удалены ссылки на Google Fonts для загрузки Roboto

3. **TN_Doc/Views/ConfiguratorView/Index.cshtml** (строка 20)
   - Обновлен font-family для конфигуратора

## Результат
- **Windows**: будет использоваться Segoe UI (стандартный системный шрифт)
- **Astra Linux**: будет использоваться PT Astra Sans (стандартный системный шрифт)
- **Fallback**: Arial (присутствует в папке fonts/)
- **Универсальный fallback**: sans-serif

## Совместимость
Приложение теперь полностью совместимо с офлайн-установками на Windows и Astra Linux без необходимости загрузки внешних шрифтов.
