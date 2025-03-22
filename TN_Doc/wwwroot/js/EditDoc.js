
function GetUsers() {
    var dic;
    $.ajax(
        {
            async: false,
            url: "http://localhost:5000/Home/GetListUsers",
            type: "GET",
            success: function (data) {
                dic = JSON.parse(data);
            }
        });

    return dic;
}

function GetInvalideChars() {
    var chars;
    $.ajax(
        {
            async: false,
            url: "http://localhost:5000/Home/GetInvalideChars",
            type: "GET",
            data: {
                IdDevice: $('#ComboboxDevice').val()
            },
            success: function (data) {
                chars = JSON.parse(data);
            }
        });
    return chars;
}

function SaveDoc(NameDevice, GuidDevice, DocGUID, IdDoc, PrefixTag) {
    return new Promise((resolve, reject) => {
        Spinner();
        Spinner.show();
        let params = [];
        let result = {};
        let startTime = Date.now(); // Время начала опроса
        const maxDuration = 5000; // Максимальное время опроса (5000 мс)
        const pollInterval = 500; // Период опроса (500 мс)

        $("select, input").each(function () {
            if ($(this).attr('data-edit') == "1") {
                let param = {};
                param["Key"] = $(this).attr('data-key');
                param["Tag"] = $(this).attr('data-tag');
                if ($(this)[0].nodeName == "SELECT") {
                    if ($(this)[0].selectedIndex == -1)
                        param["Value"] = "";
                    else
                        //param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].text;
                        /////////
                    if ($(this).attr('data-tag') == 'Metod') {
                        param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].dataset.metod;
                        //param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].value;
                    } else {
                        param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].text;
                    }
                    /////////
                } else if ($(this)[0].nodeName == "INPUT") {
                    if ($(this).attr('data-tag') == 'DocNum') {
                        param["Value"] = $(this)[0].dataset.document;
                    }
                    else {
                        param["Value"] = $(this).val();    
                    }
                }
                    

                params.push(param);
            }
        });

        result["DocID"] = IdDoc;
        result["values"] = params;

        $.ajax(
            {
                async: false,
                url: "http://localhost:5000/Home/SaveDoc",
                type: "POST",
                dataType: 'json',
                data: {
                    IdDevice: GuidDevice,
                    IdDoc: DocGUID,
                    data: JSON.stringify(result)
                },
                success: function (data) {

                },
                error: function (data) {

                }
            });

        if (DocGUID == 1) {
            const lastResult = ReadTag(NameDevice, GetFullNameTag('ARM.ARM_FillActAndPassportResult', PrefixTag), 2, 0);
            WriteTag(NameDevice, GetFullNameTag('ARM.ARM_FillActAndPassport', PrefixTag), true, 2, 0);

            const intervalId = setInterval(() => {
                const curResult = ReadTag(NameDevice, GetFullNameTag('ARM.ARM_FillActAndPassportResult', PrefixTag), 2, 0);
                const currentTime = Date.now();

                const shouldStop = curResult > lastResult;
                if (shouldStop) {
                    clearInterval(intervalId); 
                    Spinner.hide();
                    
                    try {
                        const passportData = JSON.parse(localStorage.getItem('dataPassport'));
                        if (passportData) {
                            result = {
                                ...result,
                                ...passportData
                            };
                        }
                    } catch (error) {
                        console.error('Ошибка при объединении данных протокола:', error);
                    }

                    $.ajax({
                        async: false,
                        url: 'http://localhost:5000/Home/UpdateDoc',
                        type: 'POST',
                        dataType: 'json',
                        data: {
                            IdDevice: GuidDevice,
                            IdDoc: DocGUID,
                            data: JSON.stringify(result)
                        },
                        success: function (data) {

                        },
                        error: function (data) {

                        }
                    });
                    resolve(true);
                }
                if ((currentTime - startTime) >= maxDuration) {
                    clearInterval(intervalId);
                    Spinner.hide();
                    resolve(false); 
                }
            }, pollInterval);

        }
    });
}

function SaveDocPassport(NameDevice, GuidDevice, DocGUID, IdDoc, PrefixTag) {
    var params = [];
    var result = {};
    //var metods = [];
    //var values = [];

    $("select, input").each(function () {
        if ($(this).attr('data-edit') == "1") {
            param = {};
            param["Key"] = $(this).attr('data-key');
            param["Tag"] = $(this).attr('data-tag');
            if ($(this)[0].nodeName == "SELECT") {
                if ($(this)[0].selectedIndex == -1)
                    param["Value"] = "";
                else {
                    if ($(this).attr('data-tag') == 'Metod')
                        param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].dataset.metod;
                    else
                        param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].text;
                }
            } else if ($(this)[0].nodeName == "INPUT")
                param["Value"] = $(this).val();

            params.push(param);
        }
    });

    result["DocID"] = IdDoc;
    result["values"] = params;

    $.ajax(
        {
            async: false,
            url: "http://localhost:5000/Home/SaveDoc",
            type: "POST",
            dataType: 'json',
            data: {
                IdDevice: GuidDevice,
                IdDoc: DocGUID,
                data: JSON.stringify(result)
            },
            success: function (data) {

            },
            error: function (data) {

            }
        });

    if (DocGUID == 1)
        WriteTag(NameDevice, GetFullNameTag('ARM.ARM_FillActAndPassport', PrefixTag), true, 2, 0);
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

function ReadTag(GuidDevice, tagName, namespaceIndex = 2, indexArray = 0) {
    if (GuidDevice == undefined) return undefined;

    var url = "http://localhost:5010/api/Values/";
    var result;
    $.ajax(
        {
            async: false,
            url: url + GuidDevice + '/' + tagName + '/' + namespaceIndex + '/' + indexArray,
            type: "Get",
            success: function (data) {
                result = data;
            },
        });
    return result;
}

function GetFullNameTag(tagName, PrefixTag) {
    return PrefixTag + '.' + tagName;
}