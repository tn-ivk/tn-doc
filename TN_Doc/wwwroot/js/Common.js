const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5010/SignalRApp")
    //.WithAutomaticReconnect()
    .build();

hubConnection.on("Receive", function (deviceName, tagName, tagValue) {

    if (deviceName == CurrentDeviceName) {

        if (tagName == GetFullNameTag('ARM.ARM_OnlineReportCounter'))
            GetDoc();
    } else if (deviceName == 'ARM') {
        ApplicationSecurity(tagName, tagValue);
    }

});
hubConnection.start();

window.onmessage = function (event) {
    if (event.data == 'ButtonSaveOn') {
        $("#ButtonSave").prop("disabled", false);
    } else if (event.data == 'ButtonSaveOff') {
        $("#ButtonSave").prop("disabled", true);
    }
};

var IsUsedElis = true;

var PrefixTag = "IVK_TN_01";

var CurrentDeviceName;
var CurrentDeviceId;

var table = null;
var currentId = null;
var docTemplates = {};

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

var languageDataTable = {
    "processing": "Подождите...",
    "search": "Поиск:",
    "lengthMenu": "Показать _MENU_ записей",
    "info": "Записи с _START_ до _END_ из _TOTAL_ записей",
    "infoEmpty": "Записи с 0 до 0 из 0 записей",
    "infoFiltered": "(отфильтровано из _MAX_ записей)",
    "loadingRecords": "Загрузка записей...",
    "zeroRecords": "Записи отсутствуют.",
    "emptyTable": "В таблице отсутствуют данные",
    "paginate": {
        "first": "Первая",
        "previous": "Предыдущая",
        "next": "Следующая",
        "last": "Последняя"
    },
    "aria": {
        "sortAscending": ": активировать для сортировки столбца по возрастанию",
        "sortDescending": ": активировать для сортировки столбца по убыванию"
    },
    "select": {
        "rows": {
            "_": "Выбрано записей: %d",
            "1": "Выбрана одна запись"
        },
        "cells": {
            "_": "Выбрано %d ячеек",
            "1": "Выбрана 1 ячейка "
        },
        "columns": {
            "1": "Выбран 1 столбец ",
            "_": "Выбрано %d столбцов "
        }
    },
    "searchBuilder": {
        "conditions": {
            "string": {
                "startsWith": "Начинается с",
                "contains": "Содержит",
                "empty": "Пусто",
                "endsWith": "Заканчивается на",
                "equals": "Равно",
                "not": "Не",
                "notEmpty": "Не пусто",
                "notContains": "Не содержит",
                "notStartsWith": "Не начинается на",
                "notEndsWith": "Не заканчивается на"
            },
            "date": {
                "after": "После",
                "before": "До",
                "between": "Между",
                "empty": "Пусто",
                "equals": "Равно",
                "not": "Не",
                "notBetween": "Не между",
                "notEmpty": "Не пусто"
            },
            "number": {
                "empty": "Пусто",
                "equals": "Равно",
                "gt": "Больше чем",
                "gte": "Больше, чем равно",
                "lt": "Меньше чем",
                "lte": "Меньше, чем равно",
                "not": "Не",
                "notEmpty": "Не пусто",
                "between": "Между",
                "notBetween": "Не между ними"
            },
            "array": {
                "equals": "Равно",
                "empty": "Пусто",
                "contains": "Содержит",
                "not": "Не равно",
                "notEmpty": "Не пусто",
                "without": "Без"
            }
        },
        "data": "Данные",
        "deleteTitle": "Удалить условие фильтрации",
        "logicAnd": "И",
        "logicOr": "Или",
        "title": {
            "0": "Конструктор поиска",
            "_": "Конструктор поиска (%d)"
        },
        "value": "Значение",
        "add": "Добавить условие",
        "button": {
            "0": "Конструктор поиска",
            "_": "Конструктор поиска (%d)"
        },
        "clearAll": "Очистить всё",
        "condition": "Условие",
        "leftTitle": "Превосходные критерии",
        "rightTitle": "Критерии отступа"
    },
    "searchPanes": {
        "clearMessage": "Очистить всё",
        "collapse": {
            "0": "Панели поиска",
            "_": "Панели поиска (%d)"
        },
        "count": "{total}",
        "countFiltered": "{shown} ({total})",
        "emptyPanes": "Нет панелей поиска",
        "loadMessage": "Загрузка панелей поиска",
        "title": "Фильтры активны - %d",
        "showMessage": "Показать все",
        "collapseMessage": "Скрыть все"
    },
    "buttons": {
        "pdf": "PDF",
        "print": "Печать",
        "collection": "Коллекция <span class=\"ui-button-icon-primary ui-icon ui-icon-triangle-1-s\"><\/span>",
        "colvis": "Видимость столбцов",
        "colvisRestore": "Восстановить видимость",
        "copy": "Копировать",
        "copyTitle": "Скопировать в буфер обмена",
        "csv": "CSV",
        "excel": "Excel",
        "pageLength": {
            "-1": "Показать все строки",
            "_": "Показать %d строк",
            "1": "Показать 1 строку"
        },
        "removeState": "Удалить",
        "renameState": "Переименовать",
        "copySuccess": {
            "1": "Строка скопирована в буфер обмена",
            "_": "Скопировано %d строк в буфер обмена"
        },
        "createState": "Создать состояние",
        "removeAllStates": "Удалить все состояния",
        "savedStates": "Сохраненные состояния",
        "stateRestore": "Состояние %d",
        "updateState": "Обновить",
        "copyKeys": "Нажмите ctrl  или u2318 + C, чтобы скопировать данные таблицы в буфер обмена.  Для отмены, щелкните по сообщению или нажмите escape."
    },
    "decimal": ".",
    "infoThousands": ",",
    "autoFill": {
        "cancel": "Отменить",
        "fill": "Заполнить все ячейки <i>%d<i><\/i><\/i>",
        "fillHorizontal": "Заполнить ячейки по горизонтали",
        "fillVertical": "Заполнить ячейки по вертикали",
        "info": "Информация"
    },
    "datetime": {
        "previous": "Предыдущий",
        "next": "Следующий",
        "hours": "Часы",
        "minutes": "Минуты",
        "seconds": "Секунды",
        "unknown": "Неизвестный",
        "amPm": [
            "AM",
            "PM"
        ],
        "months": {
            "0": "Январь",
            "1": "Февраль",
            "10": "Ноябрь",
            "11": "Декабрь",
            "2": "Март",
            "3": "Апрель",
            "4": "Май",
            "5": "Июнь",
            "6": "Июль",
            "7": "Август",
            "8": "Сентябрь",
            "9": "Октябрь"
        },
        "weekdays": [
            "Вс",
            "Пн",
            "Вт",
            "Ср",
            "Чт",
            "Пт",
            "Сб"
        ]
    },
    "editor": {
        "close": "Закрыть",
        "create": {
            "button": "Новый",
            "title": "Создать новую запись",
            "submit": "Создать"
        },
        "edit": {
            "button": "Изменить",
            "title": "Изменить запись",
            "submit": "Изменить"
        },
        "remove": {
            "button": "Удалить",
            "title": "Удалить",
            "submit": "Удалить",
            "confirm": {
                "_": "Вы точно хотите удалить %d строк?",
                "1": "Вы точно хотите удалить 1 строку?"
            }
        },
        "multi": {
            "restore": "Отменить изменения",
            "title": "Несколько значений",
            "info": "Выбранные элементы содержат разные значения для этого входа. Чтобы отредактировать и установить для всех элементов этого ввода одинаковое значение, нажмите или коснитесь здесь, в противном случае они сохранят свои индивидуальные значения.",
            "noMulti": "Это поле должно редактироваться отдельно, а не как часть группы"
        },
        "error": {
            "system": "Возникла системная ошибка (<a target=\"\\\" rel=\"nofollow\" href=\"\\\">Подробнее<\/a>)."
        }
    },
    "searchPlaceholder": "Что ищете?",
    "stateRestore": {
        "creationModal": {
            "button": "Создать",
            "search": "Поиск",
            "columns": {
                "search": "Поиск по столбцам",
                "visible": "Видимость столбцов"
            },
            "name": "Имя:",
            "order": "Сортировка",
            "paging": "Страницы",
            "scroller": "Позиция прокрутки",
            "searchBuilder": "Редактор поиска",
            "select": "Выделение",
            "title": "Создать новое состояние",
            "toggleLabel": "Включает:"
        },
        "removeJoiner": "и",
        "removeSubmit": "Удалить",
        "renameButton": "Переименовать",
        "duplicateError": "Состояние с таким именем уже существует.",
        "emptyError": "Имя не может быть пустым.",
        "emptyStates": "Нет сохраненных состояний",
        "removeConfirm": "Вы уверены, что хотите удалить %s?",
        "removeError": "Не удалось удалить состояние.",
        "removeTitle": "Удалить состояние",
        "renameLabel": "Новое имя для %s:",
        "renameTitle": "Переименовать состояние"
    },
    "thousands": " "
};

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

    $.ajax(
        {
            async: false,
            url: "Home/IsUsedSecurity",
            type: "GET",
            //dataType: "json",
            data:
                {},
            success: function (data) {
                if (data) {

                    let ShowEditAndSave = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.ShowEditAndSave', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.ShowEditAndSave', ShowEditAndSave);

                    let AllowEditAndSave = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.AllowEditAndSave', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.AllowEditAndSave', AllowEditAndSave);

                    let ShowPrint = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.ShowPrint', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.ShowPrint', ShowPrint);

                    let AllowPrint = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.AllowPrint', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.AllowPrint', AllowPrint);

                    let ShowExport = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.ShowExport', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.ShowExport', ShowExport);

                    let AllowExport = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.AllowExport', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.AllowExport', AllowExport);

                    let ShowEditDictionaries = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.ShowEditDictionaries', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.ShowEditDictionaries', ShowEditDictionaries);

                    let AllowEditDictionaries = ReadTagCacheARM(DeviceName = 'ARM', 'root.ARM.Reports.AllowEditDictionaries', namespaceIndex = 1, indexArray = 0);
                    ApplicationSecurity('root.ARM.Reports.AllowEditDictionaries', AllowEditDictionaries);
                }
            }
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
            url: 'Print/GetListPrinters',
            type: 'GET',
            success: function (data) {
                data.forEach((item) => {
                    let opt = document.createElement("option");
                    opt.value = item;
                    opt.appendChild(document.createTextNode(item));
                    document.querySelector('#ComboboxPrinterName').appendChild(opt);
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
            scrollY: '60vh',
            scrollCollapse: true,
            paging: false,

            info: false,
            ordering: false,

            language: languageDataTable,
            //oLanguage: languageDataTable
            //{
            //    sSearch: 'Поиск',
            //    sEmptyTable: 'Отсутствуют данные в таблице'
            //}
            //,

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
                ],
            //    columnDefs: [
            //        {
            //            targets: 0,
            //            render: DataTable.render.date()
            //        }]
        });

    table.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            var id = table.rows(indexes).data().pluck('id');
            currentId = id[0];

            if ($('#ComboboxDocGUID').val() == 32) {

                let BIKId = 1;
                let DirId = 0;

                table.rows(indexes).data().pluck('advancedProperties')[0].forEach((item) => {
                    if (item.key == 'BIKId') BIKId = item.value;
                    else if (item.key == 'DirId') DirId = item.value;
                });

                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport_BIKId'), BIKId, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport_DirId'), DirId, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_OnlineReportType'), currentId, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport'), true, 2, 0);
            } else {
                GetDoc();
            }
        }
    });
}

function InitElement() {
     let iframe = document.querySelector('.FR');
     iframe.onload = function(){
        let elisNodes = iframe.contentWindow.document.querySelectorAll('.elis-data')
    }

    /**/
    InitDirEditorComponent();
    /**/


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
                    $('#ButtonReview').prop('hidden', true);
                    $('#ButtonEdit').prop('hidden', true);
                } else {
                    $('#ButtonSave').prop('disabled', false);
                    $('#ButtonReview').prop('hidden', false);
                    $('#ButtonEdit').prop('hidden', false);
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

                $('#viewPanel').prop('hidden', false);
                $('#editPanel').prop('hidden', true);
            },
        });
}

function GetEditDoc() {
    if(currentId == null) 
        return;
    
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

                $('#viewPanel').prop('hidden', true);
                $('#editPanel').prop('hidden', false);
            }
        });
}

async function SaveDoc() {
    const result = await document.getElementsByClassName('FR')[0].contentWindow.SaveDoc(
        $('#ComboboxDevice :selected').text(),
        $('#ComboboxDevice').val(),
        $('#ComboboxDocGUID').val(),
        currentId,
        PrefixTag);
    if (result)
    {
        GetDoc();
    }
    else
    {
        const errorDialog = document.getElementById('errorDialog');
        const errorMessage = document.getElementById('errorMessage');
        errorMessage.textContent = "Не получено подтверждение записи данных от ИВК";
        errorDialog.showModal();
    }
    
}

function GetPeriodDocument() {

    var ret = null;
    $.ajax(
        {
            async: false,
            url: 'Home/GetPeriodDocument',
            type: 'GET',
            data: {
                IdDevice: $('#ComboboxDevice').val(),
                IdDoc: $('#ComboboxDocGUID').val(),
                id: currentId
            },
            success: function (data) {
                ret = data;
            }
        });

    return ret;
}

function PrintDoc() {
    $.ajax(
        {
            url: 'Print/PrintDoc',
            type: 'GET',
            data: {
                printerName: $('#ComboboxPrinterName').val()
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

function ReadTagCacheARM(DeviceName = 'ARM', tagName, namespaceIndex = 1, indexArray = 0) {

    var url = "http://localhost:5010/api/OPCClientCache/";
    var result;
    $.ajax(
        {
            async: false,
            url: url + DeviceName + '/' + tagName + '/' + namespaceIndex + '/' + indexArray,
            type: "Get",
            //dataType: 'json',
            //data: {
            //    nameTag: "ARM.ARM_OnlineReportCounter"
            //},
            success: function (data) {
                result = data;
            },
        });

    return result;
}

function GetFullNameTag(tagName) {
    return PrefixTag + '.' + tagName;
}

//Получить данные из ЕЛИС
function GetElisData() {

    ClearDataElis();

    var dataELIS;

    var clientToken = GetClientToken(CurrentDeviceId);

    if (clientToken == undefined) {

        var regData = GetDataForRegistrationDeviceInELIS(CurrentDeviceId);

        if (regData == '')
            return;
        else
            clientToken = RegistrationClient(regData);
    }

    var periodDocument = GetPeriodDocument();
    
    StateButtonGetElisData(true);

    $.ajax(
        {
            async: true,
            url: 'http://localhost:5050/api/tspd/getqp',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            headers: {
                "client-token": clientToken.clientToken
            },
            data:
                JSON.stringify({
                    startPeriod: moment.utc(periodDocument.begin * 1000).format(),
                    endPeriod: moment.utc(periodDocument.end * 1000).format()
                }),
            success: function (data) {
                if (data.isError) {
                    if(data.textError) {
                        $('#info').text(data.textError);
                        $.post("Elis/ErrorMessage/", {msg:data.textError});    
                    }
                }
                else if (data.passports.length == 0)
                    $('#info').text("Данные для паспорта в системе ЕЛИС не найдены.");
                else    //отрисовываем таблицу с паспортами
                    dataELIS = data;
            },
            error: function (data) {
                $('#info').text('Ошибка выполнения запроса.');

                //Неавторизованный пользователь
                if (data.status == 401) {
                    RegistrationClient('ИВК-1');
                }
            },
            complete: function (data) {
                StateButtonGetElisData(false);
                if(dataELIS)
                    DrawTablePassports(dataELIS);
            }
        });
}

//Зарегистрировать устройство для ЕЛИС
function RegistrationClient(regData) {

    var clientToken = null;

    $.ajax(
        {
            async: false,
            url: 'http://localhost:5050/api/client/signin',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            data: JSON.stringify(regData),
            //    JSON.stringify({
            //    ostKey: nameDevice,
            //    siknKey: '',
            //    clientName: ''
            //}),
            success: function (data) {

                clientToken = data;
            },
            error: function (data) {

            }
        });

    return clientToken;
}

//Зарегистрация устройства для взаимодействия с ТСПД
function GetElisCurrentGuidForDevice() {

}

//Получить данные для регистрации устройства в ЕЛИС.
function GetDataForRegistrationDeviceInELIS(idDevice) {

    var regData = null;

    $.ajax(
        {
            async: false,
            url: 'Home/GetDataForRegistrationDeviceInELIS',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            data: JSON.stringify({
                IdDevice: idDevice
            }),
            success: function (data) {

                regData = data;

            },
            error: function (data) {

            }
        });

    return regData;
}

//Получить GUID для устройства
function GetClientToken(idDevice) {

    var clientToken = null;

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
                clientToken = data.clientToken;
            },
            error: function (data) {

            },
            complete: function (data) {
            }
        });

    return clientToken;
}

//Сохранить GUID для устройства
function SetClientToken() {

}


function DrawTablePassports(dataELIS) {
    let element = document.querySelector('#listPassports');
    $('#listPassports').empty();
    localStorage.setItem('labInfo', JSON.stringify(dataELIS.labInfo));
    dataELIS.passports.forEach(function (item, i, arr) {
        let li = document.createElement('button');
        li.className = 'list-group-item list-group-item-action';
        li.innerHTML = `<b>Номер протокола:</b> <small>${item.protocolNumber}</small><br>
                         <b>Лаборатория:</b> <small>${item.labName}</small><br>
                         <b>Период:</b> <small>${item.startPeriodTime}-${item.endPeriodTime}</small>`;
        li.dataPassport = item;
        li.addEventListener('click', function (e) {
            let elisPassport = this;
            sessionStorage.setItem('dataPassport', JSON.stringify(elisPassport.dataPassport));
            localStorage.setItem('dataPassport', JSON.stringify(elisPassport.dataPassport));
            
            let passports = document.querySelectorAll('.list-group-item')
            passports.forEach(function (item, i, arr) {
                if (item.classList.contains('active'))
                    item.classList.remove('active');
            })
            elisPassport.classList.add('active');
        });
        element.append(li);
    });
}

function SetDataLocalStorage() {
}


function FillPassportDataElis() {
    try {
        //console.log("FillPassportDataElis" );
        let dataPassport = JSON.parse(localStorage.dataPassport);
        let labInfo = JSON.parse(localStorage.labInfo);
        let iframe = document.querySelector('.FR');
        let elisNodes = iframe.contentWindow.document.querySelectorAll('.elis-data')
        //console.log('dataPassport.parameters',dataPassport.parameters);
        //console.log()
        elisNodes.forEach((item, index, array) => {
            let itemKeys = item.dataset.elisAlias?.split('|');
            if (itemKeys === undefined)
            {
                console.log(item.dataset.key + " не имеет корректного алиаса");
            }
            else
            {
                console.log(item.dataset.key + " имеет корректный алиас");
            }
            let root = null;
            let currentKey = "";
            for (let key in dataPassport.parameters) {
                //console.log('itemKeys', itemKeys);
                //console.log('key', key);
                //console.log('itemKeys.includes(key)', itemKeys.includes(key));
                if (itemKeys.includes(key)) {
                    root = dataPassport.parameters
                    for (let iKey of itemKeys) {
                        if (iKey === key) { 
                            currentKey = key;
                            break;
                        }
                    }
                    break;
                }
            }

            if (root === null) {
                for (let key in dataPassport) {
                    if (itemKeys.includes(key)) {
                        root = dataPassport
                        for (let iKey of itemKeys) {
                            if (iKey === key) {
                                currentKey = key;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            if (root === null) {
                for (let key in labInfo) {
                    if (itemKeys.includes(key)) {
                        root = labInfo
                        for (let iKey of itemKeys) {
                            if (iKey === key) {
                                currentKey = key;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            if (root === null) {
                return;
            }

            if (item.nodeName === 'INPUT') {
                const value = item.dataset.tag === 'AdditionalInfo'
                    ? root[currentKey]
                    : root[currentKey].value;
                
                if(item.type === 'datetime-local') {
                    item.value = moment(value).format('YYYY-MM-DD HH:mm:ss');
                }
                else {
                    item.value = value;
                    FixedElisData(item);
                }    
                if(item.hasAttribute("oninput")){
                    item.oninput();
                }
                if(item.hasAttribute("backlight"))
                    item.setAttribute("backlight", "green");
                item.addEventListener("input", ManualCorrect, {once:true});
            }

            if (item.nodeName === 'SELECT') {
                item.contains = function (value) {
                    for (var i = 0, l = this.options.length; i < l; i++) {
                        if (this.options[i].text === value) {
                            return true;
                        }
                    }
                    return false;
                }
                let obj = root[currentKey];
                switch(item.dataset.tag)
                {
                    case 'AdditionalInfo': 
                        //Проверяем наличие значения в списке, если нет, добавляем.
                        if (!item.contains(obj)) {
                            item.append(new Option(obj, obj));
                        }
                        item.value = obj;            
                        break;
                    case 'Metod': 
                        const flag = obj.value?.toFloat() !== obj['valueString']?.toFloat()
                        const limitValue = parseFloat(obj.value) + 0.1
                        let metod = new Metod(0,true, 0, obj.testMethodName, flag, limitValue, obj.valueString);

                        //Проверяем наличие значения в списке, если нет, добавляем.
                        if (!item.contains(obj.testMethodName)) {
                            item.append(new Option(obj.testMethodName, obj.testMethodName));
                        }
                        item.value = obj.testMethodName;
                        item.options[item.selectedIndex].setAttribute("data-metod", JSON.stringify(metod));
                        break;
                    default: 
                        break;
                }
                if(item.hasAttribute("backlight"))
                    item.setAttribute("backlight", "green");
                item.addEventListener("input", ManualCorrect, {once:true});
            }
        });
    } finally {
        console.groupEnd();
    }
}

//Добивание незначимыми нулями данных с ЕЛИС
function FixedElisData(object) {
    if(!object) return;
    if(!object.hasAttribute('data-roundValue')) return;
    const f = x => ((x.toString().includes('.')) ? (x.toString().split('.').pop().length) : (0));
    if (f(object) >= object.getAttribute('data-roundValue')) return;
        
    const num = parseFloat(object.value.replace(",", "."));
    object.value = num.toFixed(object.getAttribute('data-roundValue'));
}

//Сброс подсветки данных при ручной корректировки
function ManualCorrect(event) {
    if(!event) return;
    
    if(event.target.hasAttribute("backlight"))
        event.target.setAttribute("backlight", "white");
}

//Состояние кнопки "Запросить данные"
function StateButtonGetElisData(state) {

    if (state) {
        $("#buttonDataRequest").prop("disabled", true);
        $("#spinnerDataRequest").prop("hidden", false);
        $("#textDataRequest")[0].innerText = "Идет запрос...";
    } else {
        $("#buttonDataRequest").prop("disabled", false);
        $("#spinnerDataRequest").prop("hidden", true);
        $("#textDataRequest")[0].innerText = "Запросить данные";
    }
}

function ButtonElis() {
    $('#listPassports').empty();
    localStorage.removeItem('dataPassport');
}

function ApplicationSecurity(tagName, tagValue) {

    if (tagName == 'root.ARM.Reports.ShowEditAndSave')
        $("#ButtonEdit").prop("hidden", !tagValue);
    else if (tagName == 'root.ARM.Reports.AllowEditAndSave')
        $("#ButtonEdit").prop("disabled", !tagValue);
    else if (tagName == 'root.ARM.Reports.ShowPrint') {
        $("#ComboboxPrinterName").prop("hidden", !tagValue);
        $("#ButtonPrint").prop("hidden", !tagValue);
    } else if (tagName == 'root.ARM.Reports.AllowPrint') {
        $("#ComboboxPrinterName").prop("disabled", !tagValue);
        $("#ButtonPrint").prop("disabled", !tagValue);
    } else if (tagName == 'root.ARM.Reports.ShowExport') {
        $("#ComboboxExportFormat").prop("hidden", !tagValue);
        $("#ButtonExport").prop("hidden", !tagValue);
    } else if (tagName == 'root.ARM.Reports.AllowExport') {
        $("#ComboboxExportFormat").prop("disabled", !tagValue);
        $("#ButtonExport").prop("disabled", !tagValue);
    } else if (tagName == 'root.ARM.Reports.ShowEditDictionaries')
        $("#ButtonDictionaries").prop("hidden", !tagValue);
    else if (tagName == 'root.ARM.Reports.AllowEditDictionaries')
        $("#ButtonDictionaries").prop("disabled", !tagValue);
}


function ClearDataElis() {
    $('#info').text('');
    $('#listPassports').empty();
    localStorage.removeItem('dataPassport');
}

String.prototype.toFloat = function (value) {
    return parseFloat(this.replace(',', '.').trim())
}

class Metod
{
    Id;
    Use;
    IdParameter;
    Name;
    LimitValueActivate;
    LimitValue;
    LimitValueString;

    constructor(pId, pUse, pIdParameter, pName, pLimitValueActivate, pLimitValue, pLimitValueString) {
        this.Id = pId;
        this.Use = pUse;
        this.IdParameter = pIdParameter;
        this.Name = pName;
        this.LimitValueActivate = pLimitValueActivate;
        this.LimitValue = pLimitValue;
        this.LimitValueString = pLimitValueString;
    }
}