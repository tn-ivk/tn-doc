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

//DataTable.datetime('DD.MM.YYYY HH:mm');

var IsUsedElis = true;

var PrefixTag = "IVK_TN_01";

var CurrentDeviceName;
var CurrentDeviceId;


var table = null;
var currentId = null;
var docTemplates = {};

let appDictionaries;
let qpCfgsDictionaries
let dictFetchOptions;

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
            url: 'Home/GetListDevices',
            type: 'GET',
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

    $.ajax(
        {
            async: false,
            url: "Home/IsUsedElis",
            type: "GET",
            //dataType: "json",
            data:
                {
                    idDevice: $('#ComboboxDevice').val()
                },
            success: function (data) {
                IsUsedElis = data;
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

        $.ajax(
            {
                async: false,
                url: "Home/IsUsedElis",
                type: "GET",
                //dataType: "json",
                data:
                    {
                        idDevice: $('#ComboboxDevice').val()
                    },
                success: function (data) {
                    IsUsedElis = data;
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
            url: 'Home/GetListDocs',
            type: 'GET',
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
            url: 'Home/GetTemplatesDoc',
            type: 'GET',
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
            url: 'Home/SetIdTemplateDoc',
            type: 'GET',
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
            url: 'Home/GetIdTemplateDoc',
            type: 'GET',
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
            accept: "application/json",
            type: "GET",
            success: function (data) {
                data.forEach((item) => {
                    $('#ComboboxPrinterName').append('<option value=' + item.replaceAll(" ", "_") + '>' + item + '</option>');
                });
            }
        });
}

function InitExportFormat() {
    $('#ComboboxExportFormat').empty();

    $.ajax(
        {
            async: false,
            url: 'Export/GetListFormats',
            type: 'GET',
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
            url: 'Home/GetListProtocolNumber',
            type: 'GET',
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
    $('#DatepickerBegin').datepicker('setDate', dtBegin);
}

function InitDatepickerEnd() {
    var dt = moment();
    var dtEnd = new Date(dt.add(1, 'days').format());

    $('#DatepickerEnd').datepicker(
        {
            dateFormat: 'dd.mm.yy'
        });
    $('#DatepickerEnd').datepicker('setDate', dtEnd);
}

function InitTableDocs() {

    table = $('#DataTable').DataTable(
        {
            select: true,
            scrollY: 600,
            scrollCollapse: true,
            paging: false,

            oLanguage: {
                sSearch: 'Поиск',
                sEmptyTable: 'Отсутствуют данные в таблице'
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
        getUrlDir: '/direditor/getdir',
        setUrlDir: '/direditor/setdir',
        getQpCfg: '/direditor/getqpconfigs',
        setQpCfg: '/direditor/setqpconfigs',
        getMethod: 'GET',
        setMethod: 'POST'
    }


    /*init dir editor component script*/


    AddTabsSelectorHandler();

    LoadAppDictionaries();
    LoadQPConfigsDictionaries();
    AddDitctionariesHandler();
    RenderUserGroupsRowTable();
    RenderAndAddHandlerLicencesTable();
    RenderAndAddHandlerUserTable();
    AddLicHandler();
    AddUserHandler();
    AddSaveButtonHandler();

    RenderQpConfigs();
    /*end*/


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
            url: 'Home/GetList',
            type: 'GET',
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
                    $('#ComboboxDocGUID').val() == 3 ||
                    $('#ComboboxDocGUID').val() == 32) {
                    $('#ButtonSave').prop('disabled', true);
                    $('#ButtonReview').prop('disabled', true);
                    $('#ButtonEdit').prop('disabled', true);
                } else {
                    $('#ButtonSave').prop('disabled', false);
                    $('#ButtonReview').prop('disabled', false);
                    $('#ButtonEdit').prop('disabled', false);
                }

                if ($('#ComboboxDocGUID').val() == 1) {
                    $('#ButtonElis').prop('hidden', !IsUsedElis);
                } else
                    $('#ButtonElis').prop('hidden', true);
            }
        });

    var data = {'data': ret};

    return data;
}

function GetDoc() {
    $('.FR').attr('src', '');

    $.ajax(
        {
            async: true,
            url: 'Home/GetDoc',
            type: 'GET',
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

                if (data)
                    $('.FR').attr('src', '/PDF/PDF.pdf#toolbar=0&view=FitH');

                //PDFObject.embed("/PDF/PDF.pdf", ".FR");                
            },
        });
}

function GetEditDoc() {

    $.ajax(
        {
            async: false,
            url: 'Home/GetDocEdit',
            type: 'GET',
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
            url: 'Print/PrintDoc',
            type: 'GET',
            data: {
                printerName: $('#ComboboxPrinterName').val().replaceAll("_", " "),
            }
        });
}

function ExportDoc() {
    $.ajax(
        {
            url: 'Home/ExportDoc',
            type: 'GET',
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
            url: 'Home/SaveDoc',
            type: 'POST',
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
    var url = 'http://localhost:5010/api/Values/';
    var result;
    $.ajax(
        {
            async: false,
            url: url,
            type: 'PUT',
            contentType: 'application/json; charset=UTF-8',
            data: JSON.stringify(
                {
                    'DeviceName': GuidDevice,
                    'NameTag': tagName,
                    'ValueTag': valueTag,
                    'NamespaceIndex': namespaceIndex,
                    'IndexArray': indexArray
                })
        });

    return result;
}

function GetFullNameTag(tagName) {
    return PrefixTag + '.' + tagName;
}


//Получить данные из ЕЛИС
function GetElisData() {

    var clientToken = GetClientToken(CurrentDeviceId);

    if (clientToken == '') {

        var SiknNumber = GetSiknNumber(CurrentDeviceId);

        if (SiknNumber == '')
            return;
        else
            clientToken = RegistrationClient(SiknNumber);
    }

    $.ajax(
        {
            async: false,
            url: 'http://localhost:5050/api/tspd/getqp',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            headers: {
                'client-token': clientToken
            },
            data: JSON.stringify({
                startPeriod: '2023-08-14T09:14:49.345Z',
                endPeriod: '2023-08-14T09:14:49.345Z'
            }),
            success: function (data) {

            },
            error: function (data) {

                $('#info').text(data.statusText);

                //Неавторизованный пользователь
                if (data.status == 401) {
                    RegistrationClient('ИВК-1');
                }
            }
        });
}

//Зарегистрировать устройство для ЕЛИС
function RegistrationClient(nameDevice) {

    $.ajax(
        {
            async: false,
            url: 'http://localhost:5050/api/client/signin',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            data: JSON.stringify({
                siknNumber: nameDevice
            }),
            success: function (data) {

            },
            error: function (data) {

            }
        });
}

//Зарегистрация устройства для взаимодействия с ТСПД
function GetElisCurrentGuidForDevice() {

}

//
function GetSiknNumber(idDevice) {
    $.ajax(
        {
            async: false,
            url: 'Home/GetSiknNumber',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            data: JSON.stringify({
                IdDevice: idDevice
            }),
            success: function (data) {

            },
            error: function (data) {

            }
        });
}

//Получить GUID для устройства
function GetClientToken(idDevice) {
    $.ajax(
        {
            async: false,
            url: 'Home/GetClientToken',
            type: 'GET',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            data: JSON.stringify({
                IdDevice: idDevice
            }),
            success: function (data) {

            },
            //error: function (data) {

            //}
        });
}

//Сохранить GUID для устройства
function SetClientToken() {

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
    VerticalCenteringText(el);
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

/**********************************************/
/*************Хелперы*************************/
/**********************************************/


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
        VerticalCenteringText(idTd)
        row.appendChild(idTd);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(userGroup['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        VerticalCenteringText(usedTd)
        row.appendChild(usedTd);

        let nameTD = document.createElement('td');
        nameTD.innerText = userGroup['Name'].toString();
        VerticalCenteringText(nameTD)
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
        VerticalCenteringText(idCell);
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(licences['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        VerticalCenteringText(usedCell);
        row.appendChild(usedCell);

        let numberCell = document.createElement('td');
        numberCell.innerText = licences['LicensesNumber'];
        VerticalCenteringText(numberCell);
        row.appendChild(numberCell);

        let dateCell = document.createElement('td');
        dateCell.innerText = licences['LicensesDate'];
        VerticalCenteringText(dateCell);
        row.appendChild(dateCell);

        let editDivElement = document.createElement('div');
        editDivElement.appendChild(CreateEditLicensesButton('fa:fa-lock:edit-licences-btn', 'btn:btn-outline-primary:edit-licences-btn', '5px'))

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
        VerticalCenteringText(idCell);
        row.appendChild(idCell);

        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(user['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedCell = document.createElement('td');
        usedCell.appendChild(usedSquare);
        VerticalCenteringText(usedCell);
        row.appendChild(usedCell);

        let groupNameCell = document.createElement('td');
        groupNameCell.innerText = usersGroups.filter(group => group['Id'] === user['IdGroup'])[0]['Name'];
        VerticalCenteringText(groupNameCell);
        row.appendChild(groupNameCell);

        let surnameCell = document.createElement('td');
        surnameCell.innerText = user.F;
        VerticalCenteringText(surnameCell);
        row.appendChild(surnameCell);

        let nameCell = document.createElement('td');
        nameCell.innerText = user.I;
        VerticalCenteringText(nameCell);
        row.appendChild(nameCell);

        let patronymicCell = document.createElement('td');
        patronymicCell.innerText = user.O;
        VerticalCenteringText(patronymicCell);
        row.appendChild(patronymicCell);

        let organizationCell = document.createElement('td');
        organizationCell.innerText = user['Factory'];
        VerticalCenteringText(organizationCell);
        row.appendChild(organizationCell);

        let postCell = document.createElement('td');
        postCell.innerText = user['Post'];
        VerticalCenteringText(postCell);
        row.appendChild(postCell);

        let licCell = document.createElement('td');
        VerticalCenteringText(licCell);
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

function VerticalCenteringText(cell) {
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
function CreateEditLicensesButton(faClass, buttonClass, margin) {
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
        AddClassToElement('.modal-header', 'disabled-item')
        DisableOtherTableRows(itemId, 'users-table');
        ConvertStableRowToEditRow(rowItem, rowMap)
        ChangeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
        itemBtn.dataset.mode = 'edit';
    } else if (itemBtn.dataset.mode === 'edit') {
        rowMap[6] = "ignore";
        rowMap[7] = "ignore";
        if (!ValidateEditRow(rowItem, rowMap))
            return;
        rowMap[6] = "text";
        rowMap[7] = "text";
        ChangeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
        ConvertEditRowToStableRow(rowItem, rowMap)
        RemoveClassToElement('.table-bottom-menu', 'disabled-item')
        RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item')
        RemoveClassToElement('#dictionaries-list', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('.close', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item')
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
        AddClassToElement('.modal-header', 'disabled-item')
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
        RemoveClassToElement('.modal-header', 'disabled-item')
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
    console.log(qpCfgsDictionaries['QpsInfo'][Number(row.closest('table').dataset.qpId)]['Methods']);
    let qpMethodsArray = qpCfgsDictionaries['QpsInfo'][Number(row.closest('table').dataset.qpId)]['Methods'];
    let qpParametersArray = qpCfgsDictionaries['QpsInfo'][Number(row.closest('table').dataset.qpId)]['Parameters'];

    let userId = Number(row.dataset.id);
    for (let i = 0; i < cells.length; i++) {
        ConvertEditCellToStableCell(cells[i], rowMap[i], usersGroupArray, licensesArray, qpMethodsArray, qpParametersArray);
    }
}

/*
* Конверитирование ячейки редактирования в ячейку стабильную 
*/
function ConvertEditCellToStableCell(cell, type, usersGroupArray, licensesArray, methodsArray, parametersArray) {
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
            let newNumNode = document.createTextNode(Intl.NumberFormat('ru').format(previewNode.value));
            cell.replaceChild(newNumNode, cell.childNodes[0])
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
            if (previewNode)
                cell.replaceChild(cbEl, previewNode)
            else
                cell.append(cbEl);
            break;
        case 'number':
            let prNumber = cell.innerText;
            newElement.type = 'number';
            newElement.value = prNumber ? prNumber : '0,0';
            if (previewNode)
                cell.replaceChild(newElement, previewNode)
            else
                cell.append(newElement);
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
function ReRenderTable() {
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
    AddClassToElement('.modal-header', 'disabled-item');
}

/* Включение активности у всех элементов у компонента редактирования справочников*/
function EnableAllElementToDirEdit() {
    RemoveClassToElement('.modal-body', 'disabled-item')
    RemoveClassToElement('.modal-footer', 'disabled-item');
    RemoveClassToElement('.close', 'disabled-item');
    RemoveClassToElement('.modal-header', 'disabled-item');
}

/**/

/*
*   Загрузка конфигураций справочников для редактирования
*   методов и параметров
*/
function LoadQPConfigsDictionaries() {
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
* Добавление обработчика для переключения табов на странице  
*/
function AddTabsSelectorHandler() {
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


function RenderQpConfigs() {
    let qpList = document.querySelector('#qp-list');
    let counter = 0;
    for (let qp of qpCfgsDictionaries["QpsInfo"]) {
        let liItem = document.createElement('li');
        liItem.classList.add("list-group-item");
        liItem.textContent = qp["Name"];
        liItem.dataset.target = "#qpId" + counter;
        qpList.append(liItem)
        let mainBaseDiv = RenderMainDivForQpTables(counter);
        RenderQpConfigsMethodsTable(counter, qp, mainBaseDiv)
        //todo: повесить обработчки на ul  а не на каждую li
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
* Рендеринг главного дива для рендеринга паспортов качества 
*/
function RenderMainDivForQpTables(id) {
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
    return tbScrollDiv;
}

/*
* Рендеринг методов паспортов качества 
*/
function RenderQpConfigsMethodsTable(counter, qps, baseDiv) {
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
        idCell.innerHTML = method['Id'];
        row.dataset.id = method['Id'];
        VerticalCenteringText(idCell);
        row.appendChild(idCell);
        let usedSquare = document.createElement('i')
        usedSquare.classList.add('fa');
        usedSquare.classList.add(method['Use'] === true ? 'fa-check-square-o' : 'fa-square-o');
        usedSquare.ariaHidden = true;
        let usedTd = document.createElement('td');
        usedTd.appendChild(usedSquare);
        VerticalCenteringText(usedTd)
        row.appendChild(usedTd);
        let methodName = document.createElement('td');
        methodName.innerHTML = method['Name'];
        VerticalCenteringText(methodName);
        row.appendChild(methodName);
        let paramsArray = qps["Parameters"];
        let param = paramsArray.filter(item => item["Id"] === method["IdParameter"])[0];
        let paramCell = document.createElement('td')
        VerticalCenteringText(paramCell)
        paramCell.innerHTML = param ? param["Name"] : " - ";
        paramCell.dataset.paramId = param ? param["Id"] : 0;
        row.appendChild(paramCell);
        let limitActive = document.createElement('i')
        limitActive.classList.add('fa');
        limitActive.classList.add(method['LimitValueActive'] === true ? 'fa-check-square-o' : 'fa-square-o');
        limitActive.ariaHidden = true;
        let limitActiveCell = document.createElement('td');
        limitActiveCell.appendChild(limitActive);
        VerticalCenteringText(limitActiveCell)
        row.appendChild(limitActiveCell);
        let LimitValueCell = document.createElement('td');
        LimitValueCell.innerHTML = method['LimitValue'];
        VerticalCenteringText(LimitValueCell);
        row.appendChild(LimitValueCell);
        let LimitValueStringCell = document.createElement('td');
        LimitValueStringCell.innerHTML = !method['LimitValueString'] ? '-' : method['LimitValueString'];
        VerticalCenteringText(LimitValueStringCell);
        row.appendChild(LimitValueStringCell);
        let actionCell = document.createElement('td');
        actionCell.appendChild(_createEditQpMethodsBtn('fa:fa-lock:edit-user-btn', 'btn:btn-outline-primary:edit-methods-btn', '5px'));
        actionCell.appendChild(_createDeleteQpMethodsBtn('fa:fa-trash:delete-btn', 'btn:btn-outline-danger:delete-btn', '5px'));
        VerticalCenteringText(actionCell);
        row.appendChild(actionCell)
        methodsTable.appendChild(row);
    }
}


/*
    Создание кнопки удаления методов из паспортов качества
*/
function _createDeleteQpMethodsBtn(faClass, buttonClass, margin) {
    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
    btn.addEventListener('click', _deleteQpMethodsBtnHandler)
    let div = document.createElement('div')
    div.appendChild(btn);
    return div;
}

/*
    Создание кнопки редактирования методов
*/
function _createEditQpMethodsBtn(faClass, buttonClass, margin) {
    let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
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

    if (item.dataset.mode === 'stable') {
        ChangeButtonIcon(item, 'fa-unlock', 'fa-lock');
        AddClassToElement('#qp-list', 'disabled-item');
        AddClassToElement('.modal-header', 'disabled-item');
        AddClassToElement('.save-btn', 'disabled-item');
        AddClassToElement('row', 'disabled-item');
        AddClassToElement('tr[data-id="' + Number(row.dataset.id) + '"] td button.delete-btn', 'disabled-item');
        _disableOtherRowsInTable(item.closest('table'), Number(row.dataset.id));
        ConvertStableRowToEditRow(row, rowMap);
        item.dataset.mode = 'edit';
    } else {
        if (!ValidateEditRow(row, rowMap))
            return;
        ConvertEditRowToStableRow(row, rowMap)
        ChangeButtonIcon(item, 'fa-lock', 'fa-unlock');
        RemoveClassToElement('#qp-list', 'disabled-item');
        RemoveClassToElement('.modal-header', 'disabled-item');
        RemoveClassToElement('.save-btn', 'disabled-item');
        RemoveClassToElement('tr[data-id="' + Number(row.dataset.id) + '"] td button.delete-btn', 'disabled-item');
        _enableOtherRowsInTable(item.closest('table'), Number(item.closest('tr').dataset.id));
        _applyQpMethodsChanged(row,Number(row.dataset.id),Number (item.closest('table').dataset.qpId));
        item.dataset.mode = 'stable';
    }
}

function _applyQpMethodsChanged(rowItem, itemId, qpId) {
    // console.log(rowItem)
    // console.log(itemId)
    // console.log(qpId)
    //
    // // if(rowItem ==)
    // // if (!rowItem || !itemId || !qpId) {
    // //     console.log(rowItem)
    // //     console.log(itemId)
    // //     console.log(qpId)
    // //     return;
    // // }
    //    
    //
    // console.log('start')
    //
    // let methodIndex=qpCfgsDictionaries["QpsInfo"][qpId]["Methods"].findIndex(item => item['Id'] === itemId);
    // if(methodIndex<0) 
    //     return;
    // let cells = rowItem.cells;
    // let updatedObject = qpCfgsDictionaries["QpsInfo"][qpId]["Methods"][methodIndex];
    //
    // updatedObject['Use'] = cells[1].childNodes[0].classList.contains('fa-check-square-o');
    // updatedObject['Name'] = cells[2].childNodes[0].textContent;
    // updatedObject['IdParameter'] =  qpCfgsDictionaries["QpsInfo"][qpId]["Parameters"].filter(item => item["Name"]=cells[3].childNodes[0].textContent)[0]['Id'];
    // updatedObject['LimitValueActivate'] = cells[4].childNodes[0].classList.contains('fa-check-square-o');
    // updatedObject['LimitValue'] = Number( cells[5].childNodes[0].textContent);
    //
    // let msg = cells[5].childNodes[0].textContent;
    // if(!msg){
    //     updatedObject['LimitValueString'] = '-'
    // }else{
    //     updatedObject['LimitValueString'] =msg;
    // }
}



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


// function CreateEditUsersButton(faClass, buttonClass, margin) {
//     let btn = CreateWithOnlyImgButton(faClass, buttonClass, margin);
//     btn.dataset.mode = 'stable';
//     btn.addEventListener('click', function (e) {
//         let itemBtn;
//         if (e.target.tagName === 'I') {
//             itemBtn = e.target.closest('button')
//         } else {
//             itemBtn = e.target;
//         }
//         let rowItem = itemBtn.closest('tr')
//         let itemId = Number(rowItem.dataset.id);
//         if (!itemId) return;
//         EditSelectedUser(itemBtn, rowItem, itemId)
//     });
//     return btn
// }

/*
* Редактирование выбранной доверенности в таблице.
* @param itemBtn - кнопка по которой нажали. Кнопка должна находиться в редактируемой строке  таблице.
* @param rowItem - редактируемая строка в таблице.
* @param itemId - id объекта в массиве.
*/
// function EditSelectedUser(itemBtn, rowItem, itemId) {
//     let rowMap = {
//         0: 'ignore',
//         1: 'bool',
//         2: 'combobox-ug',
//         3: 'text',
//         4: 'text',
//         5: 'text',
//         6: 'text',
//         7: 'text',
//         8: 'combobox-lic',
//         9: 'ignore'
//     }
//
//     if (itemBtn.dataset.mode === 'stable') {
//         AddClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item');
//         AddClassToElement('#dictionaries-list', 'disabled-item');
//         AddClassToElement('.save-btn', 'disabled-item');
//         AddClassToElement('.close', 'disabled-item');
//         AddClassToElement('.table-bottom-menu', 'disabled-item')
//         AddClassToElement('.modal-header', 'disabled-item')
//         DisableOtherTableRows(itemId, 'users-table');
//         ConvertStableRowToEditRow(rowItem, rowMap)
//         ChangeButtonIcon(itemBtn, 'fa-unlock', 'fa-lock');
//         itemBtn.dataset.mode = 'edit';
//     } else if (itemBtn.dataset.mode === 'edit') {
//         rowMap[6] = "ignore";
//         rowMap[7] = "ignore";
//         if (!ValidateEditRow(rowItem, rowMap))
//             return;
//         rowMap[6] = "text";
//         rowMap[7] = "text";
//         ChangeButtonIcon(itemBtn, 'fa-lock', 'fa-unlock');
//         ConvertEditRowToStableRow(rowItem, rowMap)
//         RemoveClassToElement('.table-bottom-menu', 'disabled-item')
//         RemoveClassToElement('tr[data-id="' + itemId + '"] td button.delete-btn', 'disabled-item')
//         RemoveClassToElement('#dictionaries-list', 'disabled-item');
//         RemoveClassToElement('.save-btn', 'disabled-item');
//         RemoveClassToElement('.close', 'disabled-item');
//         RemoveClassToElement('.modal-header', 'disabled-item')
//         ApplyUserChanges(rowItem, itemId)
//         EnableOtherTableRows(itemId, 'users-table');
//         itemBtn.dataset.mode = 'stable';
//     }
// }

