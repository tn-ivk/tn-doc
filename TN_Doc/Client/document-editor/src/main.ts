import { createApp } from 'vue';
import { createPinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import ToastService from 'primevue/toastservice';
import ConfirmationService from 'primevue/confirmationservice';
import Tooltip from 'primevue/tooltip';
import { logger } from '@tn-doc/shared';
import router from './router';
import App from './App.vue';

// PrimeVue CSS
import 'primeicons/primeicons.css';
import '../../../wwwroot/css/material3.css';
import './assets/css/tooltip-styles.css';

// Инициализация логгера с глобальным контекстом
logger.setGlobalContext({
  component: 'DocumentEditor',
  version: '1.4.3'
});

logger.trace('DocumentEditor: инициализация приложения');

const app = createApp(App);

// Pinia
app.use(createPinia());

// Router
app.use(router);

// PrimeVue
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      darkModeSelector: '.dark-mode',
      cssLayer: {
        name: 'primevue',
        order: 'tailwind-base, primevue, tailwind-utilities'
      }
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
    matchAll: 'Совпадение всех',
    matchAny: 'Совпадение любого',
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
    monthNames: [
      'Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь',
      'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'
    ],
    monthNamesShort: ['Янв', 'Фев', 'Мар', 'Апр', 'Май', 'Июн', 'Июл', 'Авг', 'Сен', 'Окт', 'Ноя', 'Дек'],
    today: 'Сегодня',
    weekHeader: 'Нед',
    firstDayOfWeek: 1,
    dateFormat: 'dd.mm.yy',
    weak: 'Слабый',
    medium: 'Средний',
    strong: 'Сильный',
    passwordPrompt: 'Введите пароль',
    emptyFilterMessage: 'Результатов не найдено',
    emptyMessage: 'Нет доступных вариантов'
  }
});

// Toast и Confirmation сервисы
app.use(ToastService);
app.use(ConfirmationService);

// Директивы
app.directive('tooltip', Tooltip);

app.mount('#app');
