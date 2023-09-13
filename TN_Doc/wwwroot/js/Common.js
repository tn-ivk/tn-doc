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
            accept:"application/json",
            type: "GET",
            success: function (data) {
                data.forEach((item) => {
                    console.log(item+ "->"+  item.replaceAll(" ","_"))
                    
                    $('#ComboboxPrinterName').append('<option value=' + item.replaceAll(" ","_") + '>' + item + '</option>');
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
            }
            else {
                GetDoc();
            }
        }
    });
}

function InitElement() {
    dictFetchOptions = {
        getUrlDir: '/direditor/getdir',
        setUrlDir: '/direditor/setdir',
        getMethod: 'GET',
        setMethod: 'POST'
    }
    

    /*init dir editor component script*/
    AddTabsSelectorHandler();
    LoadAppDictionaries();
    AddDitctionariesHandler();
    RenderUserGroupsRowTable();
    RenderAndAddHandlerLicencesTable();
    RenderAndAddHandlerUserTable();
    AddLicHandler();
    AddUserHandler();
    AddSaveButtonHandler();
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
                }
                else {
                    $('#ButtonSave').prop('disabled', false);
                    $('#ButtonReview').prop('disabled', false);
                    $('#ButtonEdit').prop('disabled', false);
                }

                if ($('#ComboboxDocGUID').val() == 1) {                    
                    $('#ButtonElis').prop('hidden', !IsUsedElis); 
                }
                else
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
                printerName: $('#ComboboxPrinterName').val().replaceAll("_"," "),
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