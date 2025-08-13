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
let isViewing = false;
let isEditingDoc = false;
let isShowEditAndSave = true;
let isAllowEditAndSave = true;

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
    updateSaveBtnText();
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
    const $combobox = $('#ComboboxPrinterName');
    const $printBtn = $('#ButtonPrint');
    $combobox.empty(); 
    
    $.ajax({
        async: false, 
        url: 'Print/GetListPrinters',
        type: 'GET',
        success: function (data) {
            if (!data || data.length === 0) {
                $combobox.hide();
                $printBtn.hide();
            } else {
                $combobox.show();
                $printBtn.show();
                data.forEach((item) => {
                    let opt = document.createElement("option");
                    opt.value = item;
                    opt.appendChild(document.createTextNode(item));
                    $combobox.append(opt);
                });
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            $combobox.hide();
            $printBtn.hide();
            //showError(`Ошибка при загрузке списка принтеров: ${textStatus} ${errorThrown}`);
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

function GetDataWithSpinner() {
    $('#ButtonGetData').prop('disabled', true);
    $('#ButtonGetDataSpinner').prop('hidden', false);
    $('#ButtonGetDataText').text('Загрузка данных...');

    let timeoutId = setTimeout(function() {
        $('#ButtonGetData').prop('disabled', false);
        $('#ButtonGetDataSpinner').prop('hidden', true);
        $('#ButtonGetDataText').text('Получить данные');
        showError('Превышен таймаут загрузки данных');
    }, 60000);

    setTimeout(function() {
        try {
            table.ajax.reload(function() {
                clearTimeout(timeoutId);
            });
        } catch (error) {
            clearTimeout(timeoutId);
            $('#ButtonGetData').prop('disabled', false);
            $('#ButtonGetDataSpinner').prop('hidden', true);
            $('#ButtonGetDataText').text('Получить данные');
            showError(`Ошибка при обновлении таблицы: ${error && error.message ? error.message : error}`);
        }
    }, 10);
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
            
            ajax: function (data, callback, settings) {
                try {
                    let result = GetData();
                    callback(result);
                    
                    $('#ButtonGetData').prop('disabled', false);
                    $('#ButtonGetDataSpinner').prop('hidden', true);
                    $('#ButtonGetDataText').text('Получить данные');
                } catch (error) {
                    showError(`Ошибка при загрузке данных: ${error && error.message ? error.message : error}`);
                    
                    $('#ButtonGetData').prop('disabled', false);
                    $('#ButtonGetDataSpinner').prop('hidden', true);
                    $('#ButtonGetDataText').text('Получить данные');
                    
                    callback({'data': []});
                }
            },

            columns:
                [
                    {data: 'dt'},
                    {data: 'description'}
                ],
        });

    table.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            let id = table.rows(indexes).data().pluck('id');
            currentId = id[0];

            if ($('#ComboboxDocGUID').val() == 32) {

                let BIKId = 1;
                let DirId = 0;

                table.rows(indexes).data().pluck('advancedProperties')[0].forEach((item) => {
                    if (item.key == 'BIKId') BIKId = item.value;
                    else if (item.key == 'DirId') DirId = item.value;
                });

                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport_BIKId'), BIKId, 2, 0);
                //WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport_DirId'), DirId, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_OnlineReportType'), currentId, 2, 0);
                WriteTag(CurrentDeviceName, GetFullNameTag('ARM.ARM_GetOnlineReport'), true, 2, 0);
            } else {
                GetDoc();
            }
        }
    });
}

function InitElement() {
    $('#ButtonPrint').hide();
    $('#ComboboxPrinterName').hide();
    $('#ButtonExport').hide();
    $('#ComboboxExportFormat').hide();
    
     let iframe = document.querySelector('.FR');
     iframe.onload = function(){
        let elisNodes = iframe.contentWindow.document.querySelectorAll('.elis-data')
    }

    InitDirEditorComponent();
    InitDevices();
    InitDocs();
    InitDatepickerBegin();
    InitDatepickerEnd();
    InitTableDocs();
    InitTemplatesDoc();
    //InitPrinterName();
    //InitExportFormat();
    InitProtocolNumber();
    $('#ComboboxDocGUID').change(function () {
        if (table != null) $('#DataTable').DataTable().clear().draw();
        $('.FR').attr('src', '');

        InitTemplatesDoc();
        InitProtocolNumber();
        
        $('#viewModeButton').prop('hidden', true);
        updateSaveBtnText();
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
    let ret = null;
    let hasError = false;
    let DTBegin = $('#DatepickerBegin').datepicker('getDate');
    let DTEnd = $('#DatepickerEnd').datepicker('getDate');
    let strDTBegin = "";
    let strDTEnd = "";
    if (DTBegin == null || DTEnd == null) {
        ret = [];
        return {'data': ret};
    } else {
        strDTBegin = DTBegin.getDate() + '.' + (DTBegin.getMonth() + 1) + '.' + DTBegin.getFullYear();
        strDTEnd = DTEnd.getDate() + '.' + (DTEnd.getMonth() + 1) + '.' + DTEnd.getFullYear();
    }

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
                ret = data;
            },
            error: function (xhr, ajaxOptions, thrownError) {
                hasError = true;
                ret = [];
            },
            complete: function (data) {
                if ($('#ComboboxDocGUID').val() == 0 ||
                    $('#ComboboxDocGUID').val() == 3 ||
                    $('#ComboboxDocGUID').val() == 32) {
                    $('#ButtonSave').prop('disabled', true);
                    isEditingDoc = false;
                    $('#viewModeButton').prop('hidden', true);
                } else {
                    $('#ButtonSave').prop('disabled', false);
                    isEditingDoc = true;
                }

                if ($('#ComboboxDocGUID').val() == 1) {
                    $('#ButtonElis').prop('hidden', !IsUsedElis);
                } else
                    $('#ButtonElis').prop('hidden', true);
            }
        });

    if (hasError) {
        throw new Error('Ошибка при загрузке данных с сервера');
    }
    return {'data': ret};
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
                if (data)
                    $('.FR').attr('src', '/PDF/PDF.pdf#view=FitH');
                    //$('.FR').attr('src', '/PDF/PDF.pdf#toolbar=0&view=FitH');

                $('#viewPanel').prop('hidden', false);
                $('#editPanel').prop('hidden', true);
                isViewing = true;
                if(isEditingDoc)
                    $('#viewModeButton')
                        .prop('value', 'Редактирование')
                        .prop('hidden', false)
                        .prop('disabled', !isAllowEditAndSave);
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
    
    isViewing = false;
    $('#viewModeButton').prop('hidden', false)
        .prop('value', '     Просмотр     ');
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

//Запросить данные из ЕЛИС
function GetElisData() {
    logTrace("Запрос данных ЕЛИС инициирован");
    ClearDataElis();
    let dataELIS;
    let clientToken = GetElisToken();

    if (clientToken == null) {
        logError("Не удалось получить токен для TN.ElisConnector. Запрос данных невозможен!");
        $('#info').html('Не удалось получить токен для TN.ElisConnector.<br>Запрос данных невозможен!');
        $.post("Elis/ErrorMessage/", {msg:"Не удалось получить токен для TN.ElisConnector"});
        return;
    }
    const periodDocument = GetPeriodDocument();
    StateButtonGetElisData(true);

    logTrace("Отправлен запрос к API ЕЛИС");
    $.ajax({
        async: true,
        url: 'http://localhost:5050/api/tspd/getqp',
        type: 'POST',
        contentType: 'application/json; charset=UTF-8',
        dataType: 'json',
        headers: {
            "client-token": clientToken.clientToken
        },
        data: JSON.stringify({
            startPeriod: moment.utc(periodDocument.begin * 1000).format(),
            endPeriod: moment.utc(periodDocument.end * 1000).format()
        }),
        success: function (data) {
            if (data.isError) {
                logError("Ошибка от API ЕЛИС: " + (data.textError || ''));
                if(data.textError) {
                    $('#info').text(data.textError);
                    $.post("Elis/ErrorMessage/", {msg:data.textError});    
                }
            }
            else if (data.passports.length == 0) {
                logWarn("Данные для паспорта в системе ЕЛИС не найдены");
                $('#info').text("Данные для паспорта в системе ЕЛИС не найдены.");
            }
            else {
                logTrace("Данные ЕЛИС успешно получены");
                dataELIS = data;
            }
        },
        error: function (data) {
            logError("Ошибка выполнения запроса к API ЕЛИС: " + (data && data.status ? data.status : ''));
            $('#info').text('Ошибка выполнения запроса.');

            if (data.status == 401) {
                logWarn("Неавторизованный пользователь при запросе к API ЕЛИС");
                RegistrationClient('ИВК-1');
            }
        },
        complete: function (data) {
            logTrace("Запрос к API ЕЛИС завершён");
            StateButtonGetElisData(false);
            if(dataELIS)
                DrawTablePassports(dataELIS);
        }
    });
}

//Получение токена для ЕЛИС
function GetElisToken() {
    let elisToken = GetClientToken(CurrentDeviceId);

    if (elisToken == null) {
        const regData = GetDataForRegistrationDeviceInELIS(CurrentDeviceId);
        if (regData == '')
            return;
        else
            elisToken = RegistrationClient(regData);
    }
    return elisToken;
}

//Зарегистрировать устройство для ЕЛИС
function RegistrationClient(regData) {

    let clientToken = null;

    $.ajax(
        {
            async: false,
            url: 'http://localhost:5050/api/client/signin',
            type: 'POST',
            contentType: 'application/json; charset=UTF-8',
            dataType: 'json',
            data: JSON.stringify(regData),
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
    logTrace('Начало отрисовки таблицы протоколов испытаний ЕЛИС');
    let element = document.querySelector('#listPassports');
    $('#listPassports').empty();
    localStorage.setItem('labInfo', JSON.stringify(dataELIS.labInfo));
    dataELIS.passports.forEach(function (item, i, arr) {
        logTrace('Добавление протокола испытаний ЕЛИС в таблицу: ' + (item.protocolNumber || '[нет номера]'));
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
            UpdateElisApplyButtonState();
        });
        element.append(li);
    });
    logTrace('Таблица протоколов испытаний ЕЛИС успешно отрисована. Количество протоколов: ' + (dataELIS.passports ? dataELIS.passports.length : 0));
    
    // Автоматически выбираем первый протокол, если протоколы загружены
    if (dataELIS.passports && dataELIS.passports.length > 0) {
        const firstProtocol = document.querySelector('#listPassports .list-group-item:first-child');
        if (firstProtocol) {
            logTrace('Автоматический выбор первого протокола ЕЛИС');
            firstProtocol.click();
        }
    } else {
        UpdateElisApplyButtonState();
    }
}

function ResetPassportDataElis() {
    $('#info').text('');
    StateButtonGetElisData(false);
}

// Функция для форматирования ФИО в формат "И. О. Фамилия"
// Пример: {givenName: "Иван", middleName: "Петрович", familyName: "Сидоров"} -> "И. П. Сидоров"
function formatLabRepresentativeName(laboratory) {
    if (!laboratory) {
        return '';
    }
    
    // Если есть новые поля, используем их
    if (laboratory.givenName && laboratory.familyName) {
        let formatted = '';
        
        // Добавляем первую букву имени с точкой
        if (laboratory.givenName) {
            formatted += laboratory.givenName.charAt(0).toUpperCase() + '. ';
        }
        
        // Добавляем первую букву отчества с точкой
        if (laboratory.middleName) {
            formatted += laboratory.middleName.charAt(0).toUpperCase() + '. ';
        }
        
        // Добавляем полную фамилию
        if (laboratory.familyName) {
            formatted += laboratory.familyName;
        }
        
        return formatted.trim();
    }
    
    // Fallback к старому полю iof, если новые поля отсутствуют
    return laboratory.iof || '';
}

function FillPassportDataElis() {
    logTrace('Начало заполнения данных паспорта из ЕЛИС');
    try {
        let dataPassport = JSON.parse(localStorage.dataPassport);
        if (!dataPassport) {
            logError('dataPassport не найден в localStorage или имеет невалидное значение');
            showError('Ошибка: данные протокола ЕЛИС не найдены');
            return;
        }
        else{
            logTrace('ПИ ЕЛИС:\n' + JSON.stringify(dataPassport, null, 2));
        }

        let labInfo = JSON.parse(localStorage.labInfo);
        if (!labInfo) {
            logError('labInfo не найден в localStorage или имеет невалидное значение');
        }
        else{
            logTrace('Информация о лаборатории:\n' + JSON.stringify(labInfo, null, 2));
        }
        
        let iframe = document.querySelector('.FR');
        let elisNodes = iframe.contentWindow.document.querySelectorAll('.elis-data');
        
        if (!elisNodes || elisNodes.length === 0) {
            logError('Форма паспорта не настроена для заполнения данными с ЕЛИС');
            showError('Форма паспорта не настроена для заполнения данными с ЕЛИС');
            return;
        }
        
        logTrace('Найдено элементов для заполнения данными ЕЛИС: ' + elisNodes.length);

        const elisElementsInfo = Array.from(elisNodes).map((item, index) => {
            const nodeName = item.nodeName;
            const tag = item.dataset.tag || 'не указан';
            const elisAlias = item.dataset.elisAlias || 'не указан';
            const key = item.dataset.key || 'не указан';
            const type = item.type || 'не указан';
            const id = item.id || 'не указан';
            const className = item.className || 'не указан';
            return `index=${index + 1}; nodeName=${nodeName}; tag=${tag}; elisAlias=${elisAlias}; key=${key}; type=${type}; id=${id}; className=${className}`;
        });
        logTrace('Элементы интерфейса для заполнения ЕЛИС:\n' + elisElementsInfo.join('\n'));
        
        // Добавляем данные о представителе лаборатории из Signers
        if (dataPassport.signers?.laboratory) {
            // Формируем ИОФ в новом формате "И. О. Фамилия"
            dataPassport.chiefLabShortSign = formatLabRepresentativeName(dataPassport.signers.laboratory);
            dataPassport.chiefLabPosition = dataPassport.signers.laboratory.post;
            dataPassport.chiefLabOrganization = dataPassport.signers.laboratory.company;
        }

        elisNodes.forEach((item, index, array) => {
            let itemKeys = item.dataset.elisAlias?.split('|');
            let root = null;
            let currentKey = "";
            for (let key in dataPassport.parameters) {
                if (itemKeys.includes(key)) {
                    root = dataPassport.parameters;
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
                        root = dataPassport;
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
                        root = labInfo;
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

            logTrace('Заполнение поля из ЕЛИС: ' + (currentKey || '[нет ключа]') + ', значение: ' + (root[currentKey] !== undefined ? JSON.stringify(root[currentKey]) : '[нет значения]'));

            if (item.nodeName === 'INPUT') {
                switch (item.dataset.tag) {
                    case 'AdditionalInfo':
                        if(item.type === 'datetime-local') {
                            item.value = moment(root[currentKey]).format('YYYY-MM-DD HH:mm:ss');
                        }
                        else {
                            item.value = root[currentKey];
                        }
                        break;
                    case 'DocNum':
                        item.value = root[currentKey].documentNumber;
                        if(item.hasAttribute("data-document"))
                            item.setAttribute("data-document", JSON.stringify(new LabDocumentInfo(root[currentKey].documentType, root[currentKey].documentNumber, root[currentKey].documentDate)));
                        break;
                    case 'Value':
                        item.value = root[currentKey].value;
                        FixedElisData(item);
                        if (root[currentKey].valueString && root[currentKey].valueString !== root[currentKey].value) {
                            let printValueInput = document.createElement('input');
                            printValueInput.type = 'hidden';
                            printValueInput.setAttribute('data-key', item.dataset.key);
                            printValueInput.setAttribute('data-tag', 'PrintValue');
                            printValueInput.setAttribute('data-edit', '1');
                            printValueInput.setAttribute('data-elis-filled', 'true');
                            printValueInput.value = root[currentKey].valueString;
                            item.parentNode.appendChild(printValueInput);
                        }
                        break;
                }
                if(item.hasAttribute("oninput")){
                    item.oninput();
                }
                item.setAttribute("data-elis-filled", "true");
                applyElisHighlight(item);
                item.addEventListener("input", ManualCorrect, {once:true});
                if (item.dataset.tag === 'Value') {
                    item.addEventListener("input", function(e) {
                        if (e.target.getAttribute('data-elis-filled') === 'false') {
                            updatePrintColumnFromInput(e.target);
                        }
                    });
                }
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
                        if (!item.contains(obj)) {
                            item.append(new Option(obj, obj));
                        }
                        item.value = obj;           
                        break;
                    case 'Metod': 
                        const flag = obj.value?.toFloat() !== obj['valueString']?.toFloat();
                        const limitValue = parseFloat(obj.value) + 0.1;
                        let metod = new Metod(0,true, 0, obj.testMethodName, flag, limitValue, obj.valueString);
                        if (!item.contains(obj.testMethodName)) {
                            item.append(new Option(obj.testMethodName, obj.testMethodName));
                        }
                        item.value = obj.testMethodName;
                        item.options[item.selectedIndex].setAttribute("data-metod", JSON.stringify(metod));
                        break;
                }
                item.setAttribute("data-elis-filled", "true");
                applyElisHighlight(item);
                item.addEventListener("input", ManualCorrect, {once:true});
            }
        });
        const metodSelects = iframe.contentWindow.document.querySelectorAll('select[data-tag="Metod"][data-elis-filled="true"]');
        metodSelects.forEach(select => {
            iframe.contentWindow.TogglePrintCellEditable(select);
        });
        logTrace('Заполнение данных паспорта из ЕЛИС завершено успешно');
    } catch (error) {
        showError(`Ошибка заполнения данных ЕЛИС: ${error && error.message ? error.message : error}`);
        logError('Ошибка заполнения данных ЕЛИС: ' + (error && error.message ? error.message : error));
    }
}

// Функция для применения зеленой подсветки к элементам, заполненным из ЕЛИС
function applyElisHighlight(element) {
    try {
        // Добавляем CSS класс к самому элементу
        element.classList.add('elis-filled-input');
        
        // Находим родительскую ячейку и добавляем к ней класс
        let parentCell = element.closest('td');
        if (parentCell) {
            parentCell.classList.add('elis-filled-cell');
        }
    } catch (error) {
        console.error('Ошибка применения подсветки:', error);
    }
}

// Функция для удаления зеленой подсветки
function removeElisHighlight(element) {
    try {
        // Убираем CSS класс с самого элемента
        element.classList.remove('elis-filled-input');
        
        // Убираем inline стиль backgroundColor с элемента
        element.style.backgroundColor = '';
        
        // Находим родительскую ячейку и убираем с неё класс
        let parentCell = element.closest('td');
        if (parentCell) {
            parentCell.classList.remove('elis-filled-cell');
            
            // Убираем inline стиль backgroundColor с родительской ячейки
            parentCell.style.backgroundColor = '';
        }
    } catch (error) {
        console.error('Ошибка удаления подсветки:', error);
    }
}

//Добивание незначимыми нулями данных с ЕЛИС
function FixedElisData(object) {
    if(!object) return;
    if(!object.hasAttribute('data-roundValue')) return;
    const f = x => ((x.toString().includes('.')) ? (x.toString().split('.').pop().length) : (0));
    if (f(object.value) >= object.getAttribute('data-roundValue')) return;
        
    const num = parseFloat(object.value.replace(",", "."));
    object.value = num.toFixed(object.getAttribute('data-roundValue'));
}

//Сброс подсветки данных при ручной корректировки
function ManualCorrect(event) {
    if(!event) {
        return;
    }
    
    if(event.target.hasAttribute("data-elis-filled")) {
        event.target.setAttribute("data-elis-filled", "false");
        
        // Убираем зеленую подсветку
        removeElisHighlight(event.target);
        
        // Если это поле значения, обновляем колонку "Печать"
        if (event.target.dataset.tag === 'Value') {
            updatePrintColumnFromInput(event.target);
        }
        
        // Если это поле "Результат-Текст", обновляем только его статус
        if (event.target.dataset.tag === 'PrintValue') {
            let parentCell = event.target.closest('td');
            if (parentCell) {
                parentCell.setAttribute('data-elis-filled', 'false');
            }
        }
    }
}

// Функция для обновления колонки "Печать" при ручном изменении значения
function updatePrintColumnFromInput(valueInput) {
    try {
        let iframe = document.querySelector('.FR');
        
        // Вызываем функцию обновления в iframe
        if (iframe.contentWindow.updatePrintValueOnHalChange) {
            iframe.contentWindow.updatePrintValueOnHalChange(valueInput);
        } else {
            // Fallback к старой логике, если функция не найдена
            let parameterKey = valueInput.dataset.key;
            let printCell = iframe.contentWindow.document.querySelector(`[data-parameter-key="${parameterKey}"]`);
            if (printCell) {
                printCell.setAttribute('data-print-value', valueInput.value || '-');
                printCell.setAttribute('data-elis-filled', 'false');
                
                let printInput = printCell.querySelector('.print-cell-input');
                if (printInput) {
                    printInput.value = valueInput.value || '-';
                    printInput.style.backgroundColor = '';
                    printInput.setAttribute("data-elis-filled", "false");
                    removeElisHighlight(printInput);
                } else {
                    printCell.textContent = valueInput.value || '-';
                    printCell.style.backgroundColor = '';
                }
            }
        }
    } catch (error) {
        console.error('Ошибка обновления колонки "Печать" при ручном изменении:', error);
    }
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

function OpenElisDialog() {
    $('#listPassports').empty();
    localStorage.removeItem('dataPassport');
    UpdateElisApplyButtonState();
}

function ApplicationSecurity(tagName, tagValue) {

    if (tagName == 'root.ARM.Reports.ShowEditAndSave') {
        isShowEditAndSave = tagValue;
    }   
    else if (tagName == 'root.ARM.Reports.AllowEditAndSave') {
        isAllowEditAndSave = tagValue;
    }
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
    UpdateElisApplyButtonState();
}

//Управление состоянием кнопки "Применить" в диалоге ЕЛИС
function UpdateElisApplyButtonState() {
    const applyButton = $('#elisApplyButton');
    const hasSelectedPassport = $('#listPassports .list-group-item.active').length > 0;
    
    applyButton.prop('disabled', !hasSelectedPassport);
}

String.prototype.toFloat = function (value) {
    return parseFloat(this.replace(',', '.').trim())
}


function ToggleMode() {
    if(isViewing)
        GetEditDoc();
    else
        GetDoc();
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
    IsDefault;

    constructor(pId, pUse, pIdParameter, pName, pLimitValueActivate, pLimitValue, pLimitValueString, pIsDefault) {
        this.Id = pId;
        this.Use = pUse;
        this.IdParameter = pIdParameter;
        this.Name = pName;
        this.LimitValueActivate = pLimitValueActivate;
        this.LimitValue = pLimitValue;
        this.LimitValueString = pLimitValueString;
        this.IsDefault = pIsDefault;
    }
}

class LabDocumentInfo
{
    Type;
    Number;
    Date;
    
    constructor(pType, pNumber, pDate) {
        this.Type = pType;
        this.Number = pNumber;
        this.Date = pDate;
    }
}

function updateSaveBtnText() {
    const button = $('#ButtonSave');
    
    $.ajax({
        url: 'Home/GetSaveBtnText',
        type: 'GET',
        data: {
            IdDevice: $('#ComboboxDevice').val(),
            IdDoc: $('#ComboboxDocGUID').val()
        },
        success: function(text) {
            button.val(text);
        },
        error: function() {
            button.val('Сохранить');
        }
    });
}

function showError(message) {
    const errorDialog = document.getElementById('errorDialog');
    const errorMessage = document.getElementById('errorMessage');
    errorMessage.textContent = message;
    errorDialog.showModal();
}

function logToServer(level, message) {
    $.ajax({
        url: '/Home/LogClientMessage',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ level: level, message: message }),
        error: function() { /* опционально: обработка ошибок отправки лога */ }
    });
}

function logInfo(message)  { logToServer('Info', message); }
function logWarn(message)  { logToServer('Warn', message); }
function logError(message) { logToServer('Error', message); }
function logDebug(message) { logToServer('Debug', message); }
function logTrace(message) { logToServer('Trace', message); }
