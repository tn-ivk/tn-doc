/*
* Загрузка словарей приложения. Словари поступают в формате json.
* Сами словари хранятся в поле 'dirJsonRaw' (datas['dirJsonRawS']).
* Словари сохраняются в объекте 'appDictionaries'.
* В случае появления ошибки 'appDictionaries' инициализруется пустым объектом
* ,а в консоль пишется ошибка
*  Дополнительно словари записываются в локальное хранилище
* */
function LoadAppDictionaries() {
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
function AddDitctionariesHandler() {

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
function RenderUserGroupsRowTable() {
    let table = document.querySelector('.user-group-table');
    for (let userGroup of appDictionaries['UsersGroup']) {
        let row = document.createElement('tr');
        row.classList.add('data-row')

        let idTd = document.createElement('td');
        idTd.innerText = userGroup['Id'].toString();
        VerticalCenteringCell(idTd)
        row.appendChild(idTd);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(userGroup['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        VerticalCenteringCell(usedTd)
        row.appendChild(usedTd);

        let nameTD = document.createElement('td');
        nameTD.innerText = userGroup['Name'].toString();
        VerticalCenteringCell(nameTD)
        row.appendChild(nameTD);

        table.append(row)
    }
}

/*
* Отрисовка таблицы для довереностей.
* Дополнительно добавляются обработчики на кнопки
*/
function RenderAndAddHandlerLicencesTable() {
    let table = document.querySelector('.licences-table');
    for (let licences of appDictionaries['Licenses']) {
        let row = document.createElement('tr')
        row.classList.add('data-row')

        let idCell = document.createElement('td');
        row.dataset.id = licences['Id'];
        idCell.innerText = licences['Id'];
        VerticalCenteringCell(idCell);
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(licences['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        VerticalCenteringCell(usedCell);
        row.appendChild(usedCell);

        let numberCell = document.createElement('td');
        numberCell.innerText = licences['LicensesNumber'];
        VerticalCenteringCell(numberCell);
        row.appendChild(numberCell);

        let dateCell = document.createElement('td');
        dateCell.innerText = licences['LicensesDate'];
        VerticalCenteringCell(dateCell);
        row.appendChild(dateCell);

        let editDivElement = document.createElement('div');
        editDivElement.appendChild(CreateEditLicensesButton('fa:fa-lock:edit-licences-btn', 'btn:btn-outline-primary:edit-licences-btn', '5px', 'Licenses'))

        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(CreateDeleteLicenseBtn('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px', 'Licenses'))

        let actionCell = document.createElement('td');
        actionCell.appendChild(editDivElement);
        actionCell.appendChild(deleteDivElement);
        row.appendChild(actionCell);
        table.append(row)
    }

}

/*
* Отрисовка таблицы пользователей.
*/
function RenderAndAddHandlerUserTable() {
    let table = document.querySelector('.users-table');
    let usersGroups = appDictionaries['UsersGroup'];
    let licences = appDictionaries['Licenses'];
    for (let user of appDictionaries['Users']) {

        let row = document.createElement('tr')
        row.classList.add('data-row')

        let idCell = document.createElement('td');
        idCell.innerText = user['Id'];
        row.dataset.id = user['Id'];
        VerticalCenteringCell(idCell);
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(user['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        VerticalCenteringCell(usedCell);
        row.appendChild(usedCell);

        let groupNameCell = document.createElement('td');
        groupNameCell.innerText = usersGroups.filter(group => group['Id'] === user['IdGroup'])[0]['Name'];
        VerticalCenteringCell(groupNameCell);
        row.appendChild(groupNameCell);

        let surnameCell = document.createElement('td');
        surnameCell.innerText = user.F;
        VerticalCenteringCell(surnameCell);
        row.appendChild(surnameCell);

        let nameCell = document.createElement('td');
        nameCell.innerText = user.I;
        VerticalCenteringCell(nameCell);
        row.appendChild(nameCell);

        let patronymicCell = document.createElement('td');
        patronymicCell.innerText = user.O;
        VerticalCenteringCell(patronymicCell);
        row.appendChild(patronymicCell);

        let organizationCell = document.createElement('td');
        organizationCell.innerText = user['Factory'];
        VerticalCenteringCell(organizationCell);
        row.appendChild(organizationCell);

        let postCell = document.createElement('td');
        postCell.innerText = user['Post'];
        VerticalCenteringCell(postCell);
        row.appendChild(postCell);

        let licCell = document.createElement('td');
        VerticalCenteringCell(licCell);
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
        editDivElement.appendChild(CreateEditUsersButton('fa:fa-lock:edit-user-btn', 'btn:btn-outline-primary:edit-user-btn', '5px'))

        let deleteDivElement = document.createElement('div');
        deleteDivElement.appendChild(CreateDeleteUserBtn('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px', 'Users'))

        let actionCell = document.createElement('td');
        actionCell.appendChild(editDivElement);
        actionCell.appendChild(deleteDivElement);

        row.appendChild(actionCell);
        table.append(row)
    }


}

function VerticalCenteringCell(cell){
    cell.classList.add('align-middle')
}

/*
*  Создает кнопку только с рисунками.
*  Присваивает указанные классы.
*  Если необходимо присвоить множество классов, то их необходимо разделять  ':'
*/
function CreateWithOnlyImgButton(faClass, buttonClass, margin) {

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
 *  Создание кнопки удаления пользователя
 */
function CreateDeleteUserBtn(faClass, buttonClass, margin) {
    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
    btn.addEventListener('click', function (e) {
        DeleteSelectedRowHandler(e, 'Users');
        let user_id = Number(e.target.closest('.data-row').dataset.id);
        let lic = appDictionaries['Licenses'].filter(item => item['IdUser'] === user_id)[0];
        if (lic)
            lic['IdUser'] = 0;
    });
    return btn
}

/*
 *  Создание кнопки удаление доверенности
 */
function CreateDeleteLicenseBtn(faClass, buttonClass, margin) {
    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
    btn.addEventListener('click', function (e) {
        DeleteSelectedRowHandler(e, 'Licenses');
        ClearRowTable('.users-table')
        RenderAndAddHandlerUserTable();
    });
    return btn
}

/*
 * Удаление строки в таблицы
 */
function DeleteSelectedRowHandler(e, arrayName) {
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
* Создание кнопки редактирования для доверенностей
*/
function CreateEditLicensesButton(faClass, buttonClass, margin, arrayName) {
    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
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
        EditSelectedLicences(itemBtn, rowItem, itemId)
    });
    return btn
}

function CreateEditUsersButton(faClass, buttonClass, margin) {
    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
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
        EditSelectedUser(itemBtn, rowItem, itemId)
    });
    return btn
}

/*
* Редактирование выбранной доверенности в таблице.
* @param itemBtn - кнопка по которой нажали. Кнопка должна находиться в редактируемой строке  таблице.
* @param rowItem - редактируемая строка в таблице.
* @param itemId - id объекта в массиве.
*/
function EditSelectedUser(itemBtn, rowItem, itemId) {
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
        DisableOtherTableRows(itemId, 'users-table');
        ConvertStableRowToEditRow(rowItem, rowMap)
        ChangeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        rowMap[6]="ignore";
        rowMap[7]="ignore";
        if (!ValidateEditRow(rowItem, rowMap))
            return;
        rowMap[6]="text";
        rowMap[7]="text";
        ChangeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
        ConvertEditRowToStableRow(rowItem, rowMap)
        RemoveClassToElement('.table-bottom-menu', 'disabled-item')
        RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item')
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        ApplyUserChanges(rowItem, itemId)
        EnableOtherTableRows(itemId, 'users-table');
        itemBtn.dataset.mode = 'stable';
    }
}


/*
* Редактирование выбранной доверенности в таблице.
* @param itemBtn - кнопка по которой нажали. Кнопка должна находиться в редактируемой строке  таблице.
* @param rowItem - редактируемая строка в таблице.
* @param itemId - id объекта в массиве.
*/
function EditSelectedLicences(itemBtn, rowItem, itemId) {
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
        DisableOtherTableRows(itemId, 'licences-table');
        ConvertStableRowToEditRow(rowItem, rowMap)
        ChangeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        if (!ValidateEditRow(rowItem, rowMap))
            return;
        ChangeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
        ConvertEditRowToStableRow(rowItem, rowMap)
        RemoveClassToElement('.table-bottom-menu', 'disabled-item')
        RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item')
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        ApplyLicenceChanges(rowItem, itemId)
        EnableOtherTableRows(itemId, 'licences-table');
        itemBtn.dataset.mode = 'stable';
        ClearRowTable('.users-table')
        RenderAndAddHandlerUserTable();
    }
}

/*
* Применение изменений доверенность 
* @param rowItem - отредкатированная строка
* @param itemId - id доверености
*/
function ApplyLicenceChanges(rowItem, itemId) {
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
* Применение изменений пользовательского справочника 
* @param rowItem - отредкатированная строка
* @param itemId - id доверености
*/
function ApplyUserChanges(rowItem, itemId) {
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

    let fact =cells[6].childNodes[0].textContent;
    if (!fact) {
        updatedObject['Factory'] = "";
    } else {
        updatedObject['Factory'] = fact;
    }

    let post =cells[7].childNodes[0].textContent;
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
* Валидация строки таблицы
*/
function ValidateEditRow(row, rowMap) {
    let cells = row.querySelectorAll('td')
    for (let i = 0; i < cells.length; i++) {
        ValidateEditCell(cells[i], rowMap[i]);
    }
    return row.querySelectorAll('td.invalid-cell-content').length === 0;
}

/*
* Валидация значение ячеек таблицы 
*/
function ValidateEditCell(cell, type) {
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
    }

}

/*
* Изменение иконки 
*/
function ChangeButtonIcon(itemBtn, newClass, oldClass) {
    itemBtn.querySelector('i').classList.replace(oldClass, newClass);
}

/*
* Конверитирование ячейки редактирования в ячейку стабильную 
*/
function ConvertEditRowToStableRow(row, rowMap) {
    let cells = row.querySelectorAll('td');
    let usersGroupArray = appDictionaries['UsersGroup'];
    let licensesArray = appDictionaries['Licenses'];
    let userId = Number(row.dataset.id);
    for (let i = 0; i < cells.length; i++) {
        ConvertEditCellToStableCell(cells[i], rowMap[i], usersGroupArray, licensesArray, userId);
    }
}

/*
* Конверитирование ячейки редактирования в ячейку стабильную 
*/
function ConvertEditCellToStableCell(cell, type, usersGroupArray, licensesArray, userId) {
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
    }
}

/*
* Конверитирование стабильной строки в строку для редактирования
*/
function ConvertStableRowToEditRow(row, rowMap) {

    let cells = row.querySelectorAll('td');
    for (let i = 0; i < cells.length; i++) {
        ConvertStableCellToEditCell(cells[i], rowMap[i])
    }
}

/*
* Конверитирование стабильной ячейки в ячейку для редактирования
*/
function ConvertStableCellToEditCell(cell, type) {
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
        default:
            break
    }


}

/*
* Отключение  других строк
*/
function DisableOtherTableRows(ignoredItemId, tableClass) {
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
* Включение других строк
*/
function EnableOtherTableRows(ignoredItemId, tableClass) {
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
* Добавление класса элементу
*/
function AddClassToElement(selector, className) {
    ModificationClassToElement(selector, className, false)
}

/*Удаление класса элемента*/
function RemoveClassToElement(selector, className) {
    ModificationClassToElement(selector, className, true)
}

/*Модификация списка классов у элемета HTML */
function ModificationClassToElement(selector, className, isRemove) {
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

/*Добавление обработчика добавления довереностей*/
function AddLicHandler() {
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
            ClearRowTable('.licences-table')
            RenderAndAddHandlerLicencesTable();
            ScrollToBottomTable(' #licences >.table-container> .table-content');

            let lastRow = document.querySelector('.licences-table').lastChild;
            let itemId = Number(lastRow.dataset.id);
            let itemBtn = lastRow.querySelector('.edit-licences-btn');
            if (!itemId || !itemBtn || !lastRow)
                return;
            EditSelectedLicences(itemBtn, lastRow, itemId)
        }
    );
}

function AddUserHandler() {
    document.querySelector('.add-user-btn').addEventListener(
        'click',
        function (e) {
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
            ClearRowTable('.users-table')
            RenderAndAddHandlerUserTable();
            ScrollToBottomTable(' #users >.table-container> .table-content');

            let lastRow = document.querySelector('.users-table').lastChild;
            let itemId = Number(lastRow.dataset.id);
            let itemBtn = lastRow.querySelector('.edit-user-btn');
            if (!itemId || !itemBtn || !lastRow)
                return;
            EditSelectedUser(itemBtn, lastRow, itemId)
        }
    );
}

/* Очистка таблицы по селектору контрола таблицы*/
function ClearRowTable(tableSelector) {
    let table = document.querySelector(tableSelector);
    if (!table) return;
    let dataRows = table.querySelectorAll('tr.data-row');
    for (let index = 1; index < dataRows.length + 1; index++)
        table.deleteRow(1)
}

/*Прокрутка таблицы вниз*/
function ScrollToBottomTable(tableSelector) {
    if (!tableSelector)
        return;
    let tableController = document.querySelector(tableSelector);
    if (!tableController)
        return;
    tableController.scrollTo(0, tableController.scrollHeight)
}

/*Перерендеринг таблиц пользователей и доверенностей*/
function  ReRenderTable(){
    ClearRowTable('.users-table');
    ClearRowTable('.licences-table');
    RenderAndAddHandlerLicencesTable();
    RenderAndAddHandlerUserTable();
}

/* Добавление обработчика события*/
function AddSaveButtonHandler() {
    document.querySelector('.modal-footer > .save-btn').addEventListener('click', function (e) {
        DisableAllElementToDirEdit();
        RemoveClassToElement('.modal-footer> .btn > i', 'd-none')
        AddClassToElement('.modal-footer> .btn > label', 'd-none')

        $.ajax({
            async: false,
            url: dictFetchOptions.setUrlDir,
            type: dictFetchOptions.setMethod,
            contentType: 'application/json; charset=UTF-8',
            data: JSON.stringify({
                dirJsonRaw: JSON.stringify(appDictionaries)
            }),
            success: function () {
                ReRenderTable();
            },
            error: function (xhr, ajaxOptions, thrownError) {
                console.error(thrownError)
                appDictionaries = {};
            },
            complete: function () {
                RemoveClassToElement('.modal-footer> .btn > label', 'd-none')
                AddClassToElement('.modal-footer> .btn > i', 'd-none')
                EnableAllElementToDirEdit();
            }
        });
    })
}

/* Отключение активности у всех элементов у компонента редактирования справочников*/
function DisableAllElementToDirEdit() {
    AddClassToElement('.close', 'disabled-item');
    AddClassToElement('.modal-body', 'disabled-item');
    AddClassToElement('.modal-footer', 'disabled-item');
}

/* Включение активности у всех элементов у компонента редактирования справочников*/
function EnableAllElementToDirEdit() {
    RemoveClassToElement('.modal-body', 'disabled-item')
    RemoveClassToElement('.modal-footer', 'disabled-item');
    RemoveClassToElement('.close', 'disabled-item');
}

/**/

/*
* Добавление обработчика для переключения табов на странице  
*/
function AddTabsSelectorHandler(){
    document.querySelector(".tabs-selector").addEventListener('click',function(e){
        let element = e.target;
        if(element.classList.contains("active"))
            return;
        for (let aItem of  document.querySelectorAll("a.nav-link")) {
            if(aItem.classList.contains('active')){
                aItem.classList.remove('active');
            }
        }
        element.classList.add('active');
        for( let tab of document.querySelectorAll('.tab-item')){
            if(!tab.classList.contains('d-none')){
                tab.classList.add('d-none')
            }
        }
        let elementId = element.dataset.type;
        if (elementId) {
            document.querySelector(elementId).classList.remove('d-none')
        }
    });
}



/***********************************/