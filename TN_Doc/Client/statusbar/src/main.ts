import { createApp } from 'vue';
import { createPinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import Tooltip from 'primevue/tooltip';
import 'primeicons/primeicons.css';
import App from './App.vue';

/**
 * Инициализация StatusBar приложения
 * Интегрируется в существующую страницу TN_Doc
 */
function initStatusBar() {
  const container = document.getElementById('status-bar');

  if (!container) {
    console.warn('[StatusBar] Container element not found. Status bar will not be initialized.');
    return;
  }

  try {
    const app = createApp(App);
    const pinia = createPinia();

    app.use(pinia);

    // Настройка PrimeVue с русской локализацией
    app.use(PrimeVue, {
      theme: {
        preset: Aura,
        options: {
          prefix: 'p',
          darkModeSelector: false,
          cssLayer: false
        }
      },
      locale: {
        startsWith: 'Начинается с',
        contains: 'Содержит',
        notContains: 'Не содержит',
        endsWith: 'Заканчивается на',
        equals: 'Равно',
        notEquals: 'Не равно',
        noFilter: 'Без фильтра',
        filter: 'Фильтр',
        lt: 'Меньше чем',
        lte: 'Меньше или равно',
        gt: 'Больше чем',
        gte: 'Больше или равно',
        dateIs: 'Дата равна',
        dateIsNot: 'Дата не равна',
        dateBefore: 'Дата до',
        dateAfter: 'Дата после',
        clear: 'Очистить',
        apply: 'Применить',
        matchAll: 'Совпадает со всеми',
        matchAny: 'Совпадает с любым',
        addRule: 'Добавить правило',
        removeRule: 'Удалить правило',
        accept: 'Да',
        reject: 'Нет',
        choose: 'Выбрать',
        upload: 'Загрузить',
        cancel: 'Отмена',
        dayNames: ['Воскресенье', 'Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота'],
        dayNamesShort: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
        dayNamesMin: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
        monthNames: ['Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь', 'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'],
        monthNamesShort: ['Янв', 'Фев', 'Мар', 'Апр', 'Май', 'Июн', 'Июл', 'Авг', 'Сен', 'Окт', 'Ноя', 'Дек'],
        today: 'Сегодня',
        weekHeader: 'Нед',
        firstDayOfWeek: 1,
        showMonthAfterYear: false,
        dateFormat: 'dd.mm.yy',
        weak: 'Слабый',
        medium: 'Средний',
        strong: 'Сильный',
        passwordPrompt: 'Введите пароль',
        emptyFilterMessage: 'Результатов не найдено',
        searchMessage: 'Доступно {0} результатов',
        selectionMessage: 'Выбрано {0} элементов',
        emptySelectionMessage: 'Не выбрано ни одного элемента',
        emptySearchMessage: 'Результатов не найдено',
        emptyMessage: 'Нет доступных вариантов',
        aria: {
          trueLabel: 'Истина',
          falseLabel: 'Ложь',
          nullLabel: 'Не выбрано',
          star: '1 звезда',
          stars: '{star} звезд',
          selectAll: 'Выбраны все элементы',
          unselectAll: 'Все элементы не выбраны',
          close: 'Закрыть',
          previous: 'Предыдущий',
          next: 'Следующий',
          navigation: 'Навигация',
          scrollTop: 'Прокрутить в начало',
          moveTop: 'Переместить в начало',
          moveUp: 'Переместить вверх',
          moveDown: 'Переместить вниз',
          moveBottom: 'Переместить в конец',
          moveToTarget: 'Переместить в цель',
          moveToSource: 'Переместить в источник',
          moveAllToTarget: 'Переместить всё в цель',
          moveAllToSource: 'Переместить всё в источник',
          pageLabel: 'Страница {page}',
          firstPageLabel: 'Первая страница',
          lastPageLabel: 'Последняя страница',
          nextPageLabel: 'Следующая страница',
          previousPageLabel: 'Предыдущая страница',
          rowsPerPageLabel: 'Строк на странице',
          jumpToPageDropdownLabel: 'Перейти к раскрывающемуся списку страниц',
          jumpToPageInputLabel: 'Перейти к вводу страницы',
          selectRow: 'Выбрана строка',
          unselectRow: 'Строка не выбрана',
          expandRow: 'Строка развернута',
          collapseRow: 'Строка свернута',
          showFilterMenu: 'Показать меню фильтра',
          hideFilterMenu: 'Скрыть меню фильтра',
          filterOperator: 'Оператор фильтра',
          filterConstraint: 'Ограничение фильтра',
          editRow: 'Редактирование строки',
          saveEdit: 'Сохранить правку',
          cancelEdit: 'Отменить правку',
          listView: 'Список',
          gridView: 'Сетка',
          slide: 'Слайд',
          slideNumber: '{slideNumber}',
          zoomImage: 'Увеличить изображение',
          zoomIn: 'Увеличить',
          zoomOut: 'Уменьшить',
          rotateRight: 'Повернуть вправо',
          rotateLeft: 'Повернуть влево'
        }
      }
    });

    // Регистрация глобальных директив
    app.directive('tooltip', Tooltip);

    app.mount(container);

    console.info('[StatusBar] Application initialized successfully');
  } catch (error) {
    console.error('[StatusBar] Failed to initialize:', error);
  }
}

// Wait for DOM to be ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initStatusBar);
} else {
  initStatusBar();
}