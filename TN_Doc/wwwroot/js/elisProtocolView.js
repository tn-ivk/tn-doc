/**
 * Модуль для отображения данных протокола ЕЛИС
 * Поддерживает два режима: табличное представление и сырой JSON
 */

// Текущие данные протокола для переключения режимов отображения
let currentProtocolData = null;

/**
 * Показать окно просмотра данных протокола ЕЛИС
 * @param {Object} protocolData - данные протокола ЕЛИС
 */
function showProtocolView(protocolData) {
    logTrace('Открытие окна просмотра протокола ЕЛИС: ' + (protocolData.protocolNumber || '[нет номера]'));

    // Сохраняем данные для переключения режимов
    currentProtocolData = protocolData;

    // Форматируем JSON для читаемого отображения
    const formattedJson = JSON.stringify(protocolData, null, 2);

    // Заполняем JSON содержимое
    document.getElementById('protocolViewContent').textContent = formattedJson;
    document.getElementById('protocolViewTitle').textContent =
        'Данные протокола ЕЛИС: ' + (protocolData.protocolNumber || '');

    // Заполняем таблицы
    fillProtocolTables(protocolData);

    // По умолчанию показываем табличное представление
    switchProtocolView('table');

    // Скрываем окно запроса ЕЛИС и показываем окно просмотра
    $('#elis').modal('hide');
    setTimeout(function() {
        $('#elisProtocolView').modal('show');
    }, 300);
}

/**
 * Переключение режима отображения протокола
 * @param {string} mode - режим отображения: 'table' или 'json'
 */
function switchProtocolView(mode) {
    const tableView = document.getElementById('protocolTableView');
    const jsonView = document.getElementById('protocolViewContent');
    const btnTable = document.getElementById('btnViewTable');
    const btnJson = document.getElementById('btnViewJson');

    if (mode === 'table') {
        tableView.style.display = 'block';
        jsonView.style.display = 'none';
        btnTable.classList.add('active');
        btnJson.classList.remove('active');
    } else {
        tableView.style.display = 'none';
        jsonView.style.display = 'block';
        btnTable.classList.remove('active');
        btnJson.classList.add('active');
    }
}

/**
 * Заполнение таблиц данными протокола
 * @param {Object} protocolData - данные протокола ЕЛИС
 */
function fillProtocolTables(protocolData) {
    // Заполняем таблицу исходных данных
    fillBaseDataTable(protocolData);

    // Заполняем таблицу показателей качества
    fillParamsTable(protocolData);
}

/**
 * Заполнение таблицы исходных данных
 * @param {Object} data - данные протокола ЕЛИС
 */
function fillBaseDataTable(data) {
    const tbody = document.querySelector('#protocolBaseDataTable tbody');
    tbody.innerHTML = '';

    // Определяем поля для отображения в порядке
    const fields = [
        { key: 'protocolNumber', label: 'Номер протокола' },
        { key: 'protocolDate', label: 'Дата протокола', format: formatDateValue },
        { key: 'labName', label: 'Лаборатория' },
        { key: 'labInfo.accreditationNumber', label: 'Номер аккредитации' },
        { key: 'labInfo.ownerName', label: 'Организация' },
        { key: 'pointDeliveryName', label: 'Точка поставки' },
        { key: 'samplingLocation', label: 'Место отбора пробы' },
        { key: 'startPeriodTime', label: 'Начало периода отбора', format: formatDateTimeValue },
        { key: 'endPeriodTime', label: 'Окончание периода отбора', format: formatDateTimeValue },
        { key: 'signers.laboratory.iof', label: 'Представитель лаборатории (ИОФ)' },
        { key: 'signers.laboratory.post', label: 'Должность представителя' },
        { key: 'signers.laboratory.company', label: 'Организация представителя' }
    ];

    fields.forEach(field => {
        const value = getNestedValue(data, field.key);
        if (value !== undefined && value !== null && value !== '') {
            const tr = document.createElement('tr');
            const formattedValue = field.format ? field.format(value) : escapeHtml(value);
            tr.innerHTML = `<td>${escapeHtml(field.label)}</td><td>${formattedValue}</td>`;
            tbody.appendChild(tr);
        }
    });

    // Если таблица пустая, показываем сообщение
    if (tbody.children.length === 0) {
        const tr = document.createElement('tr');
        tr.innerHTML = '<td colspan="2" class="protocol-empty-message">Нет данных</td>';
        tbody.appendChild(tr);
    }
}

/**
 * Заполнение таблицы показателей качества
 * @param {Object} data - данные протокола ЕЛИС
 */
function fillParamsTable(data) {
    const tbody = document.querySelector('#protocolParamsTable tbody');
    tbody.innerHTML = '';

    const parameters = data.parameters;
    if (!parameters || Object.keys(parameters).length === 0) {
        const tr = document.createElement('tr');
        tr.innerHTML = '<td colspan="5" class="protocol-empty-message">Нет показателей качества</td>';
        tbody.appendChild(tr);
        return;
    }

    // Перебираем параметры
    Object.entries(parameters).forEach(([name, param]) => {
        const tr = document.createElement('tr');

        // Формируем строку документов
        let docInfo = '';
        if (param.documentNumber) {
            docInfo = escapeHtml(param.documentNumber);
            if (param.documentDate) {
                docInfo += ' от ' + formatDateValue(param.documentDate);
            }
            if (param.documentType) {
                docInfo = escapeHtml(param.documentType) + ' ' + docInfo;
            }
        }

        // Форматируем числовое значение
        const numValue = param.value !== undefined && param.value !== null
            ? escapeHtml(String(param.value).replace('.', ','))
            : '';

        tr.innerHTML = `
            <td>${escapeHtml(name)}</td>
            <td>${escapeHtml(param.testMethodName || '')}</td>
            <td>${docInfo}</td>
            <td>${numValue}</td>
            <td>${escapeHtml(param.valueString || '')}</td>
        `;
        tbody.appendChild(tr);
    });
}

/**
 * Получение вложенного значения по ключу
 * @param {Object} obj - объект для поиска
 * @param {string} key - ключ в формате "parent.child.value"
 * @returns {*} найденное значение или undefined
 */
function getNestedValue(obj, key) {
    return key.split('.').reduce((o, k) => (o && o[k] !== undefined) ? o[k] : undefined, obj);
}

/**
 * Форматирование даты (ISO -> dd.mm.yyyy)
 * @param {string} isoDate - дата в формате ISO
 * @returns {string} отформатированная дата
 */
function formatDateValue(isoDate) {
    if (!isoDate) return '';
    try {
        const date = new Date(isoDate);
        if (isNaN(date.getTime())) return escapeHtml(isoDate);
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        return `${day}.${month}.${year}`;
    } catch {
        return escapeHtml(isoDate);
    }
}

/**
 * Форматирование даты и времени (ISO -> dd.mm.yyyy HH:MM)
 * @param {string} isoDateTime - дата и время в формате ISO
 * @returns {string} отформатированная дата и время
 */
function formatDateTimeValue(isoDateTime) {
    if (!isoDateTime) return '';
    try {
        const date = new Date(isoDateTime);
        if (isNaN(date.getTime())) return escapeHtml(isoDateTime);
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${day}.${month}.${year} ${hours}:${minutes}`;
    } catch {
        return escapeHtml(isoDateTime);
    }
}

/**
 * Экранирование HTML для безопасного отображения
 * @param {*} text - текст для экранирования
 * @returns {string} экранированный текст
 */
function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    const div = document.createElement('div');
    div.textContent = String(text);
    return div.innerHTML;
}

/**
 * Закрыть окно просмотра протокола и вернуться к списку
 */
function closeProtocolView() {
    logTrace('Закрытие окна просмотра протокола ЕЛИС');
    currentProtocolData = null;
    $('#elisProtocolView').modal('hide');
    setTimeout(function() {
        $('#elis').modal('show');
    }, 300);
}
