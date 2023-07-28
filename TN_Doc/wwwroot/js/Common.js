const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5010/SignalRApp")
    //.WithAutomaticReconnect()
    .build();

hubConnection.on("Receive", function (deviceName, tagName, tagValue) {
    if (deviceName == CurrentDeviceName) {

        if (tagName == GetFullNameTag('ARM.ARM_OnlineReportCounter'))
            GetDoc();
    }
});
hubConnection.start();

var PrefixTag = "IVK_TN_01";

var CurrentDeviceName;
var CurrentDeviceId;


var table = null;
var currentId = null;
var docTemplates = {};

let appDictionaries;
let dictFetchOptions;
let localStorageKeys;

/* Russian (UTF-8) initialisation for the jQuery UI date picker plugin. */
/* Written by Andrew Stromnov (stromnov@gmail.com). */
(function (factory) {
    "use strict";

    if (typeof define === "function" && define.amd) {

        // AMD. Register as an anonymous module.
        define(["../widgets/datepicker"], factory);
    } else {

        // Browser globals
        factory(jQuery.datepicker);
    }
})

(function (datepicker) {
    "use strict";
    datepicker.regional.ru = {
        closeText: "Закрыть",
        prevText: "&#x3C;Пред",
        nextText: "След&#x3E;",
        currentText: "Сегодня",
        monthNames: ["Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
            "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"],
        monthNamesShort: ["Янв", "Фев", "Мар", "Апр", "Май", "Июн",
            "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек"],
        dayNames: ["воскресенье", "понедельник", "вторник", "среда", "четверг", "пятница", "суббота"],
        dayNamesShort: ["вск", "пнд", "втр", "срд", "чтв", "птн", "сбт"],
        dayNamesMin: ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"],
        weekHeader: "Нед",
        dateFormat: "dd.mm.yy",
        firstDay: 1,
        isRTL: false,
        showMonthAfterYear: false,
        yearSuffix: ""
    };
    datepicker.setDefaults(datepicker.regional.ru);

    return datepicker.regional.ru;
});

$(document).ready
(
    function () {

    }
);

function InitDevices() {
    $.ajax(
        {
            async: false,
            url: "Home/GetListDevices",
            type: "GET",
            //dataType: "json",
            success: function (data) {
                data.forEach((item) => {
                    $('#ComboboxDevice').append('<option value=' + item.id + '>' + item.name + '</option>');
                });
            },
        });

    CurrentDeviceName = $("#ComboboxDevice :selected").text();
    CurrentDeviceId = $('#ComboboxDevice').val()

    $.ajax(
        {
            async: false,
            url: "Home/GetNameDBForDevice",
            type: "GET",
            //dataType: "json",
            success: function (data) {
                if (data == "IVK_TN_01")
                    PrefixTag = "IVK_TN_01";
                else
                    PrefixTag = "IVK_TN_02";
            },
        });

    $('#ComboboxDevice').change(function () {
        CurrentDeviceName = $("#ComboboxDevice :selected").text();
        CurrentDeviceId = $('#ComboboxDevice').val()

        $.ajax(
            {
                async: false,
                url: "Home/GetNameDBForDevice",
                type: "GET",
                //dataType: "json",
                success: function (data) {
                    if (data == "IVK_TN_01")
                        PrefixTag = "IVK_TN_01";
                    else
                        PrefixTag = "IVK_TN_02";
                },
            });

        InitDocs();
    });
}

function InitDocs() {

    $('#ComboboxDocGUID').empty();
    $('.FR').attr('src', '');

    if (table != null) $('#DataTable').DataTable().clear().draw();

    $.ajax(
        {
            async: false,
            url: "Home/GetListDocs",
            type: "GET",
            //dataType: "json",
            data:
                {
                    idDevice: $('#ComboboxDevice').val()
                },
            success: function (data) {
                data.forEach((item) => {
                    $('#ComboboxDocGUID').append('<option value=' + item.id + '>' + item.name + '</option>');
                });
            },
        });

//    $('#ComboboxDocGUID').change(function ()
//    {
//        if (table != null) $('#DataTable').DataTable().clear().draw();
//        //$('.FR').each(function () {
//        //    $(this).attr('src', '');
//        //});
//        $('.FR').attr('src', '');

//        InitTemplatesDoc();
//    });
}

function InitTemplatesDoc() {
    $('#ComboboxTemplateDoc').empty();

    var id = GetIdTemplateDoc();

    $.ajax(
        {
            async: false,
            url: "Home/GetTemplatesDoc",
            type: "GET",
            //dataType: "json",
            data:
                {
                    IdDevice: $('#ComboboxDevice').val(),
                    IdDoc: $('#ComboboxDocGUID').val(),
                },
            success: function (data) {
                data.forEach((item) => {
                    if (item.id == id)
                        $('#ComboboxTemplateDoc').append('<option value=' + item.id + ' selected>' + item.name + '</option>');
                    else
                        $('#ComboboxTemplateDoc').append('<option value=' + item.id + '>' + item.name + '</option>');
                });
            },
        });

//    $('#ComboboxTemplateDoc').change(function () {
//        SetPathTemplateDoc();
//    });
}

function SetIdTemplateDoc() {
    $.ajax(
        {
            async: false,
            url: "Home/SetIdTemplateDoc",
            type: "GET",
            //dataType: "json",
            data:
                {
                    IdDevice: $('#ComboboxDevice').val(),
                    IdDoc: $('#ComboboxDocGUID').val(),
                    IdTemplateDoc: $('#ComboboxTemplateDoc').val()
                }
        });
}

function GetIdTemplateDoc() {
    var ret = null;

    $.ajax(
        {
            async: false,
            url: "Home/GetIdTemplateDoc",
            type: "GET",
            //dataType: "json",
            data:
                {
                    IdDevice: $('#ComboboxDevice').val(),
                    IdDoc: $('#ComboboxDocGUID').val(),
                },
            success: function (data) {
                ret = data;
            }
        });
    return ret;
}

function InitPrinterName() {
    $('#ComboboxPrinterName').empty();

    $.ajax(
        {
            async: false,
            url: "Print/GetListPrinters",
            type: "GET",
            success: function (data) {
                data.forEach((item) => {
                    $('#ComboboxPrinterName').append('<option value=' + item + '>' + item + '</option>');
                });
            }
        });
}

function InitExportFormat() {
    $('#ComboboxExportFormat').empty();

    $.ajax(
        {
            async: false,
            url: "Export/GetListFormats",
            type: "GET",
            success: function (data) {
                data.forEach((item) => {
                    $('#ComboboxExportFormat').append('<option value=' + item + '>' + item + '</option>');
                });
            }
        });
}

function InitProtocolNumber() {
    $('#ComboboxProtocolNumber').empty();

    $.ajax(
        {
            async: false,
            url: "Home/GetListProtocolNumber",
            type: "GET",
            data:
                {
                    IdDevice: $('#ComboboxDevice').val(),
                    IdDoc: $('#ComboboxDocGUID').val(),
                },
            success: function (data) {
                data.forEach((item) => {
                    $('#ComboboxProtocolNumber').append('<option value=' + item.id + '>' + item.name + '</option>');
                });
            }
        });
}

function InitDatepickerBegin() {
    var dt = moment();
    var dtBegin = new Date(dt.format());

    $('#DatepickerBegin').datepicker(
        {
            dateFormat: 'dd.mm.yy'
        });
    $('#DatepickerBegin').datepicker("setDate", dtBegin);
}

function InitDatepickerEnd() {
    var dt = moment();
    var dtEnd = new Date(dt.add(1, 'days').format());

    $('#DatepickerEnd').datepicker(
        {
            dateFormat: 'dd.mm.yy'
        });
    $('#DatepickerEnd').datepicker("setDate", dtEnd);
}

function InitTableDocs() {
    table = $('#DataTable').DataTable(
        {
            select: true,
            scrollY: 600,
            scrollCollapse: true,
            paging: false,

            oLanguage: {
                sSearch: "Поиск",
                sEmptyTable: "Отсутствуют данные в таблице"
            },
            info: false,

            ajax: function (data, callback, settings) {
                callback
                (
                    GetData()
                );
            },

            columns:
                [
                    {data: 'dt'},
                    //{ data: function (data) { return moment(data.dt, "DD-MM-YYYYTHH:mm").format("DD.MM.YYYY HH:mm"); } },
                    {data: 'description'}
                ]
        });

    table.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            var id = table.rows(indexes).data().pluck('id');
            currentId = id[0];

            if ($('#ComboboxDocGUID').val() == 32) {
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport_BIKId'), 1, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_OnlineReportType'), currentId, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport'), true, 2, 0);
            } else {
                GetDoc();
            }
        }
    });
}

function InitElement() {
    dictFetchOptions = {
        getUrl: "/direditor/getdir",
        getMethod: "GET",
        setUrl: "/direditor/setdir",
        setMethod: "POST"
    }
    localStorageKeys = {
        dictionariesCache: "appDict"
    }


    LoadAppDictionaries();
    AddDitctionariesHandler();
    RenderUserGroupsRowTable();
    RenderAndAddHandlerLicencesTable();
    RenderAndAddHandlerUserTable();

    InitDevices();
    InitDocs();

    InitDatepickerBegin();
    InitDatepickerEnd();

    InitTableDocs();

    InitTemplatesDoc();
    InitPrinterName();
    InitExportFormat();
    InitProtocolNumber();

    $('#ComboboxDocGUID').change(function () {
        if (table != null) $('#DataTable').DataTable().clear().draw();
        //$('.FR').each(function () {
        //    $(this).attr('src', '');
        //});
        $('.FR').attr('src', '');

        InitTemplatesDoc();
        InitProtocolNumber();
    });

    $('#ComboboxTemplateDoc').change(function () {
        SetIdTemplateDoc();
        GetDoc();
    });

    $('#ComboboxProtocolNumber').change(function () {
        GetDoc();
    });


}

function GetData() {
    var ret = null;

    var DTBegin = $('#DatepickerBegin').datepicker('getDate');
    var DTEnd = $('#DatepickerEnd').datepicker('getDate');

    if (DTBegin == null || DTEnd == null) {
    } else {
        var strDTBegin = DTBegin.getDate() + '.' + (DTBegin.getMonth() + 1) + '.' + DTBegin.getFullYear();
        var strDTEnd = DTEnd.getDate() + '.' + (DTEnd.getMonth() + 1) + '.' + DTEnd.getFullYear();
    }

    //alert('1: ' + 'ComboboxDocGUID' + $('#ComboboxDocGUID').val());

    $.ajax(
        {
            async: false,
            url: "Home/GetList",
            type: "GET",
            data:
                {
                    IdDevice: $('#ComboboxDevice').val(),
                    IdDoc: $('#ComboboxDocGUID').val(),
                    DTBegin: strDTBegin,
                    DTEnd: strDTEnd
                },
            beforeSend: function (data) {
            },
            success: function (data) {
                //var table = $('#example');
                //table.data = data.tableReport;
                //table.ajax.reload();

                ret = data;
            },
            error: function (xhr, ajaxOptions, thrownError) {
                // alert("Ошибка!");
            },
            complete: function (data) {
                if ($('#ComboboxDocGUID').val() == 0 ||
                    $('#ComboboxDocGUID').val() == 32) {
                    $('#ButtonSave').prop('disabled', true);
                    $('#ButtonReview').prop('disabled', true);
                    $('#ButtonEdit').prop('disabled', true);
                } else {
                    $('#ButtonSave').prop('disabled', false);
                    $('#ButtonReview').prop('disabled', false);
                    $('#ButtonEdit').prop('disabled', false);
                }
            }
        });

    var data = {"data": ret};

    return data;
}

function GetDoc() {
    $('.FR').attr('src', '');

    //var x = 10;// Math.random() * 10;

    //var PDFObject = new PDFObject({
    //    url: '/PDF/PDF.pdf',
    //    pdfOpenParams: {
    //        view: 'Fit',
    //        scrollbars: '0',
    //        toolbar: '0',
    //        statusbar: '0',
    //        navpanes: '0'
    //    }
    //});

    $.ajax(
        {
            async: true,
            url: "Home/GetDoc",
            type: "GET",
            data: {
                IdDevice: $('#ComboboxDevice').val(),
                IdDoc: $('#ComboboxDocGUID').val(),
                id: currentId,
                protocolNumber: $('#ComboboxProtocolNumber').val()
            },
            success: function (data) {
                //$('.FR').each(function () {
                //    //#toolbar=0&view=FitH
                //    $(this).attr('src', '/PDF/PDF.pdf');
                //});

                //$('.FR').attr('src', '/PDF/PDF.pdf#toolbar=0&id=' + x);
                $('.FR').attr('src', '/PDF/PDF.pdf#toolbar=0&view=FitH');

                //PDFObject.embed("/PDF/PDF.pdf", ".FR");                
            },
        });
}

function GetEditDoc() {
    //var device = GuidDevice: $('#ComboboxDevice').val();
    //var guidDoc = GuidDoc: $('#ComboboxDocGUID').val();
    //var id = table.rows(indexes).data().pluck('id');

    $.ajax(
        {
            async: false,
            url: "Home/GetDocEdit",
            type: "GET",
            data: {
                IdDevice: $('#ComboboxDevice').val(),
                IdDoc: $('#ComboboxDocGUID').val(),
                id: currentId
            },
            success: function (data) {
                $('.FR').each(function () {
                    $(this).attr('src', '/HTML/html.html');
                });
                //$('.FR').html(data);

                //alert(data);
            }
        });
}

function SaveDoc() {
    document.getElementsByClassName('FR')[0].contentWindow.SaveDoc(
        $('#ComboboxDevice :selected').text(),
        $('#ComboboxDevice').val(),
        $('#ComboboxDocGUID').val(),
        currentId,
        PrefixTag);
}

function PrintDoc() {
    $.ajax(
        {
            url: "Print/PrintDoc",
            type: "GET",
            data: {
                printerName: $('#ComboboxPrinterName').val()
            }
        });
}

function ExportDoc() {
    $.ajax(
        {
            url: "Home/ExportDoc",
            type: "GET",
            data: {
                IdDevice: $('#ComboboxDevice').val(),
                IdDoc: $('#ComboboxDocGUID').val(),
                id: currentId,
                format: $('#ComboboxExportFormat').val()
            },
            success: function (data) {

                alert('Экспорт успешно завершен!\nФайл:' + data);
            }
        });
}

function GetValueCombobox() {
    //var obj = $("#Combobox :selected").text();
    var obj = $('#Combobox').val()
    return obj;

    $.ajax(
        {
            url: "Home/SaveDoc",
            type: "POST",
            dataType: 'json',
            data: {
                IdDoc: $('#ComboboxDocGUID').val(),
                data: JSON.stringify(result)
            },
            success: function (data) {
            },
        });

}

function WriteTag(GuidDevice, tagName, valueTag, namespaceIndex = 2, indexArray = 3) {
    var url = "http://localhost:5010/api/Values/";
    var result;
    $.ajax(
        {
            async: false,
            url: url,
            type: "PUT",
            contentType: 'application/json; charset=UTF-8',
            data: JSON.stringify(
                {
                    "DeviceName": GuidDevice,
                    "NameTag": tagName,
                    "ValueTag": valueTag,
                    "NamespaceIndex": namespaceIndex,
                    "IndexArray": indexArray
                })
        });

    return result;
}

function GetFullNameTag(tagName) {
    return PrefixTag + '.' + tagName;
}


/***********************************/

/*
* Загрузка словарей приложения. Словари поступают в формате json.
* Сами словари хранятся в поле 'dirJsonRaw' (datas['dirJsonRawS']).
* Словари сохраняются в объекте 'appDictionaries'.
* В случае появления ошибки 'appDictionaries' инициализруется пустым объектом
* ,а в консоль пишется ошибка
*  Дополнительно словари записываются в локальное хранилище
* */
function LoadAppDictionaries() {
    let dictStr = localStorage.getItem(localStorageKeys.dictionariesCache)

    if (dictStr) {
        appDictionaries = JSON.parse(dictStr);
        return;
    }

    $.ajax({
        async: false,
        url: dictFetchOptions.getUrl,
        type: dictFetchOptions.getMethod,
        success: function (data) {
            appDictionaries = JSON.parse(data['dirJsonRaw'])
            localStorage.setItem(localStorageKeys.dictionariesCache, data['dirJsonRaw'])
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.error(thrownError);
            appDictionaries = {};
            localStorage.removeItem(localStorageKeys.dictionariesCache)
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
        row.appendChild(idTd);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(userGroup['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        row.appendChild(usedTd);

        let nameTD = document.createElement('td');
        nameTD.innerText = userGroup['Name'].toString();
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
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(licences['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        row.appendChild(usedCell);

        let numberCell = document.createElement('td');
        numberCell.innerText = licences['LicensesNumber'];
        row.appendChild(numberCell);

        let dateCell = document.createElement('td');
        dateCell.innerText = licences['LicensesDate'];
        row.appendChild(dateCell);

        let actionCell = document.createElement('td');
        actionCell.appendChild(CreateEditLicensesButton('fa:fa-lock:edit-licences-btn', 'btn:btn-outline-primary:edit-licences-btn', '5px', 'Licenses'));
        actionCell.appendChild(CreateDeleteButton('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px', 'Licenses'));
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
    console.log(usersGroups);
    for (let user of appDictionaries['Users']) {

        let row = document.createElement('tr')
        row.classList.add('data-row')

        let idCell = document.createElement('td');
        idCell.innerText = user['Id'];
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(user['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        row.appendChild(usedCell);

        let groupNameCell = document.createElement('td');
        groupNameCell.innerText = usersGroups.filter(group => group['Id'] === user['IdGroup'])[0]['Name'];
        row.appendChild(groupNameCell);

        let surnameCell = document.createElement('td');
        surnameCell.innerText = user.F;
        row.appendChild(surnameCell);

        let nameCell = document.createElement('td');
        nameCell.innerText = user.I;
        row.appendChild(nameCell);

        let patronymicCell = document.createElement('td');
        patronymicCell.innerText = user.O;
        row.appendChild(patronymicCell);

        let organizationCell = document.createElement('td');
        organizationCell.innerText = user['Factory'];
        row.appendChild(organizationCell);

        let postCell = document.createElement('td');
        postCell.innerText = user['Post'];
        row.appendChild(postCell);

        table.append(row)
    }
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
* Создание кнопки удаления из списка
*/
function CreateDeleteButton(faClass, buttonClass, margin, arrayName) {

    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
    btn.addEventListener('click', function (e) {
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
    });
    return btn
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
        let rowMap = {
            0: 'ignore',
            1: 'bool',
            2: 'text',
            3: 'date',
            4: 'ignore'
        }
        if (itemBtn.dataset.mode === 'stable') {
            DisableClosestDeleteBtn(itemId)
            DisableListDictionaries()
            DisableSaveButton();
            DisableCloseButton();
            DisableOtherTableRows(itemId, 'licences-table');
            ConvertStableRowToEditRow(rowItem, rowMap)
            ChangeButtonIcon(itemBtn,'fa-unlock','fa-lock');
            itemBtn.dataset.mode = 'edit';
        } else if (itemBtn.dataset.mode === 'edit') {
            ChangeButtonIcon(itemBtn,'fa-lock','fa-unlock');
            ConvertEditRowToStableRow(rowItem, rowMap)
            EnableClosestDeleteBtn(itemId)
            EnableListDictionaries();
            EnableSaveButton();
            EnableCloseButton();
            EnableOtherTableRows(itemId, 'licences-table');
            itemBtn.dataset.mode = 'stable';
        }
    });
    return btn
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
    for (let i = 0; i < cells.length; i++) {
        ConvertEditCellToStableCell(cells[i], rowMap[i]);
    }
}

/*
* Конверитирование ячейки редактирования в ячейку стабильную 
*/
function ConvertEditCellToStableCell(cell, type) {
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
        default:
            break
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
            newElement.value = prText;
            cell.replaceChild(newElement, previewNode)
            break;
        case 'date':
            let prDate = new Date(moment(cell.innerText, 'DD.MM.YYYY').format())
            newElement.classList.add('calendar');
            cell.replaceChild(newElement, previewNode);
            $('.calendar').datepicker({dateFormat: 'dd.mm.yy'});
            $('.calendar').datepicker('setDate', prDate);
            break;
        default:
            break
    }


}

/*
* Отключение  других строк
*/
function DisableOtherTableRows(ignoredItemId, tableClass) {
    for (let t of document.querySelectorAll('.dir-item > .table')) {
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
    for (let t of document.querySelectorAll('.dir-item > .table')) {

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
* Отключение кнопки сохранения
*/
function DisableSaveButton() {
    document.querySelector('.save-btn').classList.add('disabled-item');
}

/*
* Включение кнопки сохранения
*/
function EnableSaveButton() {
    document.querySelector('.save-btn').classList.remove('disabled-item');
}

/*
* Отключение кнопки закрытия окошка
*/
function DisableCloseButton() {
    document.querySelector('.close').classList.add('disabled-item');
}

/*
* Включение  кнопки закрытия окошка
*/
function EnableCloseButton() {
    document.querySelector('.close').classList.remove('disabled-item');
}

/*
* Отключение ближайшей кнопки удаления строки 
*/
function DisableListDictionaries() {
    document.querySelector('#dictionaries-list').classList.add('disabled-item');
}

/*
* Включение списка выбора словарей
*/
function EnableListDictionaries() {
    document.querySelector('#dictionaries-list').classList.remove('disabled-item');
}

/*
* Отключение ближайшей кнопки удаления строки 
*/
function DisableClosestDeleteBtn(itemId) {
    document.querySelector('tr[data-id="' + itemId + '"] td button.delete-btn').classList.add('disabled-item');
}

/*
* Включение ближайшей кнопки удаления строки 
*/
function EnableClosestDeleteBtn(itemId) {
    document.querySelector('tr[data-id="' + itemId + '"] td button.delete-btn').classList.remove('disabled-item');
}

/***********************************/