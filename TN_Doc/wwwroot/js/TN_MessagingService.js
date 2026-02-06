let Url;
const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5010/SignalRApp")
    //.WithAutomaticReconnect()
    .build();

function ConnectToHub(url) {
    Url = url;
    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(Url)
        //.WithAutomaticReconnect()
        .build();

    hubConnection.on("Receive", function (deviceName, tagName, tagValue) {
        //if (deviceName == CurrentDeviceName) {
        //    UpdateFormElementValue(tagName, tagValue);
        //}
    });

    hubConnection.start();
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

function ReadTag(tagName, namespaceIndex = 2, indexArray = 0) {
    if (CurrentDeviceName == undefined) return undefined;

    var url = "http://localhost:5010/api/Values/";
    var result;
    $.ajax(
        {
            async: false,
            url: url + CurrentDeviceName + '/' + tagName + '/' + namespaceIndex + '/' + indexArray,
            type: "Get",
            success: function (data) {
                result = data;
            },
        });
    return result;
}

function ReadTagCache(tagName, namespaceIndex = 2, indexArray = 0) {
    if (CurrentDeviceName == undefined) {
        logWarn('ReadTagCache: CurrentDeviceName не определен');
        return undefined;
    }

    var url = "http://localhost:5010/api/OPCClientCache/";
    var result;

    try {
        $.ajax({
            async: false,
            url: url + CurrentDeviceName + '/' + tagName + '/' + namespaceIndex + '/' + indexArray,
            type: "Get",
            success: function (data) {
                result = data;
            },
            error: function(xhr, status, error) {
                if (xhr.status === 500 || xhr.status === 404) {
                    logError(`ReadTagCache failed: ${error} (${status}) for tag ${tagName}`);
                }
                result = undefined;
            }
        });
    } catch (ex) {
        logError(`ReadTagCache exception for tag ${tagName}: ${ex.message || ex}`);
        result = undefined;
    }

    return result;
}