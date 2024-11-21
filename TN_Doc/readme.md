# Гайд по TN_Doc

Проект для генерации отчетов по данным из ИВК. Для корректной работы всего проекта необходимы также проекты "TN_KMH" и "TN_MessagingService"．
Необходимые репозитории:

* http://192.168.100.100/orpovy/ivk/tn_doc.git
* http://192.168.100.100/orpovy/ivk/tn_kmh.git
* http://192.168.100.100/orpovy/ivk/tn_messagingservice.git

Лучше генерировать эти проекты из папки develop

После публикации проектов необходимов удалить файл "appsettings.Development.json" иначе  "Asible" будет ругаться.

Проекты "TN_KMH" и "TN_MessagingService"  используют конфигурацию проекта "TN_Doc". Все три проекта  должны лежать в одной директории на одном уровне.

## Взаимодействие c ЕЛИС

Для взаимодействия с ЕЛИС необходимо модуль TN.ElisConnector.
Данный модуль осуществляет взаимодействие с LabHub для получения данных по паспорту качества.

Репозиторий TN.ElisConnector：http://192.168.100.100/orpovy/ivk/tn.elisconnector.git

### Конфигурация TN_Doc

В файле "CfgApp.json" необходимо выставить следующие параметры:

```json
{
  "Elis": {
    "Use": false,
    "OstKey": "ostKey",
    "SiknKey": "siknKey",
    "ClientName": "clientName",
    "ClientToken": ""
  }
}
```

|   Параметр    | Значение                                                  | Примечание                                                                                              |
|:-------------:|:----------------------------------------------------------|:--------------------------------------------------------------------------------------------------------|
|    "Elis"     | Конфигурация подключения к ЕЛИС                           | **Используется в связке с модулем TN.ElisConnector**                                                    |
|     "Use"     | Флаг использования конфигурации для взаимодействия с ЕЛИС | **Для того, чтобы использовать необходимо выставить значение "true"**                                   |
|   "OstKey"    | Ключ ОСТ                                                  | **Данный ключ необъходимо получить у Транеснефть-Технологии. Это ключ для их системы**                  |                                            
|   "SiknKey"   | Ключ  СИКН                                                | **Данный ключ необъходимо получить у Транеснефть-Технологии. Это ключ для их системы**                  |                                               
| "ClientName"  | Имя АРМ                                                   | Необязательный параметр. Используется TN.ElisConnector для выдачи ключа. Может принимать любое значение |                                             
| "ClientToken" | Ключ TN.ElisConnector                                     | Заполнятеся автоматически после выдачи получение ключа от TN.ElisConnector                              |                                             

Полный вид файла "CfgApp.json"

```json
{
  "Devices": [
    {
      "Use": true,
      "IdDevice": 0,
      "Name": "ИВК-1",
      "Description": "",
      "Docs": [],
      "DBConnectionStrings": [],
      "UsedSI": {
        "UsedPR": true,
        "UsedPP": true,
        "UsedPVL": true,
        "UsedPVS": true,
        "UsedSecondSI_PVL": false,
        "UsedSecondSI_PVS": false
      },
      "UsedProcedureTypePR": null
    }
  ],
  "PrintSettings": {},
  "ExportDoc": {},
  "Elis": {
    "Use": false,
    "OstKey": "ostKey",
    "SiknKey": "siknKey",
    "ClientName": "clientName",
    "ClientToken": ""
  },
  "UseSecurityFeatures": false
}
```

Для каждого устройства можно подключать отделбно конфигурацию ЕЛИС. Тогда каждый ИВК будет иметь свою конфигурацию для работы с
ЕЛИС.
Выглядит будет следуюшим образом

```json
{
  "Devices": [
    {
      "Use": true,
      "IdDevice": 0,
      "Name": "ИВК-1",
      "Description": "",
      "Docs": [],
      "DBConnectionStrings": [],
      "UsedSI": {
        "UsedPR": true,
        "UsedPP": true,
        "UsedPVL": true,
        "UsedPVS": true,
        "UsedSecondSI_PVL": false,
        "UsedSecondSI_PVS": false
      },
      "Elis": {
        "Use": false,
        "OstKey": "ostKey",
        "SiknKey": "siknKey",
        "ClientName": "clientName",
        "ClientToken": ""
      },
      "UsedProcedureTypePR": null
    }
  ],
  "PrintSettings": {},
  "ExportDoc": {},
  "Elis": null,
  "UseSecurityFeatures": false
}
```

### Конфигурация TN.ElisConnector

Данный модуль используется для работы с ТСПД модулем "LabHub".\
Получение паспортов из ЕЛИС происходми именно через этот модуль.
Ниже приведены настройки данного модуля:

```json
{
  "TSPDManagerSettings": {
    "PermissibleTryCounter": 10,
    "MonitorWaitingTimeMillSec": 500,
    "ProxyPrimaryConnectionSettings": {
      "UseHttps": true,
      "PfxName": "127.0.0.1.wp.pfx",
      "PfxPass": "test_password",
      "HubUrl": "https://localhost:5051/tspd.mock.hub",
      "HubMethodName": "GetRandomQualityPassport",
      "CloseTimeoutSec": 5,
      "KeepAliveIntervalSec": 15,
      "ServerTimeoutSec": 60,
      "HandshakeTimeoutSec": 15
    },
    "ProxyAlternativeConnectionSettings": [
      {
        "UseHttps": true,
        "PfxName": "server.crt",
        "HubUrl": "https://172.30.2.1:43444/labhub",
        "HubMethodName": "GetRandomQualityPassport",
        "CloseTimeoutSec": 5,
        "KeepAliveIntervalSec": 15,
        "ServerTimeoutSec": 30,
        "HandshakeTimeoutSec": 15
      }
    ]
  }
}
```

Основные настройки подключения：

|               Параметр               | Значение                                                        | Примечание                                                                                                                                                   |
|:------------------------------------:|:----------------------------------------------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------|
|       "PermissibleTryCounter"        | Максиммальное допустимое количество попыток выполнить операцию. | Определяет число попыток выполнения одной операции. Обычно все выполняется с первой попытки. Необходимо чтобы не словить deadlock. <br/>**Лучше не трогать** |
|     "MonitorWaitingTimeMillSec"      | Время ожидания монитора                                         | Время ожидания осовобождения монитора у хранилища конифгураций. <br/>**Лучше не трогать**                                                                    |
|   "ProxyPrimaryConnectionSettings"   | Настройки конфигурации  подключения к LabHub                    | Основные настройки подключения  к LabHub.  **Описание приведены в таблице ниже**                                                                             |                                            
| "ProxyAlternativeConnectionSettings" | Альтернативные пути подключения к LabHub                        | Альтернативные пути подключения к LabHub. Это массив с подлкючений.  Необходим когда по основному подключению возникают проблемы.                            |                                            

Структура конфигурации подключений приведена в таблице ниже. Конфигурация "ProxyPrimaryConnectionSettings" идентична элементам
массива "ProxyAlternativeConnectionSettings".

ProxyPrimaryConnectionSettings:

|        Параметр        | Значение                                                               | Примечание                                                                                                                                                                                                                                                                                    |
|:----------------------:|:-----------------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|       "UseHttps"       | Флаг использования протокола https                                     | Флаг для определеня используемого протокола общения. <br/>Если выставлено  true -  то будет использоваться https, иначе  http                                                                                                                                                                 |
|       "PfxName"        | Имя сертификата/архива ssl                                             | Название сертификата для взаимодействия с Lab. Сертификат необходимо получить от хозяевов LabHub и поместить его в папку "Cert"<br/> Установить сертификат необходимо в "Доверенный коренвой центр" компьютера. <br/> **Если используется протокол http,  то можно оставить это поле пустым** |
|       "PfxPass"        | Парольсертификата                                                      | Пароль от сертификата. Если сертификат без пароля, то поле необъходимо оставить пустым .                                                                                                                                                                                                      |
|        "HubUrl"        | Url хаба LabHub                                                        | Адрес подключения к LabHub                                                                                                                                                                                                                                                                    |                                           
|    "HubMethodName"     | Имя метода хаба                                                        | Название метода в хабе у LabHub                                                                                                                                                                                                                                                               |                                            
|   "CloseTimeoutSec"    | Время ожидания ответного пакета с подтверждением закрития канала связи | Параметр по умолчанию для TCP **Лучше не трогать**                                                                                                                                                                                                                                            |             
| "KeepAliveIntervalSec" | Время ожидания пакета KeepAlive                                        | Параметр по умолчанию для TCP **Лучше не трогать**                                                                                                                                                                                                                                            |             
|   "ServerTimeoutSec"   | Максимальное время ожидания ответа от LabHub                           | Параметр по умолчанию  для TCP **Лучше не трогать**                                                                                                                                                                                                                                           |             
| "HandshakeTimeoutSec"  | Максимальное время ожидания ответа при "рукопожатие"                   | Параметр по умолчанию  для TCP **Лучше не трогать**                                                                                                                                                                                                                                           |             

#### Доп. репа.
Внутри репозитория есть TSPD.Mock.Hub. Это специальный проект для тестирования. Он имитирует работу  LabHub. Конфигурация  приведенная выше описывает взаимодействие с ним.

## Взаимодействие по OPC DA

Обычно взаимодействие с ИВК происходит по протоколу OPC UA. Но в редких случаях необходимо взаимодействовать по протоколу OPC DA (
старые версии ИВК).
Необходим модуль TN_Messanging
Репозиторий:  http://192.168.100.100/orpovy/ivk/tn_messagingservice.git
Для взаимодействия по OPC DA необходима ветка **"samara_build"** (не успел интегрировать в основную ветку).

Модуль из данной ветки умеет общаться по OPC DA. В файл "CfgApp.json" необьходимо добавить следующий объект:

```json
{
  "OpcConnectionSettings": {
    "Type": 1,
    "DaSettings": {
      "StartPrefix": "Root.PLC1.IVK_TN_01",
      "Host": "localhost",
      "ProgId": "psregualopcda_01",
      "UpdateRate": 500
    },
    "UaSettings": {
      "ConfigFilename": "Config.xml",
      "UpdateRate": 500,
      "StartPrefix": "IVK_TN_01"
    }
  }
}
```

|          Параметр           | Значение                          | Примечание                                                                      |
|:---------------------------:|:----------------------------------|:--------------------------------------------------------------------------------|
|           "Type"            | Тип используемого протокола       | Тип используемоего протокола. 0 - OPC  DA, 1 - OPC UA                           |
|        "DaSettings"         | Конфигурация для протокола OPC DA | Конфигурация для протокола OPC DA                                               |
|  "DaSettings.StartPrefix"   | Стартовый префикс тегов           | Префикс подставляется в начало каждого тега. Используется для построения дерева |
|      "DaSettings.Host"      | Адрес подключения                 | Адрес сервера OPC DA. Значение по умолачнию "localhost". **Лучше не трогать**   |
|     "DaSettings.ProgId"     | Идентификатор программы           | Значение по умолачнию "psregualopcda_01".                                       |
|   "DaSettings.UpdateRate"   | Частота обновления данных         |                                                                                 |
|        "UaSettings"         | Конфигурация для протокола OPC UA | Конфигурация для OPC UA                                                         |
| "UaSettings.ConfigFilename" | Названия файла конфигурации       | Файл располагается внутри программы. В нем содержится конфигурация              |                                           
|   "UaSettings.UpdateRate"   | Частота обновления данных         |                                                                                 |                                            
|  "UaSettings.StartPrefix"   | Стартовый префикс тегов           | рефикс подставляется в начало каждого тега. Используется для построения дерева  |

Данный тег объект подставляется для каждого ИВК. Общая структура файла будет выглядить так:

```json

{
  "Devices": [
    {
      "Use": true,
      "IdDevice": 0,
      "Name": "ИВК-1",
      "Description": "",
      "Docs": [],
      "DBConnectionStrings": [],
      "OpcConnectionSettings": {
        "Type": 1,
        "DaSettings": {
          "StartPrefix": "Root.PLC1.IVK_TN_01",
          "Host": "localhost",
          "ProgId": "psregualopcda_01",
          "UpdateRate": 500
        },
        "UaSettings": {
          "ConfigFilename": "Config.xml",
          "UpdateRate": 500,
          "StartPrefix": "IVK_TN_01"
        }
      },
      "UsedSI": {
        "UsedPR": true,
        "UsedPP": true,
        "UsedPVL": true,
        "UsedPVS": true,
        "UsedSecondSI_PVL": false,
        "UsedSecondSI_PVS": false
      },
      "UsedProcedureTypePR": null
    }
  ],
  "PrintSettings": {},
  "ExportDoc": {},
  "Elis": {
    "Use": false,
    "OstKey": "ostKey",
    "SiknKey": "siknKey",
    "ClientName": "clientName",
    "ClientToken": ""
  },
  "UseSecurityFeatures": false
}

```

Теги для взаимодействия по протоколам хранятся в файлах
* opc.da.tags.json
* opc.ua.tags.json

В данных файлах лежат протоколы для OPC DA и OPC UA. Теги делятся на две группы:  с подпиской и без подписки:
* SubscriptionTags - теги с подпиской. При изменении значений будет прилетать уведомление.
* UnsubscriptionTags - теги без подписки. При изменении уведомления приъодить не будут.

#### OPC DA  trouble

Если теги  не были прописаны в файле "opc.da.tags.json", то будет выскакивать ошибка при попытки их чтения/записи.
Необходимо заранее  прописать все известные теги в файл. C OPC UA такой проблемы нет.


## Равертывания на  OC Windows

Программы "TN_Doc","TN_KMH", "TN_MessagingService" и "TN.ElisConnector" необходимо развернуть как службы. Для этого необходимо открыть консоль от имени администратора
и использовать следующую команду

```cmd
sc create <ИМЯ_Службы> binPath="Путь/до/исполняемого/файла" start=auto
```

После запуска служб необходимо авторизовать пользователя "NT AUTHORITY\NETWORK SERVICE". Пароля у пользователя нет (необходимо удалить все  кружочки перед авторизацией).
Авторизоваться нужно в самой оснастке служб. Выбираешь службу правой кнопуой мыши и выбираешь пункт "Свойства". Во вкладке "Вход систему" небходимо ввести данные "NT AUTHORITY\NETWORK SERVICE".




http://192.168.100.100/orpovy/ivk/tn_messagingservice/-/blob/samara_build/TN_MessagingService


