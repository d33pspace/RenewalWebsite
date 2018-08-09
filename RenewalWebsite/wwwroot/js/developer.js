function ConfirmMsg(id, triggerClick, title, message) {
    $("#" + triggerClick).attr("data-id", id);

    $.confirm({
        title: title,
        content: message,
        theme: 'modern',
        type: 'orange',
        typeAnimated: true,
        closeIcon: true,
        columnClass: 'jconfirm-small',
        buttons: {
            tryAgain: {
                text: 'Confirm',
                btnClass: 'btn-red',
                action: function () {
                    document.getElementById(triggerClick).click();
                }
            },
            cancel: function () {
            }
        }
    });

    return this;
};