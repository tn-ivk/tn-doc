
function GetUsers()
{
    var dic;
    $.ajax(
        {
            async: false,
            url: "http://localhost:5000/Home/GetListUsers",
            type: "GET",
            success: function (data)
            {                
                dic = JSON.parse(data);
            }
        });

    return dic;
}

function SaveDoc(NameDevice, GuidDevice, DocGUID, IdDoc, PrefixTag) {
    var params = [];
    var result = {};
    //var metods = [];
    //var values = [];

    $("select, input").each(function () {
        if ($(this).attr('data-edit') == "1") {
            param = {};
            param["Key"] = $(this).attr('data-key');
            param["Tag"] = $(this).attr('data-tag');

            if ($(this)[0].nodeName == "SELECT")
                param["Value"] = $(this)[0].options[$(this)[0].selectedIndex].text;
            else if ($(this)[0].nodeName == "INPUT")
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

function GetFullNameTag(tagName, PrefixTag) {
    return PrefixTag + '.' + tagName;
}