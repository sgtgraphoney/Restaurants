﻿$(document).ready(function () {
    resetHeaderMargin();
});


// Changes margin of the "employees" table depending on height of "header" table.
function resetHeaderMargin() {
    $(Constants.Tables.EMPLOYEES).css("margin-top", $(Constants.Tables.HEADER).css("height"));
}


// Contains methods and variables that are responsible for changing UI while editing.
var EditManager = (function () {

    var edit = false;
    var originalContent = null;

    var EditManager = {}

    EditManager.editMode = function () {
        return edit;
    }


    // It must be invoked if user clicks on the "Add" button.
    // Adds a new row with input fields.
    EditManager.addNew = function (button) {
        if (edit) {
            return;
        }

        var $employeesTable = $(Constants.Tables.EMPLOYEES).children();
        $employeesTable.append("<tr>" + $(Constants.Tables.HIDDEN_EDIT_ROW).html() + "</tr>");

        var $row = $employeesTable.children().last();
        Row.selectRow($row);
        $row.children().css("background", Constants.Colors.EDIT_TR_COLOR);

        edit = true;

        $("html, body").animate({
            scrollTop: $row.offset().top
        }, 500);
    }


    // This method must be invoked every time when a user begins editing information.
    // Editing of information can be started by clicking on the "Add" or "Edit" buttons.
    EditManager.startEdit = function (button) {
        if (edit) {
            return;
        }

        var $row = Row.getCurrentRow();

        var personalData = [];
        for (var i = 0; i < 7; i++) {
            if (i == 4) {
                continue;
            }
            personalData[i] = $row.children().eq(i).html();
        }

        var shiftInfo = [];
        shiftInfo[0] = $row.children().eq(4).children().first().text();
        shiftInfo[1] = $row.children().eq(4).children().last().text();

        var $attestationElements = $row.children().eq(7).children();
        var attestations = [];
        for (i = 0; i < $attestationElements.length; i++) {
            attestations[i] = $attestationElements.eq(i).text();
        }

        originalContent = $row.html();
        $row.html($(Constants.Tables.HIDDEN_EDIT_ROW).html());

        $row.children().css("background", Constants.Colors.EDIT_TR_COLOR);

        $rowChildren = $row.children();
        $rowChildren.first().html(personalData[0]);
        $rowChildren.eq(4).children().first().children().first().val(shiftInfo[0]);
        $rowChildren.eq(4).children().last().children().first().val(shiftInfo[1]);
        for (i = 1; i < 7; i++) {
            if (i == 4) {
                continue;
            }
            $rowChildren.eq(i).children().first().val(personalData[i]);
        }

        $attestationElements = $row.children().eq(7).children();
        for (i = 0; i < $attestationElements.length; i++) {
            for (var j = 0; j < attestations.length; j++) {
                if ($attestationElements.eq(i).children().last().text() == attestations[j]) {
                    $attestationElements.eq(i).children().first().prop("checked", true);
                }
            }
        }

        edit = true;
    }


    // This method must be invoked every time when a user finishes edit information.
    // It's when the "Save" or "Cancel" buttons are clicked. 
    EditManager.stopEdit = function (button) {
        if (!edit) {
            return;
        }

        var $row = Row.getCurrentRow();

        // If the "Save" button is clicked, then entered information must be validated and then sent to a server.
        // If incorrect information has been entered, then nothing will be sent.
        if (button.value == Constants.Buttons.SAVE) {
            var $rowChildren = $row.children();
            var $lastNameInput = $rowChildren.eq(1).children().first(),
                $firstNameInput = $rowChildren.eq(2).children().first(),
                $patronymicInput = $rowChildren.eq(3).children().first(),
                $dateInput = $rowChildren.eq(4).children().last().children().first();

            var validLastName = validateName($lastNameInput.val()),
                validFirstName = validateName($firstNameInput.val()),
                validPatronymic = validatePatronymic($patronymicInput.val());
            var validatedDate = validateDate($dateInput.val());

            if (!(validLastName && validFirstName && validPatronymic && validatedDate)) {
                colorTextInput($lastNameInput, validLastName);
                colorTextInput($firstNameInput, validFirstName);
                colorTextInput($patronymicInput, validPatronymic);
                colorTextInput($dateInput, validatedDate);

                ErrorBar.showErrorBar(Constants.ERROR_MESSAGE);

                return;
            }

            var $attestationElements = $rowChildren.eq(7).children();
            var attestations = [],
                count = 0;
            for (var i = 0; i < $attestationElements.length; i++) {
                if ($attestationElements.eq(i).children().first().prop("checked")) {
                    attestations[count++] = {
                        Specialization: $attestationElements.eq(i).children().last().text(),
                    }
                }
            }

            // Id "-1" is assigned to a new employee. 
            sendUpdatedEmployee({
                Id: $rowChildren.eq(0).text() || -1,
                FirstName: $firstNameInput.val(),
                Patronymic: $patronymicInput.val(),
                LastName: $lastNameInput.val(),
                Shift: $rowChildren.eq(5).children().first().val(),               
                AmountOfWorkingHours: $rowChildren.eq(6).children().first().val(),
                Session: $rowChildren.eq(4).children().first().children().first().val(),
                FirstWorkingDay: (new Date(validatedDate)).toDateString(),
                Attestations: attestations
            }, $row);
        }

        if (ErrorBar.isShown()) {
            ErrorBar.hideErrorBar();
        }

        edit = false;


        // If a user edits an existing employee, then necessary content of a row has been already created.
        // Therefore the old content returns to the current row. It will be updated when a response from a server comes.
        // Otherwise the current row will be copied from another one. 
        // All text content will be removed and replaced with information from response.
        if (originalContent) {

            $row.html(originalContent);
            originalContent = null;

        } else {

            if (button.value == Constants.Buttons.SAVE) {

                $row.html($(Constants.Tables.EMPLOYEES).children().children().first().html());
                placeDataIntoRow($row, null);

                $row.mouseover(function () {
                    Row.selectRow($row);
                });

                $row.mouseout(function () {
                    Row.releaseRow($row);
                });

            } else if (button.value == Constants.Buttons.CANCEL) {
                $(Constants.Tables.EMPLOYEES).children().children().last().remove();
            }
        }
        
        Row.releaseRow($row);
    }


    function validateName(name) {
        return name.match(/^[A-ZА-Яa-zа-яЁё]+$/);
    }


    function validatePatronymic(name) {
        return name.match(/^[A-ZА-Яa-zа-яЁё]*$/);
    }


    function validateDate(dateString) {
        var pattern = /^\d?\d\.\d?\d\.\d{1,4}$/;
        if (pattern.test(dateString)) {
            var day = parseInt(/^(\d+)/.exec(dateString)[1]);
            var month = parseInt(/^\d+\.(\d+)/.exec(dateString)[1]);
            var year = /(\d+)$/.exec(dateString)[1];
            if (/^0{2,}/.test(year)) {
                return null;
            }

            year = parseInt(year);

            var now = (new Date()).getFullYear();
            if (month > 0 && month <= 12 && day > 0 && day <= daysInMonth[month - 1] &&
                (year >= 1970 && year <= now) || ((year + "").length <= 2 && year <= now % 100)) {
                return month + "." + day + "." + year;
            } else {
                return null;
            }
        } else {
            return null;
        }
        
    }


    var daysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];


    function colorTextInput($input, isValid) {
        if (isValid) {
            $input.css({
                "background-color": Constants.Colors.TextInput.BACKGROUND_COLOR,
                "color": Constants.Colors.TextInput.TEXT_COLOR,
            });
        } else {
            $input.css({
                "background-color": Constants.Colors.TextInput.ERR_BACKGROUND_COLOR,
                "color": Constants.Colors.TextInput.ERR_TEXT_COLOR,
            });
        }
    }


    return EditManager;

})();


function setCorrectCase(input) {
    if (input.value.length > 0) {
        input.value = input.value[0].toUpperCase() + input.value.substr(1).toLowerCase();
    }
}


// Contains a method that shows the error bar and a method that hides the error bar.
var ErrorBar = (function () {

    const BAR_HEIGHT = 34;

    var shown = false;

    var ErrorBar = {};


    ErrorBar.showErrorBar = function (message) {
        $("#errorBar").css("visibility", "visible").css("height", BAR_HEIGHT + "px");
        $("#errorBar td").css("height", BAR_HEIGHT + "px").eq(1).text(message);
        resetHeaderMargin();
        document.body.scrollTop += BAR_HEIGHT;
        shown = true;
    }


    ErrorBar.hideErrorBar = function () {
        $("#errorBar").css("visibility", "hidden").css("height", "0");
        $("#errorBar td").css("height", "0").eq(1).text("");
        resetHeaderMargin();
        document.body.scrollTop -= BAR_HEIGHT;
        shown = false;
    }


    ErrorBar.isShown = function () {
        return shown;
    }


    return ErrorBar;

})();


function sendUpdatedEmployee(employee, $row) {
    $.ajax({
        type: 'POST',
        dataType: 'json',
        accept: 'application/json',
        url: '/Employees/Update',
        data: employee,
        success: function (result) {
            if (result.success) {
                placeDataIntoRow($row, result.data);
            } else {
                ErrorBar.showErrorBar(result.message);
            }            
        }

    })
}


function sendDeleteReqest(id, $row) {
    $.ajax({
        type: 'POST',
        accept: 'application/json',
        url: '/Employees/Delete',
        data: { id: id },
        success: function (result) {
            if (result.success) {
                $row.remove();
            } else {
                ErrorBar.showErrorBar(result.message);
            }
        }
    })
}


function deleteEmployee(button) {
    var $row = $(button).parent().parent();
    var id = $row.children().first().text();
    sendDeleteReqest(id, $row);
}


function placeDataIntoRow($row, data) {
    $rowChildren = $row.children();

    if (data === null) {

        $shiftInfoChildren = $rowChildren.eq(4).children();
        $shiftInfoChildren.first().empty();
        $shiftInfoChildren.last().empty();

        for (var i = 0; i < $rowChildren.length - 1; i++) {
            if (i == 4) {
                continue;
            }
            $rowChildren.eq(i).empty();
        }

    } else {

        $rowChildren.eq(0).text(data.Id);
        $rowChildren.eq(1).text(data.LastName);
        $rowChildren.eq(2).text(data.FirstName);
        $rowChildren.eq(3).text(data.Patronymic);

        $sessionInfo = $rowChildren.eq(4).children();
        $sessionInfo.first().text(data.Session);
        $sessionInfo.last().text(formatDate(new Date(data.FirstWorkingDay)));

        $rowChildren.eq(5).text(data.Shift);
        $rowChildren.eq(6).text(data.AmountOfWorkingHours);

        $rowChildren.eq(7).empty();
        for (var i = 0; i < data.Attestations.length; i++) {
            $rowChildren.eq(7).append('<div class="specialization">' + data.Attestations[i].Specialization + '</div>');
        }

    }   
}


function formatDate(date) {
    var day = date.getDate();
    if (day < 10) {
        day = "0" + day;
    }

    var month = date.getMonth() + 1;
    if (month < 10) {
        month = "0" + month;
    }

    var year = date.getFullYear();

    return day + "." + month + "." + year;
}


// Saves a selected row. 
var Row = (function () {

    var $currentRow = null;

    var Row = {}

    Row.selectRow = function ($row) {
        if (!EditManager.editMode()) {
            $currentRow = $row;
            $row.children().css("background", Constants.Colors.SELECTION_TR_COLOR);
            $row.children().last().children().each(function (index, element) {
                $(element).css("visibility", "visible");
            });
        }
    }

    Row.releaseRow = function ($row) {
        if (!EditManager.editMode()) {
            $row.children().css("background", Constants.Colors.ORIGINAL_TR_COLOR);
            $row.children().last().children().each(function (index, element) {
                $(element).css("visibility", "hidden");
            });
            $currentRow = null;
        }
    }

    Row.getCurrentRow = function () {
        return $currentRow;
    }

    return Row;

})();


var Constants = {}

Constants.Colors = {
    ORIGINAL_TR_COLOR: "#F5F5F5",
    SELECTION_TR_COLOR: "#EBEBEB",
    EDIT_TR_COLOR: "#E0E0E0",

    TextInput: {
        BACKGROUND_COLOR: "white",
        TEXT_COLOR: "#444444",
        ERR_BACKGROUND_COLOR: "#FF9999",
        ERR_TEXT_COLOR: "#AA3333",
    }
};

Constants.Buttons = {
    ADD: "Добавить",
    SAVE: "Сохранить",
    CANCEL: "Отмена"
};

Constants.ERROR_MESSAGE = "Данные введены неверно";

Constants.Tables = {
    HEADER: "#header",
    EMPLOYEES: "#employees",
    HIDDEN_EDIT_ROW: "#hiddenEditRow"
}
