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
                className: "text-left",
                render: function(data) {
                    return moment(data).format('MMM D, YYYY');
                }
            },
            {
                data: "endDate", 
                name: "endDate",
                className: "text-left",
                render: function(data) {
                    return moment(data).format('MMM D, YYYY');
                }
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
                    return `<a href='#' onclick=EditHoliday('${data}') class='btn btn-sm btn-outline-primary me-2'><i class='feather icon-edit'></i></a>
                           <a href='#' onclick=DeleteHoliday('${data}') class='btn btn-sm btn-outline-danger'><i class='fas fa-trash'></i></a>`;
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
        url: '/Manager/SystemConfiguration/EditHoliday/' + id,
        type: 'GET',
        success: function(data) {
            $("#spinner").hide();
            $("#edit_holiday_modal input[name='Id']").val(data.id);
            $("#edit_holiday_modal input[name='Name']").val(data.name);
            $("#edit_holiday_modal input[name='StartDate']").val(moment(data.startDate).format('YYYY-MM-DD'));
            $("#edit_holiday_modal input[name='EndDate']").val(moment(data.endDate).format('YYYY-MM-DD'));
            $("#edit_holiday_modal textarea[name='Description']").val(data.description);
            $("#edit_holiday_modal input[name='IsRecurring']").prop('checked', data.isRecurring);
            
            // Bind click event to update button
            $("#edit_holiday_modal button[name='update_holiday_btn']").off('click').on('click', function() {
                updateHoliday(data.id);
            });
            
            $("#edit_holiday_modal").modal('show');
        },
        error: function(xhr, status, error) {
            $("#spinner").hide();
            toastr.error("Error loading holiday details");
            console.error(error);
        }
    });
}

function DeleteHoliday(id) {
    bootbox.confirm({
        message: "Are you sure you want to delete this holiday?",
        buttons: {
            confirm: {
                label: 'Yes',
                className: 'btn-danger'
            },
            cancel: {
                label: 'No',
                className: 'btn-secondary'
            }
        },
        callback: function(result) {
            if (result) {
                $.ajax({
                    url: '/Manager/SystemConfiguration/DeleteHoliday/' + id,
                    type: 'POST',
                    headers: {
                        'RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
                    },
                    success: function(data) {
                        if (data.status === "success") {
                            toastr.success(data.message || "Holiday deleted successfully");
                            $('#holidaysTable').DataTable().ajax.reload();
                        } else {
                            toastr.error(data.message || "Error deleting holiday");
                        }
                    },
                    error: function(xhr, status, error) {
                        console.error(error);
                        toastr.error("Error deleting holiday");
                    }
                });
            }
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
                $('#holidaysTable').DataTable().ajax.reload();
                $("#edit_holiday_modal").modal("hide");
            }
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.error(thrownError + "\n" + xhr.statusText + "\n" + xhr.responseText);
            toastr.error("Error updating holiday");
        }
    });
} 