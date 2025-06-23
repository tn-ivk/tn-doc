let appDictionaries;
let qpCfgsDictionaries
let dictFetchOptions;
let hashCodeLoadedCodeDict;
let hashCodeLoadedQpConfigs;
let cachedInvalidChars = null; // Кэш для недопустимых символов
let lastDeviceId = null; // ID последнего устройства

/*
    Инициализация компонента "Редактора справочника"
*/
function InitDirEditorComponent() {

    dictFetchOptions = {
        getUrlDir: '/direditor/getdir',
        setUrlDir: '/direditor/setdir',
        getQpCfg: '/direditor/getqpconfigs',
        setQpCfg: '/direditor/setqpconfigs',
        getMethod: 'GET',
        setMethod: 'POST'
    }
    /*init dir editor component script*/
    _addTabsSelectorHandler();
    _loadAppDictionaries();
    _loadQPConfigsDictionaries();
    _renderAppDictionaries();
    _renderQpConfigs();
    _addSaveButtonHandler();
    _addCloseButtonWindowHandler();
    _initTooltips();
    /*end*/
}

/**
 *  Рендеринг таблицы с "Персонал"
 * */
function _renderAppDictionaries() {
    _addDitctionariesHandler();
    _renderUserGroupsRowTable();
    _renderAndAddHandlerLicencesTable();
    _renderAndAddHandlerUserTable();
    _addLicHandler();
    _addUserHandler();
}


/*
    Загрузка словарей приложения. Словари поступают в формате json.
    Сами словари хранятся в поле 'dirJsonRaw' (datas['dirJsonRawS']).
    Словари сохраняются в объекте 'appDictionaries'.
    В случае появления ошибки 'appDictionaries' инициализруется пустым объектом
    ,а в консоль пишется ошибка
     Дополнительно словари записываются в локальное хранилище
*/
function _loadAppDictionaries() {
    $.ajax({
        async: false, url: dictFetchOptions.getUrlDir, type: dictFetchOptions.getMethod, success: function (data) {
            appDictionaries = JSON.parse(data['dirJsonRaw'])
            hashCodeLoadedCodeDict = GetObjectHashCode(appDictionaries)
        }, error: function (xhr, ajaxOptions, thrownError) {
            appDictionaries = {};
            hashCodeLoadedCodeDict = GetObjectHashCode(appDictionaries)
        }
    })
}

/*
* Добавление обработчика событий для словарей
*/
function _addDitctionariesHandler() {
    document.querySelector('#dictionaries-list').addEventListener('click', function (e) {
        let element = e.target;
        if (element.classList.contains('active')) return;
        let elements = document.querySelectorAll('#dictionaries-list>.list-group-item')
        for (let item of elements) {
            if (item.classList.contains('active')) item.classList.remove('active');
        }
        element.classList.add('active');
        document.querySelectorAll('.dir-item').forEach(function (item) {
            if (!item.classList.contains('d-none')) item.classList.add('d-none')
        });
        let elementId = element.dataset.type;
        if (elementId) {
            document.querySelector(elementId).classList.remove('d-none')
        }
    });
}

/*
* Отрисовка таблицы для групп пользователей
*/
function _renderUserGroupsRowTable() {
    let table = document.querySelector('.user-group-table');
    for (let userGroup of appDictionaries['UsersGroup']) {
        let row = document.createElement('tr');
        row.classList.add('data-row')
        let nameTD = document.createElement('td');
        nameTD.innerText = userGroup['Name'].toString();
        _addCellStyle(nameTD)
        row.appendChild(nameTD);

        table.append(row)
    }
}

/*
    Отрисовка таблицы для довереностей.
    Дополнительно добавляются обработчики на кнопки
*/
function _renderAndAddHandlerLicencesTable() {
    let table = document.querySelector('.licences-table');
    for (let licences of appDictionaries['Licenses']) {
        let row = document.createElement('tr')

        row.classList.add('data-row')
        row.dataset.id = licences['Id'];

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(licences['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        _addCellStyle(usedCell);
        row.appendChild(usedCell);

        let numberCell = document.createElement('td');
        numberCell.innerText = licences['LicensesNumber'];
        _addCellStyle(numberCell);
        row.appendChild(numberCell);

        let dateCell = document.createElement('td');
        dateCell.innerText = licences['LicensesDate'];
        _addCellStyle(dateCell);
        row.appendChild(dateCell);

        let actionCell = document.createElement('td');
        let editDivElement = document.createElement('div');
        editDivElement.appendChild(_createEditLicensesButton('fa-lock', 'btn-outline-primary', 'me-1'));
        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(_createDeleteLicenseBtn('fa-trash', 'btn-outline-danger', ''));
        actionCell.appendChild(editDivElement);
        actionCell.appendChild(deleteDivElement);
        row.appendChild(actionCell);
        table.append(row)
    }

}

/*
    Отрисовка таблицы пользователей.
*/
function _renderAndAddHandlerUserTable() {
    let table = document.querySelector('.users-table');
    let usersGroups = appDictionaries['UsersGroup'];
    let licences = appDictionaries['Licenses'];
    for (let user of appDictionaries['Users']) {

        let row = document.createElement('tr')
        row.classList.add('data-row')
        row.dataset.id = user['Id'];

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(user['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        _addCellStyle(usedCell);
        row.appendChild(usedCell);

        let groupNameCell = document.createElement('td');
        groupNameCell.innerText = usersGroups.filter(group => group['Id'] === user['IdGroup'])[0]['Name'];
        _addCellStyle(groupNameCell);
        row.appendChild(groupNameCell);

        let surnameCell = document.createElement('td');
        surnameCell.innerText = user.F;
        _addCellStyle(surnameCell);
        row.appendChild(surnameCell);

        let nameCell = document.createElement('td');
        nameCell.innerText = user.I;
        _addCellStyle(nameCell);
        row.appendChild(nameCell);

        let patronymicCell = document.createElement('td');
        patronymicCell.innerText = user.O;
        _addCellStyle(patronymicCell);
        row.appendChild(patronymicCell);

        let organizationCell = document.createElement('td');
        organizationCell.innerText = user['Factory'];
        _addCellStyle(organizationCell);
        row.appendChild(organizationCell);

        let postCell = document.createElement('td');
        postCell.innerText = user['Post'];
        _addCellStyle(postCell);
        row.appendChild(postCell);

        let licCell = document.createElement('td');
        _addCellStyle(licCell);

        let licItem = licences.filter(lic => lic['Id'] === user['LicId'])[0];
        if (licItem) {
            licCell.innerText = `${licItem['LicensesNumber']}`;
            licCell.dataset.licId = licItem['Id'];
        } else {
            licCell.innerText = 'Доверенность не выбрана';
            licCell.dataset.licId = 0;
        }
        row.appendChild(licCell);
        
        let usedSepSquare = document.createElement('i')
        usedSepSquare.classList.add('fa');
        usedSepSquare.classList.add(user['UseFullNameSeparator'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSepSquare.ariaHidden = true;
        let sepCell = document.createElement('td');
        sepCell.appendChild(usedSepSquare);
        _addCellStyle(sepCell);
        row.appendChild(sepCell);

        let usedWhiteSpaceSquare = document.createElement('i')
        usedWhiteSpaceSquare.classList.add('fa');
        usedWhiteSpaceSquare.classList.add(user['UseFullNameWhiteSpace'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedWhiteSpaceSquare.ariaHidden = true;
        let whiteSpaceCell = document.createElement('td');
        whiteSpaceCell.appendChild(usedWhiteSpaceSquare);
        _addCellStyle(whiteSpaceCell);
        row.appendChild(whiteSpaceCell);

        let usedShortFormSquare = document.createElement('i')
        usedShortFormSquare.classList.add('fa');
        usedShortFormSquare.classList.add(user['UseShortFullNameForm'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedShortFormSquare.ariaHidden = true;
        let shortFormCell = document.createElement('td');
        shortFormCell.appendChild(usedShortFormSquare);
        _addCellStyle(shortFormCell);
        row.appendChild(shortFormCell);

        let editDivElement = document.createElement('div');
        editDivElement.appendChild(_createEditUsersButton('fa-lock', 'btn-outline-primary', 'me-1'));

        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(_createDeleteUserBtn('fa-trash', 'btn-outline-danger', ''));

        let actionCell = document.createElement('td');
        actionCell.classList.add('action-buttons-cell');
        editDivElement.firstChild.classList.add('action-btn');
        deleteDivElement.firstChild.classList.add('action-btn');
        actionCell.appendChild(editDivElement);
        actionCell.appendChild(deleteDivElement);
        row.appendChild(actionCell);
        table.append(row)
    }
}

/*
    Центрирование текста внутри компонента
    @param cell - компонент  внутри которого центрируем текст (ячейка таблицы)
*/
function _addCellStyle(cell) {
    cell.classList.add('align-middle')
}

/*
    Создание кнопки удаления пользователя
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createDeleteUserBtn(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.classList.add('delete-btn');
    btn.addEventListener('click', function (e) {
        _deleteSelectedRowHandler(e, 'Users');
    });
    return btn
}

/*
    Создание кнопки удаление доверенности
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createDeleteLicenseBtn(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.classList.add('delete-btn');
    btn.addEventListener('click', function (e) {
        _cleatUserLicId(e)
        _deleteSelectedRowHandler(e, 'Licenses');
        _clearRowTable('.users-table')
        _renderAndAddHandlerUserTable();
    });
    return btn
}

/*
    Очистка пользователей при удаление  доверености. У пользователь у которым была выдана
    удаляемая довереность, скидывается
    @param e - событие удаления
*/
function _cleatUserLicId(e) {
    if (!e.target.classList.contains('delete-btn')) return;
    let rowItem = e.target.closest('tr');
    let licId = Number(rowItem.dataset.id);
    if (!licId) return;
    for (let usr of appDictionaries['Users'].filter(user => user['LicId'] === licId)) {
        usr['LicId'] = 0;
    }
}

/*
    Удаление строки в таблицы
    @param e - событие удаления
    @param arrayName - имя массива для удаления
*/
function _deleteSelectedRowHandler(e, arrayName) {
    let target = e.target;
    // Если клик был по иконке, получаем родительскую кнопку
    if (target.tagName === 'I') {
        target = target.closest('button');
    }
    if (!target || !target.classList.contains('delete-btn')) return;
    
    let rowItem = target.closest('tr');
    let itemId = Number(rowItem.dataset.id);
    if (!itemId) return;
    appDictionaries[arrayName] = appDictionaries[arrayName].filter(function (item) {
        return item['Id'] !== itemId;
    })
    rowItem.remove();
}

/*
    Создание кнопки редактирования для доверенности
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createEditLicensesButton(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.dataset.mode = 'stable';
    btn.classList.add('edit-licences-btn');
    btn.addEventListener('click', function (e) {
        let itemBtn;
        if (e.target.tagName === 'I') {
            itemBtn = e.target.closest('button')
        } else {
            itemBtn = e.target;
        }
        let rowItem = itemBtn.closest('tr')
        let itemId = Number(rowItem.dataset.id);
        if (!itemId) return;
        _editSelectedLicences(itemBtn, rowItem, itemId)
    });
    return btn
}

/*
    Создание кнопки редактирования для пользователя
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createEditUsersButton(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.dataset.mode = 'stable';
    btn.classList.add('edit-user-btn');
    btn.addEventListener('click', function (e) {
        let itemBtn;
        if (e.target.tagName === 'I') {
            itemBtn = e.target.closest('button')
        } else {
            itemBtn = e.target;
        }
        let rowItem = itemBtn.closest('tr')
        let itemId = Number(rowItem.dataset.id);
        if (!itemId) return;
        _editSelectedUser(itemBtn, rowItem, itemId)
    });
    return btn
}

/*
    Создание кнопки отмены редактирования
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createCancelEditButton(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.classList.add('cancel-edit-btn');
    btn.addEventListener('click', function (e) {
        let itemBtn;
        if (e.target.tagName === 'I') {
            itemBtn = e.target.closest('button')
        } else {
            itemBtn = e.target;
        }
        let rowItem = itemBtn.closest('tr')
        let itemId = Number(rowItem.dataset.id);
        if (!itemId) return;
        
        // Определяем тип таблицы и вызываем соответствующую функцию отмены
        if (rowItem.closest('.users-table')) {
            _cancelEditUser(rowItem, itemId);
        } else if (rowItem.closest('.licences-table')) {
            _cancelEditLicence(rowItem, itemId);
        } else if (rowItem.closest('.qp-method-table')) {
            _cancelEditQpMethod(rowItem, itemId);
        }
    });
    return btn
}

/*
    Отмена редактирования пользователя
    @param rowItem - редактируемая строка
    @param itemId - id пользователя
*/
function _cancelEditUser(rowItem, itemId) {
    // Проверяем, является ли строка новой (в режиме инициализации)
    if (rowItem.dataset.isInit === 'true') {
        // Удаляем пользователя из массива
        appDictionaries['Users'] = appDictionaries['Users'].filter(user => user.Id !== itemId);
        
        // Включаем все элементы
        RemoveClassToElement('.table-bottom-menu', 'disabled-item');
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        
        // Включаем другие строки
        _enableOtherTableRows(itemId, 'users-table');
        
        // Удаляем строку из таблицы
        rowItem.remove();
        return;
    }

    let rowMap = {
        0: 'bool', 1: 'combobox-ug', 2: 'text', 3: 'text', 4: 'text', 5: 'text', 6: 'text', 7: 'combobox-lic', 8: 'bool', 9: 'bool', 10: 'bool', 11: 'ignore'
    };
    
    // Восстанавливаем предыдущее состояние
    if (rowItem.dataset.previousState) {
        const previousState = JSON.parse(rowItem.dataset.previousState);
        _restoreRowState(rowItem, previousState);
        delete rowItem.dataset.previousState;
    }
    
    // Включаем все элементы
    RemoveClassToElement('.table-bottom-menu', 'disabled-item');
    RemoveClassToElement('#dictionaries-list', 'disabled-item');
    RemoveClassToElement('.save-btn', 'disabled-item');
    RemoveClassToElement('.close', 'disabled-item');
    RemoveClassToElement('.modal-header', 'disabled-item');
    
    // Включаем другие строки
    _enableOtherTableRows(itemId, 'users-table');
    
    // Меняем кнопки обратно
    let actionCell = rowItem.querySelector('td:last-child');
    let editDiv = actionCell.querySelector('div:first-child');
    let deleteDiv = actionCell.querySelector('div:last-child');
    
    // Удаляем текущие кнопки
    editDiv.innerHTML = '';
    deleteDiv.innerHTML = '';
    
    // Добавляем кнопки просмотра
    editDiv.appendChild(_createEditUsersButton('fa-lock', 'btn-outline-primary', 'me-1'));
    deleteDiv.appendChild(_createDeleteUserBtn('fa-trash', 'btn-outline-danger', ''));
}

/*
    Редактирование выбранной доверенности в таблице.
    @param itemBtn - кнопка по которой нажали. Кнопка должна находиться в редактируемой строке  таблице.
    @param rowItem - редактируемая строка в таблице.
    @param itemId - id объекта в массиве.
*/
function _editSelectedUser(itemBtn, rowItem, itemId) {
    let rowMap = {
        0: 'bool', 1: 'combobox-ug', 2: 'text', 3: 'text', 4: 'text', 5: 'text', 6: 'text', 7: 'combobox-lic', 8: 'bool', 9: 'bool', 10: 'bool', 11: 'ignore'
    }

    if (itemBtn.dataset.mode === 'stable' || itemBtn.dataset.mode === 'init') {
        // Сохраняем предыдущие значения перед редактированием
        if (itemBtn.dataset.mode === 'stable') {
            rowItem.dataset.previousState = JSON.stringify(_getCurrentRowState(rowItem));
        } else {
            // Для режима инициализации сохраняем этот факт в строке
            rowItem.dataset.isInit = 'true';
        }
        
        // Отключаем другие элементы
        AddClassToElement('#dictionaries-list', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('.close', 'disabled-item');
        AddClassToElement('.table-bottom-menu', 'disabled-item');
        AddClassToElement('.modal-header', 'disabled-item');
        
        // Отключаем другие строки
        _disableOtherTableRows(itemId, 'users-table');
        
        // Преобразуем строку в режим редактирования
        _convertStableRowToEditRow(rowItem, rowMap);
        
        // Меняем кнопки
        let actionCell = rowItem.querySelector('td:last-child');
        let editDiv = actionCell.querySelector('div:first-child');
        let deleteDiv = actionCell.querySelector('div:last-child');
        
        // Удаляем текущие кнопки
        editDiv.innerHTML = '';
        deleteDiv.innerHTML = '';
        
        // Добавляем кнопки редактирования
        let editBtn = _createEditUsersButton('fa-unlock', 'btn-outline-primary', 'me-1');
        editBtn.dataset.mode = 'edit';
        editDiv.appendChild(editBtn);
        deleteDiv.appendChild(_createCancelEditButton('fa-times', 'btn-outline-danger', ''));
        
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        if (!_validateEditRow(rowItem, rowMap)) return;
        
        // Применяем изменения
        _applyUserChanges(rowItem, itemId);
        _convertEditRowToStableRow(rowItem, rowMap, false);
        
        // Удаляем сохраненное предыдущее состояние и флаг инициализации
        delete rowItem.dataset.previousState;
        delete rowItem.dataset.isInit;
        
        // Включаем все элементы
        RemoveClassToElement('.table-bottom-menu', 'disabled-item');
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        
        // Включаем другие строки
        _enableOtherTableRows(itemId, 'users-table');
        
        // Меняем кнопки обратно
        let actionCell = rowItem.querySelector('td:last-child');
        let editDiv = actionCell.querySelector('div:first-child');
        let deleteDiv = actionCell.querySelector('div:last-child');
        
        // Удаляем текущие кнопки
        editDiv.innerHTML = '';
        deleteDiv.innerHTML = '';
        
        // Добавляем кнопки просмотра
        let editBtn = _createEditUsersButton('fa-lock', 'btn-outline-primary', 'me-1');
        editBtn.dataset.mode = 'stable';
        editDiv.appendChild(editBtn);
        deleteDiv.appendChild(_createDeleteUserBtn('fa-trash', 'btn-outline-danger', ''));
    }
}

function _getCurrentRowState(rowItem) {
    const cells = rowItem.cells;
    if (rowItem.closest('.users-table')) {
        return {
            use: cells[0].querySelector('i')?.classList.contains('fa-check-square-o') || false,
            groupName: cells[1].textContent.trim(),
            surname: cells[2].textContent.trim(),
            name: cells[3].textContent.trim(),
            patronymic: cells[4].textContent.trim(),
            factory: cells[5].textContent.trim(),
            post: cells[6].textContent.trim(),
            licId: cells[7].dataset.licId,
            useFullNameSeparator: cells[8].querySelector('i')?.classList.contains('fa-check-square-o') || false,
            useFullNameWhiteSpace: cells[9].querySelector('i')?.classList.contains('fa-check-square-o') || false,
            useShortFullNameForm: cells[10].querySelector('i')?.classList.contains('fa-check-square-o') || false
        };
    } else if (rowItem.closest('.licences-table')) {
        return {
            use: cells[0].querySelector('i')?.classList.contains('fa-check-square-o') || false,
            licensesNumber: cells[1].textContent.trim(),
            licensesDate: cells[2].textContent.trim()
        };
    }
    return null;
}

function _restoreRowState(rowItem, previousState) {
    const cells = rowItem.cells;
    
    if (rowItem.closest('.users-table')) {
        // Восстанавливаем флаг использования
        const useIcon = document.createElement('i');
        useIcon.classList.add('fa', previousState.use ? 'fa-check-square-o' : 'fa-square-o');
        useIcon.ariaHidden = true;
        cells[0].innerHTML = '';
        cells[0].appendChild(useIcon);
        
        // Восстанавливаем имя группы
        cells[1].textContent = previousState.groupName;
        
        // Восстанавливаем ФИО
        cells[2].textContent = previousState.surname;
        cells[3].textContent = previousState.name;
        cells[4].textContent = previousState.patronymic;
        
        // Восстанавливаем организацию и должность
        cells[5].textContent = previousState.factory;
        cells[6].textContent = previousState.post;
        
        // Восстанавливаем лицензию
        cells[7].textContent = previousState.licId === "0" ? "Доверенность не выбрана" : 
            appDictionaries['Licenses'].find(lic => lic.Id === Number(previousState.licId))?.LicensesNumber || "Доверенность не выбрана";
        cells[7].dataset.licId = previousState.licId;
        
        // Восстанавливаем флаги форматирования
        ['useFullNameSeparator', 'useFullNameWhiteSpace', 'useShortFullNameForm'].forEach((flag, index) => {
            const icon = document.createElement('i');
            icon.classList.add('fa', previousState[flag] ? 'fa-check-square-o' : 'fa-square-o');
            icon.ariaHidden = true;
            cells[8 + index].innerHTML = '';
            cells[8 + index].appendChild(icon);
        });
    } else if (rowItem.closest('.licences-table')) {
        // Восстанавливаем флаг использования
        const useIcon = document.createElement('i');
        useIcon.classList.add('fa', previousState.use ? 'fa-check-square-o' : 'fa-square-o');
        useIcon.ariaHidden = true;
        cells[0].innerHTML = '';
        cells[0].appendChild(useIcon);
        
        // Восстанавливаем номер и дату доверенности
        cells[1].textContent = previousState.licensesNumber;
        cells[2].textContent = previousState.licensesDate;
    }
}

/*
    Редактирование выбранной доверенности в таблице.
    @param itemBtn - кнопка по которой нажали. Кнопка должна находиться в редактируемой строке  таблице.
    @param rowItem - редактируемая строка в таблице.
    @param itemId - id объекта в массиве.
*/
function _editSelectedLicences(itemBtn, rowItem, itemId) {
    let rowMap = {
        0: 'bool', 1: 'text', 2: 'date', 3: 'ignore'
    }
    if (itemBtn.dataset.mode === 'stable' || itemBtn.dataset.mode === 'init') {
        // Сохраняем предыдущие значения перед редактированием
        if (itemBtn.dataset.mode === 'stable') {
            rowItem.dataset.previousState = JSON.stringify(_getCurrentRowState(rowItem));
        } else {
            // Для режима инициализации сохраняем этот факт в строке
            rowItem.dataset.isInit = 'true';
        }
        
        // Отключаем другие элементы
        AddClassToElement('#dictionaries-list', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('.close', 'disabled-item');
        AddClassToElement('.table-bottom-menu', 'disabled-item');
        AddClassToElement('.modal-header', 'disabled-item');
        
        // Отключаем другие строки
        _disableOtherTableRows(itemId, 'licences-table');
        
        // Преобразуем строку в режим редактирования
        _convertStableRowToEditRow(rowItem, rowMap);
        
        // Добавляем обработчики валидации в реальном времени
        _addValidationHandlers(rowItem);
        
        // Меняем кнопки
        let actionCell = rowItem.querySelector('td:last-child');
        let editDiv = actionCell.querySelector('div:first-child');
        let deleteDiv = actionCell.querySelector('div:last-child');
        
        // Удаляем текущие кнопки
        editDiv.innerHTML = '';
        deleteDiv.innerHTML = '';
        
        // Добавляем кнопки редактирования
        let editBtn = _createEditLicensesButton('fa-unlock', 'btn-outline-primary', 'me-1');
        editBtn.dataset.mode = 'edit';
        editDiv.appendChild(editBtn);
        deleteDiv.appendChild(_createCancelEditButton('fa-times', 'btn-outline-danger', ''));
        
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        if (!_validateEditRow(rowItem, rowMap)) return;
        
        // Применяем изменения
        _applyLicenceChanges(rowItem, itemId);
        _convertEditRowToStableRow(rowItem, rowMap, false);
        
        // Удаляем сохраненное предыдущее состояние и флаг инициализации
        delete rowItem.dataset.previousState;
        delete rowItem.dataset.isInit;
        
        // Включаем все элементы
        RemoveClassToElement('.table-bottom-menu', 'disabled-item');
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        
        // Включаем другие строки
        _enableOtherTableRows(itemId, 'licences-table');
        
        // Меняем кнопки обратно
        let actionCell = rowItem.querySelector('td:last-child');
        let editDiv = actionCell.querySelector('div:first-child');
        let deleteDiv = actionCell.querySelector('div:last-child');
        
        // Удаляем текущие кнопки
        editDiv.innerHTML = '';
        deleteDiv.innerHTML = '';
        
        // Добавляем кнопки просмотра
        let editBtn = _createEditLicensesButton('fa-lock', 'btn-outline-primary', 'me-1');
        editBtn.dataset.mode = 'stable';
        editDiv.appendChild(editBtn);
        deleteDiv.appendChild(_createDeleteLicenseBtn('fa-trash', 'btn-outline-danger', ''));
        
        // Обновляем таблицу пользователей
        _clearRowTable('.users-table');
        _renderAndAddHandlerUserTable();
    }
}

/*
    Применение изменений доверенность 
    @param rowItem - отредкатированная строка
    @param itemId - id доверености
*/
function _applyLicenceChanges(rowItem, itemId) {
    if (!rowItem || !itemId) return;
    let objIndex = appDictionaries['Licenses'].findIndex(item => item.Id === itemId);
    if (objIndex < 0) return;
    let cells = rowItem.cells;
    let updatedObject = appDictionaries['Licenses'][objIndex]
    // Корректно определяем состояние чекбокса
    const useCheckbox = cells[0].querySelector('input[type="checkbox"]');
    if (useCheckbox) {
        updatedObject['Use'] = useCheckbox.checked;
    } else {
        updatedObject['Use'] = cells[0].childNodes[0].classList.contains('fa-check-square-o');
    }
    // Получаем значения из input элементов или текстовых узлов
    const numberInput = cells[1].querySelector('input');
    const dateInput = cells[2].querySelector('input');
    updatedObject['LicensesNumber'] = numberInput ? numberInput.value : cells[1].textContent;

    // Преобразуем дату в формат DD.MM.YYYY
    if (dateInput && dateInput.value) {
        const [yyyy, mm, dd] = dateInput.value.split('-');
        updatedObject['LicensesDate'] = `${dd}.${mm}.${yyyy}`;
    } else {
        updatedObject['LicensesDate'] = cells[2].textContent;
    }
}

/*
    Применение изменений пользовательского справочника 
    @param rowItem - отредкатированная строка
    @param itemId - id доверености
*/
function _applyUserChanges(rowItem, itemId) {
    if (!rowItem || !itemId) return;
    let objIndex = appDictionaries['Users'].findIndex(item => item.Id === itemId);
    if (objIndex < 0) return;
    let cells = rowItem.cells;
    let updatedObject = appDictionaries['Users'][objIndex];
    // Корректно определяем состояние чекбокса
    const useCheckbox = cells[0].querySelector('input[type="checkbox"]');
    if (useCheckbox) {
        updatedObject['Use'] = useCheckbox.checked;
    } else {
        const useIcon = cells[0].querySelector('i');
        updatedObject['Use'] = useIcon ? useIcon.classList.contains('fa-check-square-o') : false;
    }
    // Обновляем ID группы
    const groupInput = cells[1].querySelector('select');
    if (groupInput) {
        updatedObject['IdGroup'] = Number(groupInput.value);
    } else {
        const groupName = cells[1].textContent.trim();
        const group = appDictionaries['UsersGroup'].find(item => item['Name'] === groupName);
        updatedObject['IdGroup'] = group ? group['Id'] : 1;
    }
    
    // Обновляем ФИО
    const surnameInput = cells[2].querySelector('input');
    updatedObject['F'] = surnameInput ? surnameInput.value : cells[2].textContent.trim();
    
    const nameInput = cells[3].querySelector('input');
    updatedObject['I'] = nameInput ? nameInput.value : cells[3].textContent.trim();
    
    const patronymicInput = cells[4].querySelector('input');
    updatedObject['O'] = patronymicInput ? patronymicInput.value : cells[4].textContent.trim();
    
    // Обновляем организацию и должность
    const factoryInput = cells[5].querySelector('input');
    updatedObject['Factory'] = factoryInput ? factoryInput.value : cells[5].textContent.trim();
    
    const postInput = cells[6].querySelector('input');
    updatedObject['Post'] = postInput ? postInput.value : cells[6].textContent.trim();
    
    // Обновляем ID лицензии
    const licSelect = cells[7].querySelector('select');
    updatedObject['LicId'] = licSelect ? Number(licSelect.value) : Number(cells[7].dataset.licId || 0);
    
    // Обновляем флаги форматирования
    const sepCheckbox = cells[8].querySelector('input[type="checkbox"]');
    if (sepCheckbox) {
        updatedObject['UseFullNameSeparator'] = sepCheckbox.checked;
    } else {
        const sepIcon = cells[8].querySelector('i');
        updatedObject['UseFullNameSeparator'] = sepIcon ? sepIcon.classList.contains('fa-check-square-o') : false;
    }

    const whiteSpaceCheckbox = cells[9].querySelector('input[type="checkbox"]');
    if (whiteSpaceCheckbox) {
        updatedObject['UseFullNameWhiteSpace'] = whiteSpaceCheckbox.checked;
    } else {
        const whiteSpaceIcon = cells[9].querySelector('i');
        updatedObject['UseFullNameWhiteSpace'] = whiteSpaceIcon ? whiteSpaceIcon.classList.contains('fa-check-square-o') : false;
    }

    const shortFormCheckbox = cells[10].querySelector('input[type="checkbox"]');
    if (shortFormCheckbox) {
        updatedObject['UseShortFullNameForm'] = shortFormCheckbox.checked;
    } else {
        const shortFormIcon = cells[10].querySelector('i');
        updatedObject['UseShortFullNameForm'] = shortFormIcon ? shortFormIcon.classList.contains('fa-check-square-o') : false;
    }
}

/*
    Валидация строки таблицы
*/
function _validateEditRow(row, rowMap) {
    let cells = row.querySelectorAll('td')
    for (let i = 0; i < cells.length; i++) {
        _validateEditCell(cells[i], rowMap[i]);
    }
    // Проверяем наличие класса invalid-cell-content у элементов ввода, а не у ячеек таблицы
    const invalidElements = row.querySelectorAll('td input.invalid-cell-content, td select.invalid-cell-content');
    return invalidElements.length === 0;
}

/*
   Валидация значение ячеек таблицы 
*/
function _validateEditCell(cell, type) {
    const input = cell.querySelector('input, select');
    if (!input) return true;
    
    let isValid = true;
    const value = input.value.trim();
    
    // Удаляем предыдущие стили и подсказки
    $(input).removeClass('invalid-cell-content');
    try {
        $(input).tooltip('destroy');
    } catch (e) {
        // Игнорируем ошибку, если tooltip еще не инициализирован
    }
    
    if (type === 'text') {
        if (value === '') {
            isValid = false;
            $(input).addClass('invalid-cell-content')
                   .attr('title', 'Поле не может быть пустым');
            initTooltip(input);
        } else if (value.length > 100) {
            isValid = false;
            $(input).addClass('invalid-cell-content')
                   .attr('title', 'Превышена максимальная длина (100 символов)');
            initTooltip(input);
        } else {
            const invalidChars = GetInvalideChars();
            for (let char of invalidChars) {
                if (value.includes(char)) {
                    isValid = false;
                    $(input).addClass('invalid-cell-content')
                           .attr('title', `Некорректный символ: ${char}`);
                    initTooltip(input);
                    break;
                }
            }
            if (isValid) {
                $(input).removeClass('invalid-cell-content')
                       .removeAttr('title');
                try {
                    $(input).tooltip('destroy');
                } catch (e) {
                    // Игнорируем ошибку
                }
            }
        }
    } else if (type === 'number') {
        if (!/^[+-]?\d*([.,])?\d+$/.test(value)) {
            isValid = false;
            $(input).addClass('invalid-cell-content')
                   .attr('title', 'Введите числовое значение');
            initTooltip(input);
        } else {
            $(input).removeClass('invalid-cell-content')
                   .removeAttr('title');
            try {
                $(input).tooltip('destroy');
            } catch (e) {
                // Игнорируем ошибку
            }
        }
    } else if (type === 'email') {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            $(input).addClass('invalid-cell-content')
                   .attr('title', 'Введите корректный email адрес');
            initTooltip(input);
        } else {
            $(input).removeClass('invalid-cell-content')
                   .removeAttr('title');
            try {
                $(input).tooltip('destroy');
            } catch (e) {
                // Игнорируем ошибку
            }
        }
    }
    
    return isValid;
}

function initTooltip(input) {
    try {
        $(input).tooltip('destroy');
    } catch (e) {
        // Игнорируем ошибку
    }
    
    $(input).tooltip({
        content: input.getAttribute('title'),
        position: { my: 'left+15 center', at: 'right center', of: input },
        classes: { 'ui-tooltip': 'tooltip-inner bg-danger' },
        show: { effect: 'fade', duration: 200 },
        hide: { effect: 'fade', duration: 200 },
        open: function(event, ui) {
            // Подсказка будет показываться только при наведении
            if (!$(this).is(':hover')) {
                $(this).tooltip('close');
            }
        }
    });
}

/*
    Добавляем обработчики для валидации в реальном времени
*/
function _addValidationHandlers(row) {
    const inputs = row.querySelectorAll('input, select');
    inputs.forEach(input => {
        // Определяем тип таблицы и соответствующую карту полей
        let rowMap;
        if (row.closest('.users-table')) {
            rowMap = {
                0: 'bool', 1: 'combobox-ug', 2: 'text', 3: 'text', 4: 'text', 
                5: 'text', 6: 'text', 7: 'combobox-lic', 8: 'bool', 
                9: 'bool', 10: 'bool', 11: 'ignore'
            };
        } else if (row.closest('.licences-table')) {
            rowMap = {
                0: 'bool', 1: 'text', 2: 'date', 3: 'ignore'
            };
        } else if (row.closest('.qp-method-table')) {
            rowMap = {
                0: 'bool', 1: 'text', 2: 'bool', 
                3: 'number', 4: 'text', 5: 'ignore'
            };
        } else {
            return; // Если таблица не распознана, выходим
        }

        // Валидация при потере фокуса
        input.addEventListener('blur', function() {
            const cell = this.closest('td');
            const cellIndex = Array.from(cell.parentElement.children).indexOf(cell);
            _validateEditCell(cell, rowMap[cellIndex]);
        });

        // Валидация при вводе (с задержкой)
        let timeout;
        input.addEventListener('input', function() {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                const cell = this.closest('td');
                const cellIndex = Array.from(cell.parentElement.children).indexOf(cell);
                _validateEditCell(cell, rowMap[cellIndex]);
            }, 500);
        });
    });
}

/*
    Конверитирование ячейки редактирования в ячейку стабильную 
    @param row - строка таблицы
    @param rowMap - карта строки
    @param isPassportTable - флаг является ли таблица паспортом качества
*/
function _convertEditRowToStableRow(row, rowMap, isPassportTable) {
    let cells = row.querySelectorAll('td');
    let usersGroupArray = appDictionaries['UsersGroup'];
    let licensesArray = appDictionaries['Licenses'];
    let qpMethodsArray;
    let qpParametersArray;
    if (isPassportTable) {
        qpMethodsArray = qpCfgsDictionaries['QpsInfo'][Number(row.closest('table').dataset.qpId)]['Methods'];
        qpParametersArray = qpCfgsDictionaries['QpsInfo'][Number(row.closest('table').dataset.qpId)]['Parameters'];
    }
    for (let i = 0; i < cells.length; i++) {
        _convertEditCellToStableCell(cells[i], rowMap[i], usersGroupArray, licensesArray, qpMethodsArray, qpParametersArray);
    }
    // Добавляем обработчики валидации
    _addValidationHandlers(row);
}

/*
    Конверитирование ячейки редактирования в ячейку стабильную 
    @param cell - ячейка таблицы
    @param type - тип данных в ячейки
    @param usersGroupArray - массив пользователей
    @param licensesArray  - массив доверенностей
    @param methodsArray - массив методов паспортов качества
    @param parametersArray = массива параметров паспортов качества
*/
function _convertEditCellToStableCell(cell, type, usersGroupArray, licensesArray, methodsArray, parametersArray) {
    if (!type || !cell) return;
    let previewNode = cell.childNodes[0];
    switch (type) {
        case 'bool':
            let image = document.createElement('i');
            image.classList.add('fa');
            image.classList.add(previewNode.checked ? 'fa-check-square-o' : 'fa-square-o')
            cell.replaceChild(image, cell.childNodes[0])
            break;
        case 'text':
            let newTextNode = document.createTextNode(previewNode.value);
            cell.replaceChild(newTextNode, cell.childNodes[0])
            break;
        case 'date':
            let date = previewNode.value ? new Date(previewNode.value) : new Date();
            let day = date.getDate();
            let dayStr = day < 10 ? `0${day}` : day.toString();
            let month = date.getMonth() + 1;
            let monthStr = month < 10 ? `0${month}` : month.toString();
            let yearStr = date.getFullYear().toString();
            let newDateText = document.createTextNode(`${dayStr}.${monthStr}.${yearStr}`);
            cell.replaceChild(newDateText, cell.childNodes[0])
            break;
        case 'combobox-ug':
            let groupId = Number(previewNode.value);
            let userGroup = usersGroupArray.filter(item => item['Id'] === groupId)[0];
            let userGroupElement = document.createTextNode(userGroup['Name']);
            cell.replaceChild(userGroupElement, cell.childNodes[0])
            break;
        case 'combobox-lic':
            let licId = Number(previewNode.value);
            let license = licensesArray.filter(item => item['Id'] === licId)[0];
            if (license) {
                let licenseElement = document.createTextNode(`${license['LicensesNumber']}`);
                cell.replaceChild(licenseElement, cell.childNodes[0])
                cell.dataset.licId = String(licId);
            } else {
                let licenseElement = document.createTextNode('Доверенность не выбрана');
                cell.replaceChild(licenseElement, cell.childNodes[0])
                cell.dataset.licId = String(licId);
            }
            break;
        case 'combobox-params':
            let paramId = Number(previewNode.value);
            let parameter = parametersArray.filter(item => item["Id"] === paramId)[0];
            if (parameter) {
                let paramNameNode = document.createTextNode(parameter['Name'])
                cell.replaceChild(paramNameNode, cell.childNodes[0])
                cell.dataset.paramId = String(paramId);
            } else {
                let paramNameNode = document.createTextNode('Параметр не выбран');
                cell.replaceChild(paramNameNode, cell.childNodes[0])
                cell.dataset.paramId = String(paramId);
            }
            break;
        case 'number':
            let newNumNode = document.createTextNode(previewNode.value.replaceAll('.', ','));
            cell.replaceChild(newNumNode, cell.childNodes[0])
            break;
        default:
            break
    }
}

/*
    Конверитирование стабильной строки в строку для редактирования
    @param row - конвертируемая строка таблицы
    @param rowMap -  карта строки
*/
function _convertStableRowToEditRow(row, rowMap) {
    let cells = row.querySelectorAll('td');
    for (let i = 0; i < cells.length; i++) {
        _convertStableCellToEditCell(cells[i], rowMap[i])
    }
    // Добавляем обработчики валидации
    _addValidationHandlers(row);
}

/*
    Конверитирование стабильной ячейки в ячейку для редактирования
    @param cell - конвертируемая ячейка таблицы
    @param type - тип ячейки
*/
function _convertStableCellToEditCell(cell, type) {
    if (!type || !cell) return;
    let newElement = document.createElement('input');
    let previewNode = cell.childNodes[0];
    switch (type) {
        case 'bool':
            let innerImage = cell.querySelector('i');
            if (!innerImage) return;
            newElement.type = 'checkbox';
            newElement.checked = innerImage.classList.contains('fa-check-square-o');
            cell.replaceChild(newElement, cell.childNodes[0])
            break;
        case 'text':
            let prText = cell.innerText;
            newElement.type = 'text';
            newElement.value = prText ? prText : '';
            if (previewNode) cell.replaceChild(newElement, previewNode)
            else cell.append(newElement);
            break;
        case 'date':
            let prDate = cell.innerText ? moment(cell.innerText, 'DD.MM.YYYY').toDate() : new Date();
            newElement.type = 'date';
            newElement.value = moment(prDate).format('YYYY-MM-DD');
            if (previewNode) cell.replaceChild(newElement, previewNode)
            else cell.append(newElement);
            break;
        case 'combobox-ug':
            let cbElement = document.createElement('select');
            let counter = 0;
            for (let i = 0; i < appDictionaries['UsersGroup'].length; i++) {
                let opt = document.createElement('option');
                opt.setAttribute('value', appDictionaries['UsersGroup'][i]['Id']);
                opt.append(appDictionaries['UsersGroup'][i]['Name']);
                cbElement.append(opt);
                if (opt.textContent === previewNode.textContent) {
                    cbElement.selectedIndex = counter;
                }
                counter++;
            }
            if (previewNode) cell.replaceChild(cbElement, previewNode)
            else cell.append(cbElement);
            break;
        case 'combobox-lic':
            let cbElementLic = document.createElement('select');
            let licenses = appDictionaries['Licenses'];
            let counterLic = 0;

            let optDefault = document.createElement('option');
            optDefault.setAttribute('value', "0");
            optDefault.append(`Доверенность не выбрана`);
            cbElementLic.append(optDefault);
            for (let i = 0; i < licenses.length; i++) {
                let opt = document.createElement('option');
                opt.setAttribute('value', licenses[i]['Id']);
                opt.append(`${licenses[i]['LicensesNumber']} - ${licenses[i]['LicensesDate']}`);
                cbElementLic.append(opt);
                counterLic++;
                if (Number(cell.dataset.licId) === licenses[i]['Id']) {
                    cbElementLic.selectedIndex = counterLic;
                }

            }
            if (previewNode) cell.replaceChild(cbElementLic, previewNode)
            else cell.append(cbElementLic);
            break;
        case 'combobox-params':
            let qpId = Number(cell.closest('table').dataset.qpId);
            let params = qpCfgsDictionaries['QpsInfo'][qpId]['Parameters'];
            let counterParams = 1;
            let cbEl = document.createElement('select');
            let optDef = document.createElement('option');
            optDef.setAttribute('value', "0");
            optDef.append(`Параметр не выбран`);
            cbEl.appendChild(optDef);
            for (let param of params) {
                let opt = document.createElement('option');
                opt.setAttribute('value', param['Id']);
                opt.append(param['Name']);
                cbEl.appendChild(opt);
                if (Number(cell.dataset.paramId) === param['Id']) {
                    cbEl.selectedIndex = counterParams;
                }
                counterParams++;
            }
            for (let item of cell.childNodes) {
                item.remove();
            }

            for (let br of cell.querySelectorAll('br')) br.remove();
            cell.appendChild(cbEl);
            break;
        case 'number':
            let prNumber = cell.innerText;
            newElement.type = 'number';
            newElement.value = prNumber ? Number.parseFloat(prNumber.replaceAll(',', '.')) : '0.0';
            if (previewNode) {
                cell.replaceChild(newElement, previewNode)
            } else cell.append(newElement);
            break;
        default:
            break
    }
}

/*
    Отключение  других строк в таблицах
    @param ignoredItemId - ид строки игнорирования
    @param tableClass - класс таблицы в которой находится игнорируемая строка
*/
function _disableOtherTableRows(ignoredItemId, tableClass) {
    for (let t of document.querySelectorAll('.dir-item >.table-container>.table-content>.table')) {
        if (!t.classList.contains(tableClass)) {
            t.classList.add('disabled-item')
            continue;
        }
        t.querySelectorAll('tr.data-row').forEach(row => {
            if (Number(row.dataset.id) === ignoredItemId) return;
            row.classList.add('disabled-item');
        });

    }
}

/*
    Включение других строк
    @param ignoredItemId - ид строки игнорирования
    @param tableClass - класс таблицы в которой находится игнорируемая строка
*/
function _enableOtherTableRows(ignoredItemId, tableClass) {
    for (let t of document.querySelectorAll('.dir-item >.table-container>.table-content>.table')) {

        if (!t.classList.contains(tableClass)) {
            t.classList.remove('disabled-item')
            continue;
        }
        t.querySelectorAll('tr.data-row').forEach(row => {
            if (Number(row.dataset.id) === ignoredItemId) return;
            row.classList.remove('disabled-item');
        });

    }
}

/*
    Добавление обработчика добавления довереностей
*/
function _addLicHandler() {
    document.querySelector('.add-lic-btn').addEventListener('click', function (e) {
        let item = e.target;
        if (e.target.tagName === "I" || e.target.tagName === "LABEL") {
            item = e.target.closest('button');
        }
        let maxId = appDictionaries['Licenses'].length !== 0 ? appDictionaries['Licenses'].reduce((aid, cid) => {
            return aid > cid ? aid : cid;
        })['Id'] : 0;
        appDictionaries['Licenses'].push({
            Id: maxId + 1, Use: false, LicensesDate: "", LicensesNumber: ""
        })
        _clearRowTable('.licences-table')
        _renderAndAddHandlerLicencesTable();
        _scrollToBottomTable(' #licences >.table-container> .table-content');

        let lastRow = document.querySelector('.licences-table').lastChild;
        let itemId = Number(lastRow.dataset.id);
        let itemBtn = lastRow.querySelector('.edit-licences-btn');
        if (!itemId || !itemBtn || !lastRow) return;
        
        // Устанавливаем режим инициализации для новой строки
        itemBtn.dataset.mode = 'init';
        _editSelectedLicences(itemBtn, lastRow, itemId);
    });
}

/*
    Добавление обработчика добавления пользователя
*/
function _addUserHandler() {
    document.querySelector('.add-user-btn').addEventListener('click', function () {
        let maxId = appDictionaries['Users'].length !== 0 ? appDictionaries['Users'].reduce((aid, cid) => {
            return aid > cid ? aid : cid;
        })['Id'] : 0;
        appDictionaries['Users'].push({
            Use: false,
            Id: maxId + 1, 
            IdGroup: 1,
            F: "",
            I: "", 
            O: "", 
            Factory: "", 
            Post: "",
            UseFullNameSeparator:true,
            UseFullNameWhiteSpace:true,
            UseShortFullNameForm:true,
        })
        _clearRowTable('.users-table')
        _renderAndAddHandlerUserTable();
        _scrollToBottomTable(' #users >.table-container> .table-content');

        let lastRow = document.querySelector('.users-table').lastChild;
        let itemId = Number(lastRow.dataset.id);
        let itemBtn = lastRow.querySelector('.edit-user-btn');
        if (!itemId || !itemBtn || !lastRow) return;
        
        // Устанавливаем режим инициализации для новой строки
        itemBtn.dataset.mode = 'init';
        _editSelectedUser(itemBtn, lastRow, itemId);
    });
}

/**
 *  Детектирования изменений конфигурации проекта
 * @param {object} appDictionaries - словарь конфигурации;
 * @param {object} qpCfgsDictionaries  - конфигурация параметров паспортов качества;
 * @param {number} oldHashCodeAppDict -  последний известный хэш код конфигурации словаря;
 * @param {number} oldHashCodeQpsConfig - последний известный хэш код конфигурации параметров паспортов;
 * @return {boolean} - Возвращает true, если конфигурации были изменены с последнего момента сохранения/загрузки, в других случаях false;
 * @private
 * */
function _detectConfigsChanges(appDictionaries, qpCfgsDictionaries, oldHashCodeAppDict
    , oldHashCodeQpsConfig) {
    let curHashCodeAppDict = GetObjectHashCode(appDictionaries);
    let curHashCodeQpsConfig = GetObjectHashCode(qpCfgsDictionaries);
    return (curHashCodeAppDict !== oldHashCodeAppDict) || (curHashCodeQpsConfig !== oldHashCodeQpsConfig);
}

/**
 * Проверка намерения сохраниться. Выскакиевает окно с подтверждением.
 * @return {Promise<boolean>}
 * @desc Если пользователь ответил отрицательно, то вернется false и таблица перередерится со старыми значениями.
 * @desc Если пользователь ответил утвердительно, то  вернется truе и таблица сохранится измененые значения.
 * @private
 */
async function _checkIntentSaving() {
    if (_detectConfigsChanges(appDictionaries, qpCfgsDictionaries, hashCodeLoadedCodeDict, hashCodeLoadedQpConfigs)) {
        let needSaveChanges = await confirm('Cохранить изменения?', {
            'title': 'Сохранение', 'yesBtnName': 'Да', 'noBtnName': 'Нет','iconClass': "fa fa-exclamation-circle text-primary"
        });
        if (!needSaveChanges) {
            _loadAppDictionaries();
            _loadQPConfigsDictionaries();
            _reRenderUserAndLicTable();
            _clearQpsConfig();
            _renderQpConfigs();
            return false;
        }
        return true;

    } else {
        return false;
    }
}

/**
 *  Проверка намерения закрыть окно. Выскакиевает окно с подтверждением.
 * @return {Promise<boolean>}
 * @desc Если пользователь ответил отрицательно, то вернется false и таблица перередерится со старыми значениями.
 * @desc Если пользователь ответил утвердительно, то  вернется truе и таблица сохранится измененые значения.
 * @private
 */
async function _checkIntentClose() {
    if (_detectConfigsChanges(appDictionaries, qpCfgsDictionaries, hashCodeLoadedCodeDict, hashCodeLoadedQpConfigs)) {
        let needSave = await confirm('Хотите сохранить изменения?', {
            'title': 'Изменения не сохранены', 'yesBtnName': 'Сохранить', 'noBtnName': 'Не сохранять',
            'iconClass': "fa fa-exclamation-circle text-danger"
        });

        if (!needSave) {
            _loadAppDictionaries();
            _loadQPConfigsDictionaries();
            _reRenderUserAndLicTable();
            _clearQpsConfig();
            _renderQpConfigs();
            return false;
        }
        return true;

    } else {
        return false;
    }
}


/**
 * Добавление обработчика событий закрытие модального окна справочника
 * @desc Перед закрытие происходит проверка на  необходимость сохранение данных.
 * @desc Если изменения были не сохранены, то выпадет окно с предложением сохранить.
 * @desc Если пользователь ответил отрицательно, то изменения не сохраются и окно закрывается.
 * @desc Если пользователь ответил положительно, то изменения сохраняется.
 * @desc После этого окно всегда закрывается.
 * @private
 */
function _addCloseButtonWindowHandler() {
    $('.modal-header .close-menu .close-modal-wnd-btn').on('click', async function (e) {
        if (await _checkIntentClose()) {
            _saveQpAndDict();
        }
    });
}


/**
 * Добавление обработчика события сохранения данных
 * @private
 */
function _addSaveButtonHandler() {
    document.querySelector('.modal-header > .close-menu > .save-btn')
        .addEventListener('click', async function () {
            if (!await _checkIntentSaving()) {
                return;
            }
            _disableAllElementToDirEdit();
            RemoveClassToElement('.modal-header>.close-menu >.btn > i > .fa-spin', 'd-none')
            AddClassToElement('.modal-header>.close-menu >.btn > i > .fa-flooppy', 'd-none')
            try {
                _saveQpAndDict();
            } finally {
                RemoveClassToElement('.modal-header>.close-menu> .btn > i > .fa-flooppy', 'd-none')
                AddClassToElement('.modal-header>.close-menu> .btn >  i > .fa-spin', 'd-none')
                _enableAllElementToDirEdit();
            }
        })
}

/*QP*/

/*
   Загрузка конфигураций справочников для редактирования
   методов и параметров
*/
function _loadQPConfigsDictionaries() {
    $.ajax({
        async: false, url: dictFetchOptions.getQpCfg, type: dictFetchOptions.getMethod, success: function (data) {
            qpCfgsDictionaries = JSON.parse(data['qpCfgJsonRaw']);
            hashCodeLoadedQpConfigs = GetObjectHashCode(qpCfgsDictionaries);
        }, error: function (xhr, ajaxOptions, thrownError) {
            appDictionaries = {};
            hashCodeLoadedQpConfigs = GetObjectHashCode(qpCfgsDictionaries);
        }
    })
}


/*
    Добавление обработчика для переключения табов на странице  
*/
function _addTabsSelectorHandler() {
    document.querySelector(".tabs-selector").addEventListener('click', function (e) {
        let element = e.target;
        if (element.classList.contains("active")) return;
        for (let aItem of document.querySelectorAll("a.nav-link")) {
            if (aItem.classList.contains('active')) {
                aItem.classList.remove('active');
            }
        }
        element.classList.add('active');
        for (let tab of document.querySelectorAll('.tab-item')) {
            if (!tab.classList.contains('d-none')) {
                tab.classList.add('d-none')
            }
        }
        let elementId = element.dataset.type;
        if (elementId) {
            document.querySelector(elementId).classList.remove('d-none')
        }
    });
}

/**
 * Отрисовка паспортов качества
 * @private
 */
function _renderQpConfigs() {
    let qpList = document.querySelector('#qp-list');
    let counter = 0;
    for (let qp of qpCfgsDictionaries["QpsInfo"]) {
        let liItem = document.createElement('li');
        liItem.classList.add("list-group-item");
        liItem.textContent = qp["Name"];
        liItem.dataset.target = "#qpId" + counter;
        qpList.append(liItem)
        let mainBaseDiv = _renderMainDivForQpTables(counter);
        _renderQpConfigsMethodsTable(counter, qp, mainBaseDiv)
        liItem.addEventListener("click", function (e) {
            let element = e.target;
            if (element.classList.contains('active')) return
            for (let item of document.querySelectorAll('.qp-dir-item')) {
                if (!item.classList.contains('d-none')) item.classList.add('d-none');
            }
            let elem = document.querySelector(element.dataset.target);
            if (elem) elem.classList.remove('d-none');

            for (let el of document.querySelectorAll('#qp-list>.list-group-item')) {
                if (el.classList.contains('active')) el.classList.remove('active');
            }
            element.classList.add('active');
        });
        if (counter === 0) {
            liItem.classList.add("active");
            document.querySelector(liItem.dataset.target).classList.remove('d-none');
        }
        counter += 1;
    }
}

/**
 * Сохранение словарей и паспортов качества на сервере
 * @private
 */
function _saveQpAndDict() {
    $.ajax({
        async: false,
        url: dictFetchOptions.setUrlDir,
        type: dictFetchOptions.setMethod,
        contentType: 'application/json; charset=UTF-8',
        data: JSON.stringify({
            dirJsonRaw: JSON.stringify(appDictionaries)
        }),
        success: function () {
            _reRenderUserAndLicTable();
        },
        error: function (xhr, ajaxOptions, thrownError) {
            appDictionaries = {};
        },
    });

    $.ajax({
        async: false,
        url: dictFetchOptions.setQpCfg,
        type: dictFetchOptions.setMethod,
        contentType: 'application/json; charset=UTF-8',
        data: JSON.stringify({
            qpCfgJsonRaw: JSON.stringify(qpCfgsDictionaries)
        }),
        error: function (xhr, ajaxOptions, thrownError) {
            qpCfgsDictionaries = {};

        },

    });
    hashCodeLoadedCodeDict = GetObjectHashCode(appDictionaries);
    hashCodeLoadedQpConfigs = GetObjectHashCode(qpCfgsDictionaries);
}

/*
   Рендеринг главного дива для рендеринга паспортов качества 
*/
function _renderMainDivForQpTables(id) {
    let rootDiv = document.querySelector('#qp-dir-page > div.col-9');
    let methodsDiv = document.createElement('div');
    methodsDiv.classList.add('qp-dir-item');
    methodsDiv.classList.add('d-none');
    methodsDiv.id = "qpId" + id;
    rootDiv.appendChild(methodsDiv)

    let tbContainerDiv = document.createElement('div');
    tbContainerDiv.classList.add('table-container');
    methodsDiv.appendChild(tbContainerDiv);

    let tbScrollDiv = document.createElement('div');
    tbScrollDiv.classList.add('table-content');
    tbContainerDiv.appendChild(tbScrollDiv)
    tbContainerDiv.appendChild(_addBtnToAddQp(id));
    return tbScrollDiv;
}

/*
    Добавление кнопки добавления новых методов
    @param qpId - идентификатор паспорта качества
    @returns {HTMLDivElement} - div с кнопкой
*/
function _addBtnToAddQp(qpId) {
    let btnDiv = document.createElement('div');
    btnDiv.classList.add('table-bottom-menu');

    let btn = document.createElement('button');
    btn.classList.add('btn', 'btn-primary', 'default-btn-size', 'add-qp-btn');
    btn.dataset.qpId = qpId
    btnDiv.appendChild(btn);

    btn.addEventListener('click', function (event) {
        let item = event.target;
        if (event.target.tagName === "I" || event.target.tagName === "LABEL") {
            item = event.target.closest('button');
        }
        let qpId = item.dataset.qpId;
        
        // Получаем текущий выбранный параметр
        let currentParameterId = item.dataset.currentParameterId;
        if (!currentParameterId) {
            // Если параметр не установлен, берем первый доступный
            let container = document.querySelector(`.methods-tables-container[data-qp-id="${qpId}"]`);
            let selector = container ? container.parentElement.querySelector('.parameter-select') : null;
            if (selector && selector.options.length > 0) {
                currentParameterId = parseInt(selector.value);
            } else {
                alert('Нет доступных параметров для добавления метода');
                return;
            }
        }
        
        let maxId = qpCfgsDictionaries['QpsInfo'][qpId]['Methods'].length !== 0 ? qpCfgsDictionaries['QpsInfo'][qpId]['Methods'].reduce((accumId, curId) => {
            return accumId > curId ? accumId : curId;
        }) ['Id'] : 1;
        
        // Создаем новый метод с привязкой к текущему параметру
        qpCfgsDictionaries['QpsInfo'][qpId]['Methods'].push({
            Id: maxId + 1, 
            Use: false, 
            IdParameter: parseInt(currentParameterId), 
            Name: '', 
            LimitValueActivate: false, 
            LimitValue: 0, 
            LimitValueString: ''
        });

        _clearQpsConfig();
        _renderQpConfigs();

        for (let liItem of document.querySelectorAll('#qp-list>li')) {
            if (liItem.classList.contains('active')) {
                liItem.classList.remove('active');
            }
        }

        for (let qpDirItem of document.querySelectorAll('.qp-dir-item')) {
            if (!qpDirItem.classList.contains('d-none')) {
                qpDirItem.classList.add('d-none');
            }
        }

        let li = document.querySelector('li[data-target="' + '#qpId' + item.dataset.qpId + '"]');
        li.classList.add('active');
        document.querySelector(li.dataset.target).classList.remove('d-none');
        
        // Переключаемся на таблицу нужного параметра
        _switchParameterTable(qpId, parseInt(currentParameterId));
        
        // Находим текущую видимую таблицу и последнюю строку в ней
        let visibleTable = document.querySelector(`[data-qp-id="${qpId}"] .parameter-table-wrapper:not(.d-none) table`);
        if (!visibleTable) return;
        
        let lastRow = visibleTable.lastChild;
        let itemBtn = lastRow.querySelector('.edit-methods-btn');
        if (!itemBtn || !lastRow) return;
        
        // Устанавливаем режим инициализации для новой строки
        itemBtn.dataset.mode = 'init';
        _editQpMethod(lastRow, itemBtn);
    });

    let img = document.createElement('i');
    img.classList.add('fa', 'fa-plus');
    img.ariaHidden = 'true';
    let imgDiv = document.createElement('div');
    imgDiv.classList.add('padding-one-px-top');
    imgDiv.appendChild(img);
    btn.appendChild(imgDiv);
    let lbl = document.createElement('label');
    lbl.appendChild(document.createTextNode('Добавить'));
    let lblDiv = document.createElement('div');
    lblDiv.appendChild(lbl);
    btn.appendChild(lblDiv);
    return btnDiv;
}


/*
    Очистка экрана отобрадения методов паспортов качества
*/
function _clearQpsConfig() {
    for (let child of document.querySelectorAll('#qp-list>li')) {
        child.remove();
    }
    for (let child of document.querySelectorAll('.qp-dir-item')) {
        child.remove();
    }
}

/*
* Рендеринг методов паспортов качества с группировкой по параметрам
*/
function _renderQpConfigsMethodsTable(counter, qps, baseDiv) {
    let methods = qps["Methods"];
    let parameters = qps["Parameters"];
    if (!methods || !parameters) return;

    // Создаем контейнер для навигации по параметрам
    let navigationDiv = document.createElement('div');
    navigationDiv.classList.add('parameter-navigation', 'mb-3');
    baseDiv.appendChild(navigationDiv);
    
    // Создаем выпадающий список параметров
    _renderParameterSelector(counter, qps, navigationDiv);
    
    // Группируем методы по параметрам
    let methodsByParameter = _groupMethodsByParameter(methods, parameters);
    
    // Создаем контейнер для таблиц
    let tablesContainer = document.createElement('div');
    tablesContainer.classList.add('methods-tables-container');
    tablesContainer.dataset.qpId = counter;
    baseDiv.appendChild(tablesContainer);
    
    // Создаем таблицу для каждого параметра
    for (let [parameterId, parameterMethods] of Object.entries(methodsByParameter)) {
        let parameter = parameters.find(p => p.Id === parseInt(parameterId));
        if (!parameter) continue;
        
        _createParameterMethodsTable(counter, parameter, parameterMethods, tablesContainer);
    }
    
    // Показываем первую таблицу по умолчанию
    _showFirstParameterTable(tablesContainer);
}

/*
* Группировка методов по параметрам
*/
function _groupMethodsByParameter(methods, parameters) {
    let methodsByParameter = {};
    
    for (let method of methods) {
        let parameterId = method['IdParameter'] || 0;
        if (!methodsByParameter[parameterId]) {
            methodsByParameter[parameterId] = [];
        }
        methodsByParameter[parameterId].push(method);
    }
    
    return methodsByParameter;
}

/*
* Создание таблицы методов для конкретного параметра
*/
function _createParameterMethodsTable(qpId, parameter, methods, container) {
    let tableWrapper = document.createElement('div');
    tableWrapper.classList.add('parameter-table-wrapper', 'd-none');
    tableWrapper.dataset.parameterId = parameter.Id;
    container.appendChild(tableWrapper);
    
    let methodsTable = document.createElement('table');
    methodsTable.classList.add('table', 'table-bordered', 'inner-item-center', 'qp-method-table');
    methodsTable.dataset.qpId = qpId;
    methodsTable.dataset.parameterId = parameter.Id;
    tableWrapper.appendChild(methodsTable);
    
    // Создание заголовка таблицы (без колонки "Параметр")
    let tbMethodsHead = document.createElement('thead');
    methodsTable.appendChild(tbMethodsHead);
    let hRow = document.createElement('tr');
    hRow.classList.add('table-primary');
    tbMethodsHead.appendChild(hRow);
    
    hRow.appendChild(_createTableColumnHeader("Активен"));
    hRow.appendChild(_createTableColumnHeader("Метод"));
    hRow.appendChild(_createTableColumnHeader("Контроль мин. значения"));
    hRow.appendChild(_createTableColumnHeader("Мин. значение"));
    hRow.appendChild(_createTableColumnHeader("Сообщение"));
    hRow.appendChild(_createTableColumnHeader("Действия"));
    
    // Создание строк для методов данного параметра
    for (let method of methods) {
        let row = document.createElement('tr');
        row.classList.add('data-row');
        row.dataset.id = method['Id'];
        row.dataset.parameterId = parameter.Id;
        
        // Столбец "Активен"
        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(method['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        _addCellStyle(usedTd)
        row.appendChild(usedTd);
        
        // Столбец "Метод"
        let methodName = document.createElement('td');
        methodName.innerText = method['Name'];
        _addCellStyle(methodName);
        row.appendChild(methodName);
        
        // Столбец "Контроль мин. значения"
        let limitActive = document.createElement('i')
        limitActive.classList.add('fa');
        limitActive.classList.add(method['LimitValueActivate'] === true ? 'fa-check-square-o' : 'fa-square-o');
        limitActive.ariaHidden = true;
        let limitActiveCell = document.createElement('td');
        limitActiveCell.appendChild(limitActive);
        _addCellStyle(limitActiveCell)
        row.appendChild(limitActiveCell);
        
        // Столбец "Мин. значение"
        let LimitValueCell = document.createElement('td');
        LimitValueCell.innerText = method['LimitValue'];
        _addCellStyle(LimitValueCell);
        row.appendChild(LimitValueCell);
        
        // Столбец "Сообщение"
        let LimitValueStringCell = document.createElement('td');
        LimitValueStringCell.innerText = !method['LimitValueString'] ? '-' : method['LimitValueString'];
        _addCellStyle(LimitValueStringCell);
        row.appendChild(LimitValueStringCell);
        
        // Столбец "Действия"
        let actionCell = document.createElement('td');
        actionCell.classList.add('action-buttons-cell');
        let editDivElement = document.createElement('div');
        editDivElement.appendChild(_createEditQpMethodsBtn('fa-lock', 'btn-outline-primary', 'me-1'));
        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(_createDeleteQpMethodsBtn('fa-trash', 'btn-outline-danger', ''));
        editDivElement.firstChild.classList.add('action-btn');
        deleteDivElement.firstChild.classList.add('action-btn');
        actionCell.appendChild(editDivElement);
        actionCell.appendChild(deleteDivElement);
        _addCellStyle(actionCell);
        row.appendChild(actionCell)
        methodsTable.appendChild(row);
    }
}

/*
* Создание выпадающего списка для выбора параметра
*/
function _renderParameterSelector(qpId, qps, container) {
    let selectorDiv = document.createElement('div');
    selectorDiv.classList.add('parameter-selector');
    
    let label = document.createElement('label');
    label.textContent = 'Параметр:';
    selectorDiv.appendChild(label);
    
    let select = document.createElement('select');
    select.classList.add('parameter-select');
    select.dataset.qpId = qpId;
    selectorDiv.appendChild(select);
    
    // Получаем уникальные параметры из методов
    let parameters = qps["Parameters"];
    let methods = qps["Methods"];
    let usedParameterIds = [...new Set(methods.map(m => m.IdParameter))];
    
    // Добавляем опции для каждого используемого параметра
    for (let parameterId of usedParameterIds) {
        let parameter = parameters.find(p => p.Id === parameterId);
        if (!parameter) continue;
        
        let option = document.createElement('option');
        option.value = parameterId;
        option.textContent = parameter.Name || `Параметр ${parameterId}`;
        select.appendChild(option);
    }
    
    // Обработчик изменения выбора параметра
    select.addEventListener('change', function(e) {
        _switchParameterTable(qpId, parseInt(e.target.value));
    });
    
    container.appendChild(selectorDiv);
}

/*
* Переключение между таблицами параметров
*/
function _switchParameterTable(qpId, parameterId) {
    let container = document.querySelector(`.methods-tables-container[data-qp-id="${qpId}"]`);
    if (!container) return;
    
    // Скрываем все таблицы
    let allTables = container.querySelectorAll('.parameter-table-wrapper');
    allTables.forEach(table => {
        table.classList.add('d-none');
    });
    
    // Показываем таблицу для выбранного параметра
    let selectedTable = container.querySelector(`[data-parameter-id="${parameterId}"]`);
    if (selectedTable) {
        selectedTable.classList.remove('d-none');
    }
    
    // Обновляем кнопку "Добавить" для текущего параметра
    _updateAddButtonForParameter(qpId, parameterId);
}

/*
* Показать первую таблицу по умолчанию
*/
function _showFirstParameterTable(container) {
    let firstTable = container.querySelector('.parameter-table-wrapper');
    if (firstTable) {
        firstTable.classList.remove('d-none');
        
        // Обновляем селектор
        let qpId = container.dataset.qpId;
        let parameterId = firstTable.dataset.parameterId;
        // Ищем селектор в родительском элементе контейнера
        let selector = container.parentElement.querySelector('.parameter-select');
        if (selector) {
            selector.value = parameterId;
        }
        
        // Обновляем кнопку добавления
        _updateAddButtonForParameter(qpId, parseInt(parameterId));
    }
}

/*
* Обновление кнопки "Добавить" для текущего параметра
*/
function _updateAddButtonForParameter(qpId, parameterId) {
    let addButton = document.querySelector(`.add-qp-btn[data-qp-id="${qpId}"]`);
    if (addButton) {
        addButton.dataset.currentParameterId = parameterId;
    }
}


/*
    Создание кнопки удаления методов из паспортов качества
*/
function _createDeleteQpMethodsBtn(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.classList.add('delete-btn');
    btn.addEventListener('click', function(e) {
        let row = e.target.closest('tr');
        if (!row) return;
        let table = row.closest('table');
        if (!table) return;
        let qpId = Number(table.dataset.qpId);
        qpCfgsDictionaries['QpsInfo'][qpId]['Methods'] = qpCfgsDictionaries["QpsInfo"][qpId]['Methods'].filter(function (item) {
            return item["Id"] !== Number(row.dataset.id);
        });
        row.remove();
    });
    return btn;
}

/*
    Создание кнопки редактирования методов
*/
function _createEditQpMethodsBtn(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.dataset.mode = 'stable';
    btn.classList.add('edit-methods-btn');
    
    // Обновляем иконку в зависимости от режима
    const updateIcon = () => {
        const icon = btn.querySelector('i');
        if (icon) {
            icon.className = 'fa';
            icon.classList.add(btn.dataset.mode === 'stable' ? 'fa-lock' : 'fa-unlock');
        }
    };
    
    // Обновляем иконку при изменении режима
    const observer = new MutationObserver(updateIcon);
    observer.observe(btn, { attributes: true, attributeFilter: ['data-mode'] });
    
    btn.addEventListener('click', function(e) {
        let item = e.target.tagName === 'I' ? e.target.closest('button') : e.target;
        let row = item.closest('tr');
        if (!row) return;
        _editQpMethod(row, item);
    });
    
    // Инициализируем иконку
    updateIcon();
    
    return btn;
}


/*
    Обработчик событий удаления  метода паспорта качества из таблицы.
    @param event - возникшее событие.
*/
function _deleteQpMethodsBtnHandler(event) {
    let row = event.target.closest('tr');
    if (!row) return;
    let table = row.closest('table');
    if (!table) return;
    let qpId = Number(table.dataset.qpId);
    qpCfgsDictionaries['QpsInfo'][qpId]['Methods'] = qpCfgsDictionaries["QpsInfo"][qpId]['Methods'].filter(function (item) {
        return item["Id"] !== Number(row.dataset.id);
    });
    row.remove();
}

/*
    Обработчик событий редактирования метода паспорта качества из таблицы.
    @param event - возникшее событие.
*/
function _editQpMethodBtnHandler(event) {
    let item = event.target.tagName === 'I' ? event.target.closest('button') : event.target;
    let row = item.closest('tr');
    if (!row) return;
    
    // Если это кнопка отмены, вызываем соответствующую функцию
    if (item.classList.contains('cancel-edit-btn')) {
        _cancelEditQpMethod(row, Number(row.dataset.id));
        return;
    }
    
    // В противном случае обрабатываем как обычное редактирование
    _editQpMethod(row, item);
}

/*
    Редактирование строки метода паспорта качества
    @param row - строка редактирования
    @param itemBtn - кнопка редактирования в строке
*/
function _editQpMethod(row, itemBtn) {
    if (!row || !itemBtn) return;
    
    let rowMap = {
        0: 'bool', 1: 'text', 2: 'bool', 3: 'number', 4: 'text', 5: 'ignore'
    }

    if (itemBtn.dataset.mode === 'stable' || itemBtn.dataset.mode === 'init') {
        // Сохраняем предыдущие значения перед редактированием только в режиме stable
        if (itemBtn.dataset.mode === 'stable') {
            row.dataset.previousState = JSON.stringify(_getCurrentQpMethodState(row));
        } else {
            // Для режима инициализации сохраняем этот факт в строке
            row.dataset.isInit = 'true';
        }
        
        // Отключаем другие элементы
        AddClassToElement('#qp-list', 'disabled-item');
        AddClassToElement('.modal-header', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('row', 'disabled-item');
        AddClassToElement('tr[data-id="' + Number(row.dataset.id) + '"] td button.delete-btn', 'disabled-item');
        
        // Отключаем другие строки
        _disableOtherRowsInTable(itemBtn.closest('table'), Number(row.dataset.id));
        
        // Преобразуем строку в режим редактирования
        _convertStableRowToEditRow(row, rowMap);
        
        // Добавляем обработчики валидации в реальном времени
        _addValidationHandlers(row);
        
        // Меняем кнопки
        let actionCell = row.querySelector('td:last-child');
        if (!actionCell) return;
        
        let editDiv = actionCell.querySelector('div:first-child');
        let deleteDiv = actionCell.querySelector('div:last-child');
        
        if (editDiv && deleteDiv) {
            // Удаляем текущие кнопки
            editDiv.innerHTML = '';
            deleteDiv.innerHTML = '';
            
            // Добавляем кнопки редактирования
            let editBtn = _createEditQpMethodsBtn('fa-unlock', 'btn-outline-primary', 'me-1');
            editBtn.dataset.mode = 'edit';
            editDiv.appendChild(editBtn);
            
            let cancelBtn = _createCancelEditButton('fa-times', 'btn-outline-danger', '');
            deleteDiv.appendChild(cancelBtn);
        }
        
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        if (!_validateEditRow(row, rowMap)) return;
        
        // Применяем изменения
        _applyQpMethodsChanged(row, Number(row.dataset.id), Number(itemBtn.closest('table').dataset.qpId));
        _convertEditRowToStableRow(row, rowMap, true);
        
        // Удаляем сохраненное предыдущее состояние и флаг инициализации
        delete row.dataset.previousState;
        delete row.dataset.isInit;
        
        // Включаем все элементы
        RemoveClassToElement('#qp-list', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('tr[data-id="' + Number(row.dataset.id) + '"] td button.delete-btn', 'disabled-item');
        
        // Включаем другие строки
        _enableOtherRowsInTable(itemBtn.closest('table'), Number(row.dataset.id));
        
        // Меняем кнопки обратно
        let actionCell = row.querySelector('td:last-child');
        if (!actionCell) return;
        
        let editDiv = actionCell.querySelector('div:first-child');
        let deleteDiv = actionCell.querySelector('div:last-child');
        
        if (editDiv && deleteDiv) {
            // Удаляем текущие кнопки
            editDiv.innerHTML = '';
            deleteDiv.innerHTML = '';
            
            // Добавляем кнопки просмотра
            let editBtn = _createEditQpMethodsBtn('fa-lock', 'btn-outline-primary', 'me-1');
            editBtn.dataset.mode = 'stable';
            editDiv.appendChild(editBtn);
            
            let deleteBtn = _createDeleteQpMethodsBtn('fa-trash', 'btn-outline-danger', '');
            deleteDiv.appendChild(deleteBtn);
        }
        
        itemBtn.dataset.mode = 'stable';
    }
}

/*
    Получение текущего состояния строки метода испытаний
    @param rowItem - строка таблицы
    @returns {Object} - объект с текущим состоянием
*/
function _getCurrentQpMethodState(rowItem) {
    const cells = rowItem.cells;
    return {
        use: cells[0].querySelector('i')?.classList.contains('fa-check-square-o') || false,
        name: cells[1].textContent.trim(),
        paramId: rowItem.dataset.parameterId || "0",
        limitValueActivate: cells[2].querySelector('i')?.classList.contains('fa-check-square-o') || false,
        limitValue: cells[3].textContent.trim(),
        limitValueString: cells[4].textContent.trim()
    };
}

/*
    Отмена редактирования метода испытаний
    @param rowItem - редактируемая строка
    @param itemId - id метода
*/
function _cancelEditQpMethod(rowItem, itemId) {
    // Проверяем, является ли строка новой (в режиме инициализации)
    if (rowItem.dataset.isInit === 'true') {
        let table = rowItem.closest('table');
        let qpId = Number(table.dataset.qpId);
        
        // Удаляем метод из массива
        qpCfgsDictionaries['QpsInfo'][qpId]['Methods'] = qpCfgsDictionaries['QpsInfo'][qpId]['Methods']
            .filter(method => method.Id !== itemId);
        
        // Включаем все элементы
        RemoveClassToElement('#qp-list', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item');
        
        // Включаем другие строки
        _enableOtherRowsInTable(table, itemId);
        
        // Удаляем строку из таблицы
        rowItem.remove();
        return;
    }

    let rowMap = {
        0: 'bool', 1: 'text', 2: 'bool', 3: 'number', 4: 'text', 5: 'ignore'
    };
    
    // Восстанавливаем предыдущее состояние
    if (rowItem.dataset.previousState) {
        const previousState = JSON.parse(rowItem.dataset.previousState);
        _restoreQpMethodState(rowItem, previousState);
        delete rowItem.dataset.previousState;
    }
    
    // Включаем все элементы
    RemoveClassToElement('#qp-list', 'disabled-item');
    RemoveClassToElement('.modal-header', 'disabled-item');
    RemoveClassToElement('.save-btn', 'disabled-item');
    RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item');
    
    // Включаем другие строки
    _enableOtherRowsInTable(rowItem.closest('table'), itemId);
    
    // Меняем кнопки обратно
    let actionCell = rowItem.querySelector('td:last-child');
    let editDiv = actionCell.querySelector('div:first-child');
    let deleteDiv = actionCell.querySelector('div:last-child');
    
    // Удаляем текущие кнопки
    editDiv.innerHTML = '';
    deleteDiv.innerHTML = '';
    
    // Добавляем кнопки просмотра
    editDiv.appendChild(_createEditQpMethodsBtn('fa-lock', 'btn-outline-primary', 'me-1'));
    deleteDiv.appendChild(_createDeleteQpMethodsBtn('fa-trash', 'btn-outline-danger', ''));
}

/*
    Восстановление состояния строки метода испытаний
    @param rowItem - строка таблицы
    @param previousState - предыдущее состояние
*/
function _restoreQpMethodState(rowItem, previousState) {
    const cells = rowItem.cells;
    
    // Восстанавливаем флаг использования
    const useIcon = document.createElement('i');
    useIcon.classList.add('fa', previousState.use ? 'fa-check-square-o' : 'fa-square-o');
    useIcon.ariaHidden = true;
    cells[0].innerHTML = '';
    cells[0].appendChild(useIcon);
    
    // Восстанавливаем название метода
    cells[1].textContent = previousState.name;
    
    // Параметр теперь хранится в dataset строки (колонка удалена)
    rowItem.dataset.parameterId = previousState.paramId;
    
    // Восстанавливаем флаг контроля минимального значения (теперь индекс 2)
    const limitIcon = document.createElement('i');
    limitIcon.classList.add('fa', previousState.limitValueActivate ? 'fa-check-square-o' : 'fa-square-o');
    limitIcon.ariaHidden = true;
    cells[2].innerHTML = '';
    cells[2].appendChild(limitIcon);
    
    // Восстанавливаем минимальное значение и сообщение (индексы сдвинулись)
    cells[3].textContent = previousState.limitValue;
    cells[4].textContent = previousState.limitValueString;
}

/*
    Применение изменений метода паспорта качества 
*/
function _applyQpMethodsChanged(rowItem, itemId, qpId) {
    if (!rowItem || !itemId) return;
    if (qpId === undefined) return;
    let methodIndex = qpCfgsDictionaries["QpsInfo"][qpId]["Methods"].findIndex(item => item['Id'] === itemId);
    if (methodIndex < 0) return;
    let cells = rowItem.cells;
    let updatedObject = qpCfgsDictionaries["QpsInfo"][qpId]["Methods"][methodIndex];
    // Корректно определяем состояние чекбокса
    const useCheckbox = cells[0].querySelector('input[type="checkbox"]');
    if (useCheckbox) {
        updatedObject['Use'] = useCheckbox.checked;
    } else {
        updatedObject['Use'] = cells[0].childNodes[0].classList.contains('fa-check-square-o');
    }
    // Имя метода
    const nameInput = cells[1].querySelector('input');
    updatedObject['Name'] = nameInput ? nameInput.value : cells[1].textContent;
    // Параметр берем из dataset строки (поскольку колонка "Параметр" удалена)
    if (rowItem.dataset.parameterId) {
        updatedObject['IdParameter'] = Number(rowItem.dataset.parameterId);
    }
    // Контроль мин. значения (теперь индекс 2 вместо 3)
    const limitCheckbox = cells[2].querySelector('input[type="checkbox"]');
    if (limitCheckbox) {
        updatedObject['LimitValueActivate'] = limitCheckbox.checked;
    } else {
        updatedObject['LimitValueActivate'] = cells[2].childNodes[0].classList.contains('fa-check-square-o');
    }
    // Мин. значение (теперь индекс 3 вместо 4)
    const limitValueInput = cells[3].querySelector('input');
    updatedObject['LimitValue'] = limitValueInput ? Number.parseFloat(limitValueInput.value.replaceAll(',', '.')) : Number.parseFloat(cells[3].textContent.replaceAll(',', '.'));
    // Сообщение (теперь индекс 4 вместо 5)
    const msgInput = cells[4].querySelector('input');
    let msg = msgInput ? msgInput.value : cells[4].textContent;
    if (!msg) {
        updatedObject['LimitValueString'] = '-';
    } else {
        updatedObject['LimitValueString'] = msg;
    }
}


/**********************************************/
/*************Хелперы*************************/
/**********************************************/


/*
    Создание заголовков таблицы <br/>
    Входные параметры: <br/>
    @param headerText - заголовок столбца 
*/
function _createTableColumnHeader(headerText) {
    let el = _createElementWithOptions('th', {scope: "col", textContent: headerText});
    _addCellStyle(el);
    return el;
}

/*
    Создание элемента html c инициализацией  параметров<br/>
    Входные переменные:<br/>
    @param tag - тег, создаваемого html элемента <br/>
    @param options - объекта параметров, который необходимо инициализировать при созданмие

*/
function _createElementWithOptions(tag, options) {
    return Object.assign(document.createElement(tag), options)
}


/*
* Добавление класса элементу
*/
function AddClassToElement(selector, className) {
    _modificateElementClasses(selector, className, false)
}

/*Удаление класса элемента*/
function RemoveClassToElement(selector, className) {
    _modificateElementClasses(selector, className, true)
}


/*Модификация списка классов у элемета HTML */
function _modificateElementClasses(selector, className, isRemove) {
    if (!selector || !className) return;
    let elements = document.querySelectorAll(selector);
    if (!elements) return;
    let classes = className.split(':');
    elements.forEach(el => {
        if (isRemove) {
            for (let cl of classes) {
                if (!cl) return;
                if (el.classList.contains(cl)) el.classList.remove(cl)
            }
        } else {
            elements.forEach(el => {
                for (let cl of classes) {
                    if (!cl) return;
                    if (!el.classList.contains(cl)) el.classList.add(cl)
                }
            });
        }
    });
}

/*
    Создает кнопку только с иконкой.
    Присваивает указанные классы.
    Если необходимо присвоить множество классов, то их необходимо разделять  ':'
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createWithOnlyImgButton(faClass, buttonClass, margin) {
    let img = document.createElement('i');
    img.classList.add('fa');
    if (typeof faClass === 'string') {
        if (faClass.includes(':')) {
            let faClasses = faClass.split(':');
            for (let cl of faClasses) {
                if (!cl) continue;
                img.classList.add(cl);
            }
        } else {
            img.classList.add(faClass);
        }
    }
    img.ariaHidden = true;
    img.style.fontSize = '1.5em';
    
    let btn = document.createElement('button');
    if (typeof buttonClass === 'string') {
        if (buttonClass.includes(':')) {
            let btnClasses = buttonClass.split(':');
            for (let cl of btnClasses) {
                if (!cl) continue;
                btn.classList.add(cl);
            }
        } else {
            btn.classList.add(buttonClass);
        }
    }
    btn.appendChild(img);
    btn.style.margin = margin;
    btn.style.alignSelf = 'center';
    return btn;
}

/*
    Изменение иконки на кнопке
    @param itemBtn -элемент  кнопки
    @param newClass - новый класс с иконкой
    @param oldClass - старый класс с иконкой
*/
function _changeButtonIcon(itemBtn, newClass, oldClass) {
    itemBtn.querySelector('i').classList.replace(oldClass, newClass);
}

/* 
    Очистка таблицы по селектору контрола таблицы
    @param tableSelector - селектор таблицы, которую надо чистить
*/
function _clearRowTable(tableSelector) {
    let table = document.querySelector(tableSelector);
    if (!table) return;
    let dataRows = table.querySelectorAll('tr.data-row');
    for (let index = 1; index < dataRows.length + 1; index++) table.deleteRow(1)
}

/*
    Прокрутка таблицы вниз
    @param tableSelector - селектор таблицы
*/
function _scrollToBottomTable(tableSelector) {
    if (!tableSelector) return;
    let tableController = document.querySelector(tableSelector);
    if (!tableController) return;
    tableController.scrollTo(0, tableController.scrollHeight)
}

/*
    Перерендеринг таблиц пользователей и доверенностей
*/
function _reRenderUserAndLicTable() {
    _clearRowTable('.users-table');
    _clearRowTable('.licences-table');
    _renderAndAddHandlerLicencesTable();
    _renderAndAddHandlerUserTable();
}

/*
    Отключение активности у всех элементов у компонента редактирования справочников
*/
function _disableAllElementToDirEdit() {
    AddClassToElement('.close', 'disabled-item');
    AddClassToElement('.modal-body', 'disabled-item');
    AddClassToElement('.modal-header', 'disabled-item');
}

/* 
    Включение активности у всех элементов у компонента редактирования справочников
*/
function _enableAllElementToDirEdit() {
    RemoveClassToElement('.modal-body', 'disabled-item')
    RemoveClassToElement('.close', 'disabled-item');
    RemoveClassToElement('.modal-header', 'disabled-item');
}


/*
    Отключение строк в таблице
    @param table - таблица в которой необходимо заблокировать строки 
    @param ignoredId - идентификатор строки, которую необходимо игнорировать
*/
function _disableOtherRowsInTable(table, ignoredId) {
    if (!table) return;
    for (let row of table.querySelectorAll('tr')) {
        let rowId = Number(row.dataset.id);
        if (ignoredId === rowId) continue;
        if (!row.classList.contains('disabled-item')) row.classList.add('disabled-item');
    }
}

/*
    Включение строк в таблице
    @param table - таблица в которой необходимо включить строки 
    @param ignoredId - идентификатор строки, которую необходимо игнорировать
*/
function _enableOtherRowsInTable(table, ignoredId) {
    if (!table) return;
    for (let row of table.querySelectorAll('tr')) {
        let rowId = Number(row.dataset.id);
        if (ignoredId === rowId) continue;
        if (row.classList.contains('disabled-item')) row.classList.remove('disabled-item');
    }
}

/**********************************************/
/*************Хелперы*************************/
/**********************************************/

function GetInvalideChars() {
    const currentDeviceId = $('#ComboboxDevice').val();
    
    // Если ID устройства изменился или кэш пуст, обновляем кэш
    if (currentDeviceId !== lastDeviceId || cachedInvalidChars === null) {
        $.ajax({
            async: false,
            url: '/Home/GetInvalideChars',
            type: 'GET',
            data: {
                IdDevice: currentDeviceId
            },
            success: function(data) {
                if (data) {
                    cachedInvalidChars = JSON.parse(data);
                    lastDeviceId = currentDeviceId;
                }
            },
            error: function(xhr, status, error) {
                console.error('Ошибка при получении списка некорректных символов:', error);
                cachedInvalidChars = [];
            }
        });
    }
    
    return cachedInvalidChars || [];
}

function TestValidation() {
    // Проверяем наличие комбобокса
    const deviceCombo = $('#ComboboxDevice');
    if (!deviceCombo.length) {
        // console.error('Элемент ComboboxDevice не найден');
        return;
    }
    
    // Получаем список некорректных символов
    const invalidChars = GetInvalideChars();
    
    // Находим все текстовые поля
    const textFields = document.querySelectorAll('td input[type="text"]');
    
    // Тестируем валидацию на каждом поле
    textFields.forEach((field, index) => {
        
        // Тест 1: Пустое значение
        field.value = '';
        _validateEditCell(field.parentElement, 'text');
        
        // Тест 2: Значение с некорректным символом
        if (invalidChars && invalidChars.length > 0) {
            field.value = `Тест${invalidChars[0]}текст`;
            _validateEditCell(field.parentElement, 'text');
        }
        
        // Тест 3: Корректное значение
        field.value = 'Тестовый текст';
        _validateEditCell(field.parentElement, 'text');
    });
}

function _initTooltips() {
    $('.dir-editor-table input, .dir-editor-table select').each(function() {
        try {
            $(this).tooltip('destroy');
        } catch (e) {
            // Игнорируем ошибку, если tooltip еще не инициализирован
        }
        
        $(this).tooltip({
            position: { my: 'left+15 center', at: 'right center', of: this },
            classes: { 'ui-tooltip': 'tooltip-inner bg-danger' }
        });
    });
}

/*
    Отмена редактирования доверенности
    @param rowItem - редактируемая строка
    @param itemId - id доверенности
*/
function _cancelEditLicence(rowItem, itemId) {
    // Проверяем, является ли строка новой (в режиме инициализации)
    if (rowItem.dataset.isInit === 'true') {
        // Удаляем доверенность из массива
        appDictionaries['Licenses'] = appDictionaries['Licenses'].filter(lic => lic.Id !== itemId);
        
        // Включаем все элементы
        RemoveClassToElement('.table-bottom-menu', 'disabled-item');
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        
        // Включаем другие строки
        _enableOtherTableRows(itemId, 'licences-table');
        
        // Удаляем строку из таблицы
        rowItem.remove();
        return;
    }

    let rowMap = {
        0: 'bool', 1: 'text', 2: 'date', 3: 'ignore'
    };
    
    // Восстанавливаем предыдущее состояние
    if (rowItem.dataset.previousState) {
        const previousState = JSON.parse(rowItem.dataset.previousState);
        _restoreRowState(rowItem, previousState);
        delete rowItem.dataset.previousState;
    }
    
    // Включаем все элементы
    RemoveClassToElement('.table-bottom-menu', 'disabled-item');
    RemoveClassToElement('#dictionaries-list', 'disabled-item');
    RemoveClassToElement('.save-btn', 'disabled-item');
    RemoveClassToElement('.close', 'disabled-item');
    RemoveClassToElement('.modal-header', 'disabled-item');
    
    // Включаем другие строки
    _enableOtherTableRows(itemId, 'licences-table');
    
    // Меняем кнопки обратно
    let actionCell = rowItem.querySelector('td:last-child');
    let editDiv = actionCell.querySelector('div:first-child');
    let deleteDiv = actionCell.querySelector('div:last-child');
    
    // Удаляем текущие кнопки
    editDiv.innerHTML = '';
    deleteDiv.innerHTML = '';
    
    // Добавляем кнопки просмотра
    editDiv.appendChild(_createEditLicensesButton('fa-lock', 'btn-outline-primary', 'me-1'));
    deleteDiv.appendChild(_createDeleteLicenseBtn('fa-trash', 'btn-outline-danger', ''));
}

