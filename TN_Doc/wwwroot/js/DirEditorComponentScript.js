let appDictionaries;
let qpCfgsDictionaries
let dictFetchOptions;

/*
    Инициализация компонента "Редактора справочника"
*/
function InitDirEditorComponent(){

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
    _addDitctionariesHandler();
    _renderUserGroupsRowTable();
    _renderAndAddHandlerLicencesTable();
    _renderAndAddHandlerUserTable();
    _addLicHandler();
    _addUserHandler();
    _addSaveButtonHandler();
    _renderQpConfigs();
    /*end*/
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
        async: false,
        url: dictFetchOptions.getUrlDir,
        type: dictFetchOptions.getMethod,
        success: function (data) {
            appDictionaries = JSON.parse(data['dirJsonRaw'])
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.error(thrownError);
            appDictionaries = {};
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
        let elements = document.querySelectorAll('.list-group-item')
        for (let item of elements) {
            if (item.classList.contains('active'))
                item.classList.remove('active');
        }
        element.classList.add('active');
        document.querySelectorAll('.dir-item').forEach(function (item) {
            if (!item.classList.contains('d-none'))
                item.classList.add('d-none')
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

        let idTd = document.createElement('td');
        idTd.innerText = userGroup['Id'].toString();
        _verticalCenteringText(idTd)
        row.appendChild(idTd);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(userGroup['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        _verticalCenteringText(usedTd)
        row.appendChild(usedTd);

        let nameTD = document.createElement('td');
        nameTD.innerText = userGroup['Name'].toString();
        _verticalCenteringText(nameTD)
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

        let idCell = document.createElement('td');
        row.dataset.id = licences['Id'];
        idCell.innerText = licences['Id'];
        _verticalCenteringText(idCell);
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(licences['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        _verticalCenteringText(usedCell);
        row.appendChild(usedCell);

        let numberCell = document.createElement('td');
        numberCell.innerText = licences['LicensesNumber'];
        _verticalCenteringText(numberCell);
        row.appendChild(numberCell);

        let dateCell = document.createElement('td');
        dateCell.innerText = licences['LicensesDate'];
        _verticalCenteringText(dateCell);
        row.appendChild(dateCell);

        let editDivElement = document.createElement('div');
        editDivElement.appendChild(_createEditLicensesButton('fa:fa-lock:edit-licences-btn', 'btn:btn-outline-primary:edit-licences-btn', '5px'))

        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(_createDeleteLicenseBtn('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px', 'Licenses'))

        let actionCell = document.createElement('td');
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

        let idCell = document.createElement('td');
        idCell.innerText = user['Id'];
        row.dataset.id = user['Id'];
        _verticalCenteringText(idCell);
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(user['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        _verticalCenteringText(usedCell);
        row.appendChild(usedCell);

        let groupNameCell = document.createElement('td');
        groupNameCell.innerText = usersGroups.filter(group => group['Id'] === user['IdGroup'])[0]['Name'];
        _verticalCenteringText(groupNameCell);
        row.appendChild(groupNameCell);

        let surnameCell = document.createElement('td');
        surnameCell.innerText = user.F;
        _verticalCenteringText(surnameCell);
        row.appendChild(surnameCell);

        let nameCell = document.createElement('td');
        nameCell.innerText = user.I;
        _verticalCenteringText(nameCell);
        row.appendChild(nameCell);

        let patronymicCell = document.createElement('td');
        patronymicCell.innerText = user.O;
        _verticalCenteringText(patronymicCell);
        row.appendChild(patronymicCell);

        let organizationCell = document.createElement('td');
        organizationCell.innerText = user['Factory'];
        _verticalCenteringText(organizationCell);
        row.appendChild(organizationCell);

        let postCell = document.createElement('td');
        postCell.innerText = user['Post'];
        _verticalCenteringText(postCell);
        row.appendChild(postCell);

        let licCell = document.createElement('td');
        _verticalCenteringText(licCell);
        let licItem = licences.filter(item => item.IdUser === user['Id'])[0];
        if (licItem) {
            licCell.innerText = `[${licItem['Id']}] ${licItem['LicensesNumber']}`;
            licCell.dataset.licId = licItem['Id'];
        } else {
            licCell.innerText = 'Доверенность не выбрана';
            licCell.dataset.licId = 0;
        }
        row.appendChild(licCell);


        let editDivElement = document.createElement('div');
        editDivElement.appendChild(_createEditUsersButton('fa:fa-lock:edit-user-btn', 'btn:btn-outline-primary:edit-user-btn', '5px'))

        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(_createDeleteUserBtn('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px', 'Users'))

        let actionCell = document.createElement('td');
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
function _verticalCenteringText(cell) {
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
    btn.addEventListener('click', function (e) {
        _deleteSelectedRowHandler(e, 'Users');
        let user_id = Number(e.target.closest('.data-row').dataset.id);
        let lic = appDictionaries['Licenses'].filter(item => item['IdUser'] === user_id)[0];
        if (lic)
            lic['IdUser'] = 0;
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
    btn.addEventListener('click', function (e) {
        _deleteSelectedRowHandler(e, 'Licenses');
        _clearRowTable('.users-table')
        _renderAndAddHandlerUserTable();
    });
    return btn
}

/*
    Удаление строки в таблицы
    @param e - событие удаления
    @param arrayName - имя массива для удаления
*/
function _deleteSelectedRowHandler(e, arrayName) {
    if (!e.target.classList.contains('delete-btn'))
        return;
    let rowItem = e.target.closest('tr');
    let itemId = Number(rowItem.dataset.id);
    if (!itemId)
        return;
    appDictionaries[arrayName] = appDictionaries[arrayName].filter(function (item) {
        return item['Id'] !== itemId;
    })
    rowItem.remove();
}


/*
    Создание кнопки редактирования для доверенностей
    @param faClass - набор классов с картинками. Разделитель ":"
    @param buttonClass - набор классов для кнопки. Разделитель ":"
    @param margin - конфигурация отсутупов
*/
function _createEditLicensesButton(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.dataset.mode = 'stable';
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
    Редактирование выбранной доверенности в таблице.
    @param itemBtn - кнопка по которой нажали. Кнопка должна находиться в редактируемой строке  таблице.
    @param rowItem - редактируемая строка в таблице.
    @param itemId - id объекта в массиве.
*/
function _editSelectedUser(itemBtn, rowItem, itemId) {
    let rowMap = {
        0: 'ignore',
        1: 'bool',
        2: 'combobox-ug',
        3: 'text',
        4: 'text',
        5: 'text',
        6: 'text',
        7: 'text',
        8: 'combobox-lic',
        9: 'ignore'
    }

    if (itemBtn.dataset.mode === 'stable') {
        AddClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item');
        AddClassToElement('#dictionaries-list', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('.close', 'disabled-item');
        AddClassToElement('.table-bottom-menu', 'disabled-item')
        AddClassToElement('.modal-header', 'disabled-item')
        _disableOtherTableRows(itemId, 'users-table');
        _convertStableRowToEditRow(rowItem, rowMap)
        _changeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        rowMap[6] = "ignore";
        rowMap[7] = "ignore";
        if (!_validateEditRow(rowItem, rowMap))
            return;
        rowMap[6] = "text";
        rowMap[7] = "text";
        _changeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
        _convertEditRowToStableRow(rowItem, rowMap, false)
        RemoveClassToElement('.table-bottom-menu', 'disabled-item')
        RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item')
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item')
        _applyUserChanges(rowItem, itemId)
        _enableOtherTableRows(itemId, 'users-table');
        itemBtn.dataset.mode = 'stable';
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
        0: 'ignore',
        1: 'bool',
        2: 'text',
        3: 'date',
        4: 'ignore'
    }
    if (itemBtn.dataset.mode === 'stable') {
        AddClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item');
        AddClassToElement('#dictionaries-list', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('.close', 'disabled-item');
        AddClassToElement('.table-bottom-menu', 'disabled-item')
        AddClassToElement('.modal-header', 'disabled-item')
        _disableOtherTableRows(itemId, 'licences-table');
        _convertStableRowToEditRow(rowItem, rowMap)
        _changeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        if (!_validateEditRow(rowItem, rowMap))
            return;
        _changeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
        _convertEditRowToStableRow(rowItem, rowMap, false)
        RemoveClassToElement('.table-bottom-menu', 'disabled-item')
        RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item')
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item')
        _applyLicenceChanges(rowItem, itemId)
        _enableOtherTableRows(itemId, 'licences-table');
        itemBtn.dataset.mode = 'stable';
        _clearRowTable('.users-table')
        _renderAndAddHandlerUserTable();
    }
}

/*
    Применение изменений доверенность 
    @param rowItem - отредкатированная строка
    @param itemId - id доверености
*/
function _applyLicenceChanges(rowItem, itemId) {
    if (!rowItem || !itemId)
        return;
    let objIndex = appDictionaries['Licenses'].findIndex(item => item.Id === itemId);
    if (objIndex < 0) return;
    let cells = rowItem.cells;
    let updatedObject = appDictionaries['Licenses'][objIndex]
    updatedObject['Use'] = cells[1].childNodes[0].classList.contains('fa-check-square-o');
    updatedObject['LicensesNumber'] = cells[2].childNodes[0].textContent;
    updatedObject['LicensesDate'] = cells[3].childNodes[0].textContent;
}

/*
    Применение изменений пользовательского справочника 
    @param rowItem - отредкатированная строка
    @param itemId - id доверености
*/
function _applyUserChanges(rowItem, itemId) {
    if (!rowItem || !itemId)
        return;
    let objIndex = appDictionaries['Users'].findIndex(item => item.Id === itemId);
    if (objIndex < 0) return;
    let cells = rowItem.cells;
    let updatedObject = appDictionaries['Users'][objIndex]
    updatedObject['Use'] = cells[1].childNodes[0].classList.contains('fa-check-square-o');
    updatedObject['IdGroup'] = appDictionaries['UsersGroup'].filter(item => item['Name'] === cells[2].childNodes[0].textContent)[0]['Id'];
    updatedObject['F'] = cells[3].childNodes[0].textContent;
    updatedObject['I'] = cells[4].childNodes[0].textContent;
    updatedObject['O'] = cells[5].childNodes[0].textContent;

    let fact = cells[6].childNodes[0].textContent;
    if (!fact) {
        updatedObject['Factory'] = "";
    } else {
        updatedObject['Factory'] = fact;
    }

    let post = cells[7].childNodes[0].textContent;
    if (!post) {
        updatedObject['Post'] = "";
    } else {
        updatedObject['Post'] = post;
    }


    let lic = appDictionaries['Licenses'].filter(item => item['Id'] === Number(cells[8].dataset.licId))[0];
    if (!lic)
        return;
    lic['IdUser'] = updatedObject['Id'];
}

/*
    Валидация строки таблицы
*/
function _validateEditRow(row, rowMap) {
    let cells = row.querySelectorAll('td')
    for (let i = 0; i < cells.length; i++) {
        _validateEditCell(cells[i], rowMap[i]);
    }
    return row.querySelectorAll('td.invalid-cell-content').length === 0;
}

/*
   Валидация значение ячеек таблицы 
*/
function _validateEditCell(cell, type) {
    if (!type || !cell)
        return false;

    if (cell.classList.contains('invalid-cell-content')) {
        cell.classList.remove('invalid-cell-content')
    }

    switch (type) {
        case 'text':
            let text = cell.childNodes[0].value;
            if (!text)
                cell.classList.add('invalid-cell-content');
            break;
        case 'date':
            let date = cell.childNodes[0].value;
            if (!date)
                cell.classList.add('invalid-cell-content');
            break;
        case 'number':
            let num = cell.childNodes[0].value;
            if (!num)
                cell.classList.add('invalid-cell-content');
            break;
    }

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
            let date = $('.calendar').datepicker('getDate');
            let dayStr = date.getDate().toString();
            let month = date.getMonth() + 1;
            let monthStr = month < 9 ? `0${date.getMonth() + 1}` : month.toString();
            let yearStr = date.getFullYear().toString();
            let newDateText = document.createTextNode(dayStr + '.' + monthStr + '.' + yearStr);
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
                let licenseElement = document.createTextNode(`[${license['Id']}] ${license['LicensesNumber']}`);
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
            if (!innerImage)
                return;
            newElement.type = 'checkbox';
            newElement.checked = innerImage.classList.contains('fa-check-square-o');
            cell.replaceChild(newElement, cell.childNodes[0])
            break;
        case 'text':
            let prText = cell.innerText;
            newElement.type = 'text';
            newElement.value = prText ? prText : '';
            if (previewNode)
                cell.replaceChild(newElement, previewNode)
            else
                cell.append(newElement);
            break;
        case 'date':
            let prDate = new Date(moment(cell.innerText, 'DD.MM.YYYY').format())
            newElement.classList.add('calendar');
            if (previewNode)
                cell.replaceChild(newElement, previewNode)
            else
                cell.append(newElement);
            $('.calendar').datepicker({dateFormat: 'dd.mm.yy'});
            $('.calendar').datepicker('setDate', prDate);
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
            if (previewNode)
                cell.replaceChild(cbElement, previewNode)
            else
                cell.append(cbElement);
            break;
        case 'combobox-lic':
            let cbElementLic = document.createElement('select');
            let licenses = appDictionaries['Licenses'];
            let counterLic = 0;

            let optDefault = document.createElement('option');
            optDefault.setAttribute('value', "0");
            optDefault.append(`0 - Доверенность не выбрана`);
            cbElementLic.append(optDefault);
            for (let i = 0; i < licenses.length; i++) {
                let opt = document.createElement('option');
                opt.setAttribute('value', licenses[i]['Id']);
                opt.append(`${licenses[i]['Id']} - ${licenses[i]['LicensesNumber']} - ${licenses[i]['LicensesDate']}`);
                cbElementLic.append(opt);
                if (Number(cell.dataset.licId) === licenses[i]['Id']) {
                    cbElementLic.selectedIndex = counterLic;
                }
                counterLic++;
            }
            if (previewNode)
                cell.replaceChild(cbElementLic, previewNode)
            else
                cell.append(cbElementLic);
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

            for (let br of cell.querySelectorAll('br'))
                br.remove();
            cell.appendChild(cbEl);
            break;
        case 'number':
            let prNumber = cell.innerText;
            newElement.type = 'number';
            newElement.value = prNumber ? Number.parseFloat(prNumber.replaceAll(',', '.')) : '0.0';
            if (previewNode) {
                cell.replaceChild(newElement, previewNode)
            } else
                cell.append(newElement);
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
    document.querySelector('.add-lic-btn').addEventListener(
        'click',
        function (e) {
            let item = e.target;
            if (e.target.tagName === "I" || e.target.tagName === "LABEL") {
                item = e.target.closest('button');
            }
            let maxId = appDictionaries['Licenses'].length !== 0 ? appDictionaries['Licenses'].reduce((aid, cid) => {
                    return aid > cid ? aid : cid;
                })['Id']
                : 0;
            appDictionaries['Licenses'].push({
                Id: maxId + 1,
                Use: false,
                IdUser: 0,
                LicensesDate: "",
                LicensesNumber: ""
            })
            _clearRowTable('.licences-table')
            _renderAndAddHandlerLicencesTable();
            _scrollToBottomTable(' #licences >.table-container> .table-content');

            let lastRow = document.querySelector('.licences-table').lastChild;
            let itemId = Number(lastRow.dataset.id);
            let itemBtn = lastRow.querySelector('.edit-licences-btn');
            if (!itemId || !itemBtn || !lastRow)
                return;
            _editSelectedLicences(itemBtn, lastRow, itemId)
        }
    );
}

/*
    Добавление обработчика добавления пользователя
*/
function _addUserHandler() {
    document.querySelector('.add-user-btn').addEventListener(
        'click',
        function () {
            let maxId = appDictionaries['Users'].length !== 0 ? appDictionaries['Users'].reduce((aid, cid) => {
                    return aid > cid ? aid : cid;
                })['Id']
                : 0;
            appDictionaries['Users'].push({
                Use: false,
                Id: maxId + 1,
                IdGroup: 1,
                F: "",
                I: "",
                O: "",
                Factory: "",
                Post: ""
            })
            _clearRowTable('.users-table')
            _renderAndAddHandlerUserTable();
            _scrollToBottomTable(' #users >.table-container> .table-content');

            let lastRow = document.querySelector('.users-table').lastChild;
            let itemId = Number(lastRow.dataset.id);
            let itemBtn = lastRow.querySelector('.edit-user-btn');
            if (!itemId || !itemBtn || !lastRow)
                return;
            _editSelectedUser(itemBtn, lastRow, itemId)
        }
    );
}

/* 
    Добавление обработчика события сохранения данных
*/
function _addSaveButtonHandler() {
    document.querySelector('.modal-header > .close-menu > .save-btn')
        .addEventListener('click', function () {
        _disableAllElementToDirEdit();
        RemoveClassToElement('.modal-header>.close-menu >.btn > i > .fa-spin', 'd-none')
        AddClassToElement('.modal-header>.close-menu >.btn > i > .fa-flooppy', 'd-none')
        try {
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
                    console.error(thrownError)
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
                    console.error(thrownError)
                    qpCfgsDictionaries = {};
                },

            });
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
        async: false,
        url: dictFetchOptions.getQpCfg,
        type: dictFetchOptions.getMethod,
        success: function (data) {
            qpCfgsDictionaries = JSON.parse(data['qpCfgJsonRaw'])
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.error(thrownError);
            appDictionaries = {};
        }
    })
}


/*
    Добавление обработчика для переключения табов на странице  
*/
function _addTabsSelectorHandler() {
    document.querySelector(".tabs-selector").addEventListener('click', function (e) {
        let element = e.target;
        if (element.classList.contains("active"))
            return;
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

/*
    Отрисовка паспортов качества 
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
            if (element.classList.contains('active'))
                return
            for (let item of document.querySelectorAll('.qp-dir-item')) {
                if (!item.classList.contains('d-none'))
                    item.classList.add('d-none');
            }
            let elem = document.querySelector(element.dataset.target);
            if (elem)
                elem.classList.remove('d-none');

            for (let el of document.querySelectorAll('#qp-list>.list-group-item')) {
                if (el.classList.contains('active'))
                    el.classList.remove('active');
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
        let maxId = qpCfgsDictionaries['QpsInfo'][qpId]['Methods'].length !== 0
            ? qpCfgsDictionaries['QpsInfo'][qpId]['Methods'].reduce((accumId, curId) => {
                return accumId > curId ? accumId : curId;
            }) ['Id'] : 1;
        qpCfgsDictionaries['QpsInfo'][qpId]['Methods'].push({
            Id: maxId + 1,
            Use: false,
            IdParameter: 0,
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
        _scrollToBottomTable(li.dataset.target + ' > .table-container > .table-content');
        _editQpMethod(
            document.querySelector(li.dataset.target + '> .table-container > .table-content > table').lastChild,
            document.querySelector(li.dataset.target + '> .table-container > .table-content > table').lastChild.querySelector('.edit-methods-btn')
        )
    });

    let img = document.createElement('i');
    img.classList.add('fa', 'fa-plus');
    img.ariaHidden = 'true';
    btn.appendChild(img);
    let lbl = document.createElement('label');
    lbl.appendChild(document.createTextNode(' Добавить'));
    btn.appendChild(lbl);
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
* Рендеринг методов паспортов качества 
*/
function _renderQpConfigsMethodsTable(counter, qps, baseDiv) {
    let methodsTable = document.createElement('table');
    methodsTable.classList.add('table', 'table-bordered', 'inner-item-center', 'qp-method-table');
    methodsTable.dataset.qpId = counter;
    baseDiv.appendChild(methodsTable);
    let tbMethodsHead = document.createElement('thead');
    methodsTable.appendChild(tbMethodsHead);
    let hRow = document.createElement('tr');
    hRow.classList.add('table-primary');
    tbMethodsHead.appendChild(hRow);
    hRow.appendChild(_createTableColumnHeader("ID"));
    hRow.appendChild(_createTableColumnHeader("Активен"));
    hRow.appendChild(_createTableColumnHeader("Метод"));
    hRow.appendChild(_createTableColumnHeader("Параметр"));
    hRow.appendChild(_createTableColumnHeader("Контроль мин. значения"));
    hRow.appendChild(_createTableColumnHeader("Мин. значение"));
    hRow.appendChild(_createTableColumnHeader("Сообщение"));
    hRow.appendChild(_createTableColumnHeader("Действия"));
    let methods = qps["Methods"];
    if (!methods)
        return;
    for (let method of methods) {
        let row = document.createElement('tr');
        row.classList.add('data-row');
        let idCell = document.createElement('td');
        idCell.innerText = method['Id'];
        row.dataset.id = method['Id'];
        _verticalCenteringText(idCell);
        row.appendChild(idCell);
        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(method['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        _verticalCenteringText(usedTd)
        row.appendChild(usedTd);
        let methodName = document.createElement('td');
        methodName.innerText = method['Name'];
        _verticalCenteringText(methodName);
        row.appendChild(methodName);
        let paramsArray = qps["Parameters"];
        let param = paramsArray.filter(item => item["Id"] === method["IdParameter"])[0];
        let paramCell = document.createElement('td')
        _verticalCenteringText(paramCell)
        paramCell.innerText = param ? param["Name"] : " - ";
        paramCell.dataset.paramId = param ? param["Id"] : 0;
        row.appendChild(paramCell);
        let limitActive = document.createElement('i')
        limitActive.classList.add('fa');
        limitActive.classList.add(method['LimitValueActive'] === true ? 'fa-check-square-o' : 'fa-square-o');
        limitActive.ariaHidden = true;
        let limitActiveCell = document.createElement('td');
        limitActiveCell.appendChild(limitActive);
        _verticalCenteringText(limitActiveCell)
        row.appendChild(limitActiveCell);
        let LimitValueCell = document.createElement('td');
        LimitValueCell.innerText = method['LimitValue'];
        _verticalCenteringText(LimitValueCell);
        row.appendChild(LimitValueCell);
        let LimitValueStringCell = document.createElement('td');
        LimitValueStringCell.innerText = !method['LimitValueString'] ? '-' : method['LimitValueString'];
        _verticalCenteringText(LimitValueStringCell);
        row.appendChild(LimitValueStringCell);
        let actionCell = document.createElement('td');
        actionCell.appendChild(_createEditQpMethodsBtn('fa:fa-lock:edit-user-btn', 'btn:btn-outline-primary:edit-methods-btn', '5px'));
        actionCell.appendChild(_createDeleteQpMethodsBtn('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px'));
        _verticalCenteringText(actionCell);
        row.appendChild(actionCell)
        methodsTable.appendChild(row);
    }
}


/*
    Создание кнопки удаления методов из паспортов качества
*/
function _createDeleteQpMethodsBtn(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.addEventListener('click', _deleteQpMethodsBtnHandler)
    let div = document.createElement('div')
    div.appendChild(btn);
    return div;
}

/*
    Создание кнопки редактирования методов
*/
function _createEditQpMethodsBtn(faClass, buttonClass, margin) {
    let btn = _createWithOnlyImgButton(faClass, buttonClass, margin);
    btn.dataset.mode = 'stable';
    let div = document.createElement('div');
    btn.addEventListener('click', _editQpMethodBtnHandler)
    div.appendChild(btn);
    return div;
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
    _editQpMethod(row, item);
}

/*
    Редактирование строки метода паспорта качества
    @param row - строка редактирования
    @param itemBtn - кнопка редактирования в строке
*/
function _editQpMethod(row, itemBtn) {
    let rowMap = {
        0: 'ignore',
        1: 'bool',
        2: 'text',
        3: 'combobox-params',
        4: 'bool',
        5: 'number',
        6: 'text',
        7: 'ignore'
    }

    if (itemBtn.dataset.mode === 'stable') {
        _changeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
        AddClassToElement('#qp-list', 'disabled-item');
        AddClassToElement('.modal-header', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('row', 'disabled-item');
        AddClassToElement('tr[data-id="' + Number(row.dataset.id) + '"] td button.delete-btn', 'disabled-item');
        _disableOtherRowsInTable(itemBtn.closest('table'), Number(row.dataset.id));
        _convertStableRowToEditRow(row, rowMap);
        itemBtn.dataset.mode = 'edit';
    } else {
        if (!_validateEditRow(row, rowMap))
            return;
        _convertEditRowToStableRow(row, rowMap, true)
        _changeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
        RemoveClassToElement('#qp-list', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('tr[data-id="' + Number(row.dataset.id) + '"] td button.delete-btn', 'disabled-item');
        _enableOtherRowsInTable(itemBtn.closest('table'), Number(itemBtn.closest('tr').dataset.id));
        _applyQpMethodsChanged(row, Number(row.dataset.id), Number(itemBtn.closest('table').dataset.qpId));
        itemBtn.dataset.mode = 'stable';
    }
}

/*
    Применение изменений метода паспорта качества 
*/
function _applyQpMethodsChanged(rowItem, itemId, qpId) {
    if (!rowItem || !itemId)
        return;
    if (qpId === undefined)
        return;
    let methodIndex = qpCfgsDictionaries["QpsInfo"][qpId]["Methods"].findIndex(item => item['Id'] === itemId);
    if (methodIndex < 0)
        return;
    let cells = rowItem.cells;
    let updatedObject = qpCfgsDictionaries["QpsInfo"][qpId]["Methods"][methodIndex];
    updatedObject['Use'] = cells[1].childNodes[0].classList.contains('fa-check-square-o');
    updatedObject['Name'] = cells[2].childNodes[0].textContent;
    let parameter = qpCfgsDictionaries["QpsInfo"][qpId]["Parameters"].filter(item => item["Name"] === cells[3].childNodes[0].textContent)[0];
    if (parameter) {
        updatedObject['IdParameter'] = parameter['Id'];
    } else {
        updatedObject['IdParameter'] = 0;
    }

    updatedObject['LimitValueActivate'] = cells[4].childNodes[0].classList.contains('fa-check-square-o');
    updatedObject['LimitValue'] = Number.parseFloat(cells[5].childNodes[0].textContent.replaceAll(',', '.'));

    let msg = cells[6].childNodes[0].textContent;
    if (!msg) {
        updatedObject['LimitValueString'] = '-'
    } else {
        updatedObject['LimitValueString'] = msg;
    }
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
        if (ignoredId === rowId)
            continue;
        if (!row.classList.contains('disabled-item'))
            row.classList.add('disabled-item');
    }
}

function _enableOtherRowsInTable(table, ignoredId) {
    if (!table) return;
    for (let row of table.querySelectorAll('tr')) {
        let rowId = Number(row.dataset.id);
        if (ignoredId === rowId)
            continue;
        if (row.classList.contains('disabled-item'))
            row.classList.remove('disabled-item');
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
    _verticalCenteringText(el);
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
    if (!selector || !className)
        return;
    let elements = document.querySelectorAll(selector);
    if (!elements)
        return;
    let classes = className.split(':');
    elements.forEach(el => {
        if (isRemove) {
            for (let cl of classes) {
                if (!cl) return;
                if (el.classList.contains(cl))
                    el.classList.remove(cl)
            }
        } else {
            elements.forEach(el => {
                for (let cl of classes) {
                    if (!cl) return;
                    if (!el.classList.contains(cl))
                        el.classList.add(cl)
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
    AddClassToElement(faClass, img)
    img.ariaHidden = true;
    img.style.fontSize = '1.5em'
    let btn = document.createElement('button')
    AddClassToElement(buttonClass, btn)
    btn.appendChild(img);
    btn.style.margin = margin;
    btn.style.alignSelf = 'center';
    return btn;

    function AddClassToElement(classes, element) {
        if (!classes) return;
        let elClasses = classes.split(':');
        for (let cl of elClasses) {
            if (!cl) continue;
            element.classList.add(cl);
        }
    }

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
    for (let index = 1; index < dataRows.length + 1; index++)
        table.deleteRow(1)
}

/*
    Прокрутка таблицы вниз
    @param tableSelector - селектор таблицы
*/
function _scrollToBottomTable(tableSelector) {
    if (!tableSelector)
        return;
    let tableController = document.querySelector(tableSelector);
    if (!tableController)
        return;
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

/**********************************************/
/*************Хелперы*************************/
/**********************************************/