$(document).ready(function () {
    resetHeaderMargin();
});


// Changes margin of the "employees" table depending on height of "header" table.
function resetHeaderMargin() {
    $("#employees").css("margin-top", $("#header").css("height"));
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

        var $employeesTable = $("#employees").children();
        $employeesTable.append("<tr>" + $("#hiddenEditRow").html() + "</tr>");

        var $row = $employeesTable.children().last();
        Row.selectRow($row);
        $row.css("background", COLORS.TR.EDIT_COLOR);

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

        $row.css("background", COLORS.TR.EDIT_COLOR);

        var personalData = [];
        for (var i = 0; i < 7; i++) {
            personalData[i] = $row.children().eq(i).text();
        }

        var $attestationElements = $row.children().eq(7).children();
        var attestations = [];
        for (i = 0; i < $attestationElements.length; i++) {
            attestations[i] = $attestationElements.eq(i).text();
        }

        originalContent = $row.html();
        $row.html($("#hiddenEditRow").html());

        $rowChildren = $row.children();
        $rowChildren.first().html(personalData[0]);
        for (i = 1; i < 7; i++) {
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
        if (button.value == "Сохранить") {
            var $rowChildren = $row.children();
            var $lastNameInput = $rowChildren.eq(1).children().first(),
                $firstNameInput = $rowChildren.eq(2).children().first(),
                $patronymicInput = $rowChildren.eq(3).children().first();

            var validLastName = validateName($lastNameInput.val()),
                validFirstName = validateName($firstNameInput.val()),
                validPatronymic = validatePatronymic($patronymicInput.val());

            if (!(validLastName && validFirstName && validPatronymic)) {
                colorTextInput($lastNameInput, validLastName);
                colorTextInput($firstNameInput, validFirstName);
                colorTextInput($patronymicInput, validPatronymic);

                ErrorBar.showErrorBar("Данные введены неверно");

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
                Session: $rowChildren.eq(4).children().first().val(),
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

            if (button.value == "Сохранить") {

                $row.html($("#employees").children().children().first().html());
                placeDataIntoRow($row, null);

                $row.mouseover(function () {
                    Row.selectRow($row);
                });

                $row.mouseout(function () {
                    Row.releaseRow($row);
                });

            } else if (button.value == "Отмена") {
                $("#employees").children().children().last().remove();
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


    function colorTextInput($input, isValid) {
        if (isValid) {
            $input.css({
                "background-color": "white",
                "color": "black",
                "border-color": "#CCCCCC",
                "border-width": "1px"
            });
        } else {
            $input.css({
                "background-color": "#FFCCCC",
                "color": "#FF5555",
                "border-color": "#FF5555",
                "border-width": "2px"
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

    const BAR_HEIGHT = 50;

    var shown = false;

    var ErrorBar = {};


    ErrorBar.showErrorBar = function (message) {
        $("#errorBar").css("visibility", "visible").css("height", BAR_HEIGHT + "px");
        $("#errorBar td").css("height", BAR_HEIGHT + "px").html(message);
        resetHeaderMargin();
        document.body.scrollTop += BAR_HEIGHT;
        shown = true;
    }


    ErrorBar.hideErrorBar = function () {
        $('#errorBar').css('visibility', 'hidden').css('height', '0');
        $('#errorBar td').css('height', '0').html('');
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

        for (var i = 0; i < $rowChildren.length - 1; i++) {
            $rowChildren.eq(0).empty();
        }

    } else {

        $rowChildren.eq(0).text(data.Id);
        $rowChildren.eq(1).text(data.LastName);
        $rowChildren.eq(2).text(data.FirstName);
        $rowChildren.eq(3).text(data.Patronymic);
        $rowChildren.eq(4).text(data.Session);
        $rowChildren.eq(5).text(data.Shift);
        $rowChildren.eq(6).text(data.AmountOfWorkingHours);

        $rowChildren.eq(7).empty();
        for (var i = 0; i < data.Attestations.length; i++) {
            $rowChildren.eq(7).append('<div class="specialization">' + data.Attestations[i].Specialization + '</div>');
        }

    }   
}


// Saves a selected row. 
var Row = (function () {

    var $currentRow = null;

    var Row = {}

    Row.selectRow = function ($row) {
        if (!EditManager.editMode()) {
            $currentRow = $row;
            $row.css("background", COLORS.TR.SELECTION_COLOR);
            $row.children().last().children().each(function (index, element) {
                $(element).css("visibility", "visible");
            });
        }
    }

    Row.releaseRow = function ($row) {
        if (!EditManager.editMode()) {
            $row.css("background", COLORS.TR.ORIGINAL_COLOR);
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


var COLORS = {}
COLORS.TR = {
    ORIGINAL_COLOR: 'white',
    SELECTION_COLOR: '#EFEFEF',
    EDIT_COLOR: 'linear-gradient(#FFEEBB, #FFEE99)',
}