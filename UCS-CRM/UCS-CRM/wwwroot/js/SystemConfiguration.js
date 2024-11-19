$(function () {
    // Hide spinner initially
    $("#spinner").hide();

    var datatable = $('#holidaysTable').DataTable({
        "processing": true,
        "serverSide": false,
        "order": [[1, "desc"]],
        ajax: {
            url: '/Manager/SystemConfiguration/GetHolidays',
            type: 'POST',
            datatype: 'Json',
            headers: {
                'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
            },
            beforeSend: function() {
                $("#spinner").show();
            },
            complete: function() {
                $("#spinner").hide();
            },
            error: function (xhr, textStatus, errorThrown) {
                $("#spinner").hide();
                console.error('AJAX Error:', textStatus, errorThrown);
                console.log('Response:', xhr.responseText);
            }
        },
        columnDefs: [
            {
                defaultContent: "",
                targets: "_all",
                'orderable': true
            },
        ],
        columns: [
            {
                data: "name",
                name: "name",
                className: "text-left"
            },
            {
                data: "startDate",
                name: "startDate",
                className: "text-left"
            },
            {
                data: "endDate",
                name: "endDate",
                className: "text-left"
            },
            {
                data: "isRecurring",
                name: "isRecurring",
                className: "text-center",
                render: function(data) {
                    return data ? '<span class="text-success">Yes</span>' : '<span class="text-danger">No</span>';
                }
            },
            {
                data: "id",
                name: "id",
                orderable: false,
                render: function (data) {
                    return `<a href='#' onclick=EditHoliday('${data}') class='feather icon-edit' title='edit'></a>
                           <a href='#' onclick=DeleteHoliday('${data}') class='feather icon-trash text-danger' title='delete'></a>`;
                }
            }
        ],
        responsive: true,
        autoWidth: false
    });

    function toggleCustomDateTime() {
        if ($('#DateConfiguration_UseSystemTime').is(':checked')) {
            $('#customDateTimeDiv').hide();
        } else {
            $('#customDateTimeDiv').show();
        }
    }

    $('#DateConfiguration_UseSystemTime').change(toggleCustomDateTime);
    toggleCustomDateTime();
});

function EditHoliday(id) {
    $("#spinner").show();
    $.ajax({
        url: 'SystemConfiguration/EditHoliday/' + id,
        type: 'GET'
    }).done(function (data) {
        $("#spinner").hide();
        var description = $("#edit_holiday_modal textarea[name='Description']").val(data.description);
        var startDate = $("#edit_holiday_modal input[name='StartDate']").val(data.startDate);
        var endDate = $("#edit_holiday_modal input[name='EndDate']").val(data.endDate);
        var isRecurring = $("#edit_holiday_modal input[name='IsRecurring']").prop('checked', data.isRecurring);
        $("#edit_holiday_modal input[name='Id']").val(data.id);

        $("#edit_holiday_modal button[name='update_holiday_btn']").unbind().click(function () { 
            updateHoliday(id);
        });

        $("#edit_holiday_modal").modal("show");
    }).fail(function() {
        $("#spinner").hide();
    });
}

function DeleteHoliday(id) {
    bootbox.confirm("Are you sure you want to delete this holiday?", function (result) {
        if (result) {
            $.ajax({
                url: 'SystemConfiguration/DeleteHoliday/' + id,
                type: 'POST',
                headers: {
                    'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
                }
            }).done(function (data) {
                if (data.status == "success") {
                    toastr.success(data.message);
                } else {
                    toastr.error(data.message);
                }
                datatable.ajax.reload();
            }).fail(function (response) {
                toastr.error(response.responseText);
                datatable.ajax.reload();
            });
        }
    });
}

function updateHoliday(id) {
    var authenticationToken = $("#edit_holiday_modal input[name='__RequestVerificationToken']").val();
    var name = $("#edit_holiday_modal input[name='Name']").val();
    var startDate = $("#edit_holiday_modal input[name='StartDate']").val();
    var endDate = $("#edit_holiday_modal input[name='EndDate']").val();
    var description = $("#edit_holiday_modal textarea[name='Description']").val();
    var isRecurring = $("#edit_holiday_modal input[name='IsRecurring']").is(':checked');

    var userInput = {
        __RequestVerificationToken: authenticationToken,
        Id: id,
        Name: name,
        StartDate: startDate,
        EndDate: endDate,
        Description: description,
        IsRecurring: isRecurring
    };

    $.ajax({
        url: $("#edit_holiday_modal form").attr("action"),
        type: 'POST',
        data: userInput,
        success: function (data) {
            var parsedData = $.parseHTML(data);
            var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true";

            if (isInvalid) {
                $("#edit_holiday_modal").html(data);
                $("#edit_holiday_modal button[name='update_holiday_btn']").unbind().click(function () { 
                    updateHoliday(id); 
                });

                var form = $("#edit_holiday_modal");
                $(form).removeData("validator")
                      .removeData("unobtrusiveValidation");
                $.validator.unobtrusive.parse(form);
            } else {
                toastr.success(data.message || "Holiday updated successfully");
                datatable.ajax.reload();
                $("#edit_holiday_modal").modal("hide");
            }
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.error(thrownError + "\n" + xhr.statusText + "\n" + xhr.responseText);
            toastr.error("Error updating holiday");
        }
    });
} 