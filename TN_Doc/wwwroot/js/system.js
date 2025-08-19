/**
 Переопределение окна для подтверждения действий пользователя c возможность выбора окна через параметр opt
 @desc  opt:
 @desc {
 @desc   'title' : 'Внимание', <- заголовок окна
 @desc   'yesBtnName' : 'Да', <- Название кнопки с положительным ответом
 @desc   'noBtnName'  : 'Нет', <- Название кнопки с отрицательным ответом
 @desc }
 */
function RedefinitionConfirmWindow() {
    window.confirm = (message, opt) => {
        let title = 'Внимание';
        let yesBtnName = 'Да';
        let noBtnName = 'Нет';
        let iconClass = 'fa fa-exclamation-circle text-success';
        if (opt) {
            title = opt['title'] ? opt['title'] : title;
            yesBtnName = opt['yesBtnName'] ? opt['yesBtnName'] : yesBtnName;
            noBtnName = opt['noBtnName'] ? opt['noBtnName'] : noBtnName;
            iconClass = opt ['iconClass'] ? opt['iconClass'] : iconClass;
        }

        $('#PromiseConfirm .modal-title span').html(title);
        $('#PromiseConfirm .modal-title i').addClass(iconClass);
        $('#PromiseConfirm .modal-footer .confirm-ok-btn').html(yesBtnName);
        $('#PromiseConfirm .modal-footer .confirm-cancel-btn').html(noBtnName);
        $('#PromiseConfirm .modal-body').html(message);
        
        // Скрытие кнопки "Нет" если передан null
        if (opt && opt['noBtnName'] === null) {
            $('#PromiseConfirm .modal-footer .confirm-cancel-btn').hide();
        } else {
            $('#PromiseConfirm .modal-footer .confirm-cancel-btn').show();
        }

        let PromiseConfirm = $('#PromiseConfirm').modal({
            keyboard: false,
            backdrop: 'static'
        }).modal('show');

        let confirm = false;

        $('#PromiseConfirm .confirm-ok-btn').on('click', e => {
            confirm = true;
        });

        return new Promise(function (resolve, reject) {
            PromiseConfirm.on('hidden.bs.modal', (e) => {
                resolve(confirm);
            });
        });
    };

}

/**
 *  Расчет хеша объекта
 *  @param {object} obj - объект для которого рассчитывается хэш
 *  @return {number } - хэш код функции
 */
function GetObjectHashCode(obj) {
    let js = JSON.stringify(obj);
    let hash = 0;
    if (js.length === 0) {
        return hash;
    }
    for (let i = 0; i < js.length; i++) {
        let char = js.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash; // преобразование в 32-битное целое число
    }
    return hash;
}


//
// // Пример использования функции getObjectHashCode()
// let obj = {name: 'John', age: 23};
// let obj2 = {name: 'John', age: 25};
// let hash = GetObjectHashCode(obj);
// let hash2 = GetObjectHashCode(obj2);
// console.log(hash);
// console.log(hash2);
// console.log(hash === hash2);