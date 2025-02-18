﻿$(function () {

    hideSpinner();
    //hook up a click event to the login button

    var createTicketButton = $("#create_ticket_modal button[name='create_ticket_btn']").unbind().click(OnCreateClick);


    function OnCreateClick() {

        //get the form url

        var form_url = $("#create_ticket_modal form").attr("action");

        var form = $("#create_ticket_modal form");

        //get the authentication token

        var authenticationToken = $("#create_ticket_modal input[name='__RequestVerificationToken']").val();

        //get the form fields

        var title = $("#create_ticket_modal input[name ='Title']").val()
        var description = $("#create_ticket_modal textarea[name ='Description']").val()
        var ticketCategoryId = $("#create_ticket_modal select[name ='TicketCategoryId']").val()
        var ticketPriorityId = $("#create_ticket_modal select[name ='TicketPriorityId']").val()
        var stateId = $("#create_ticket_modal select[name ='StateId']").val()
        var memberId = $("#create_ticket_modal input[name ='MemberId']").val()

        var files = $("#create_ticket_modal input[name = 'Attachments']")[0].files;

        //prepare data for request pushing
        var formData = new FormData(form[0]);

        //send the request

        $.ajax({
            url: form_url,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (data) {

                //parse whatever comes back to html

                var parsedData = $.parseHTML(data)



                //check if there is an error in the data that is coming back from the user

                var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


                if (isInvalid == true) {

                    //replace the form data with the data retrieved from the server
                    $("#create_ticket_modal").html(data)

                    //hook up the custom select

                    initSelect2({
                        url: "/officer/Tickets/GetAllMembersJson",
                        hiddenFieldId: "MemberId",
                        pageSize: 20,
                        initialSearchValue: "",
                    }, "create_ticket_modal");


                    //rewire the onclick event on the form

                    $("#create_ticket_modal button[name='create_ticket_btn']").unbind().click(OnCreateClick);

                    var form = $("#create_ticket_modal")

                    $(form).removeData("validator")
                    $(form).removeData("unobtrusiveValidation")
                    $.validator.unobtrusive.parse(form)


                } else {
                    //show success message to the user
                    var dataTable = $('#my_table').DataTable();

                    toastr.success("New ticket addded successfully")

                    $("#create_ticket_modal").modal("hide")

                    dataTable.ajax.reload();

                }


            },
            error: function (xhr, ajaxOtions, thrownError) {

                console.error(xhr.statusText)

                
            }

        });
    }



    //events


    $('#DepartmentId').on('change', function () {
        // Get the selected value
        var selectedValue = $(this).val();

       

        $.get('tickets/FetchReassignList', { selectedValue: selectedValue }, function (data) {
            var $select = $('#edit_ticket_modal select[name="AssignedToId"]');
           

            // Clear the options of the second dropdown list
            $select.find('option').remove();

            if (data && Array.isArray(data) && data.length > 0) {
                // Iterate through the received JSON data and create options for the second dropdown list
                $.each(data, function (index, item) {
                    var option = $('<option>').val(item.value).text(item.text);
                    $select.append(option);
                });
            } else {
                console.warn("No data received or data is not in the expected format");
            }

           
        }).fail(function (jqXHR, textStatus, errorThrown) {
            console.error("Error fetching data: " + textStatus, errorThrown);
            // Clear the options if there is an error
            var $select = $('#AssignedToId');
            $select.find('option').remove();

            // If using selectpicker or another plugin, refresh it
            if ($select.hasClass('selectpicker')) {
                $select.selectpicker('refresh');
            }
        });
    });


})

function PickTicket(id) {
    bootbox.confirm({
        message: "Are you sure you want to pick this ticket?",
        buttons: {
            confirm: {
                label: 'Yes',
                className: 'btn-success'
            },
            cancel: {
                label: 'No',
                className: 'btn-danger'
            }
        },
        callback: function (result) {
            if (result) {
                $.ajax({
                    url: 'tickets/PickTicket/' + id,
                    type: 'GET',
                    success: function(response) {
                        if (response.status === "success") {
                            toastr.options = {
                                "positionClass": "toast-top-right",
                                "backgroundColor": "#d4edda",
                                "textColor": "#155724"
                            };
                            toastr.success(response.message || "Ticket picked successfully");
                            datatable.ajax.reload();
                        } else {
                            toastr.options = {
                                "positionClass": "toast-top-right",
                                "backgroundColor": "#f8d7da",
                                "textColor": "#721c24"
                            };
                            toastr.error(response.message || "Failed to pick ticket");
                        }
                    },
                    error: function() {
                        toastr.options = {
                            "positionClass": "toast-top-right",
                            "backgroundColor": "#f8d7da",
                            "textColor": "#721c24"
                        };
                        toastr.error("An error occurred while picking the ticket");
                    }
                });
            }
        }
    });
}


function EditForm(id, area = "") {
    //get the record from the database
    $.ajax({
        url: area + 'tickets/edit/' + id,
        type: 'GET'
    }).done(function (data) {
        const ticketFields = {
            Title: 'title',
            Description: 'description', 
            TicketCategoryId: 'ticketCategoryId',
            TicketPriorityId: 'ticketPriorityId',
            AssignedToId: 'assignedToId',
            MemberId: 'memberId',
            StateId: 'stateId',
            DepartmentId: 'departmentId',
            Id: 'id',
        };

        const editTicketModal = $("#edit_ticket_modal");

        Object.entries(ticketFields).forEach(([fieldName, dataKey]) => {
            const field = editTicketModal.find(`[name='${fieldName}']`);

            if (fieldName === 'MemberId') {
                // If the field is MemberId, set the value and trigger initSelect2 with the retrieved member ID
                field.val(data[dataKey]);
                initSelect2({
                    url: "/officer/Tickets/GetAllMembersJson",
                    hiddenFieldId: "MemberId", 
                    pageSize: 20,
                    initialSearchValue: data.member.accountNumber
                }, "edit_ticket_modal");
            } else if (fieldName === 'DepartmentId') {
                // For department, set value and trigger loadAssignees
                field.val(data[dataKey]);
                if (data[dataKey]) {
                    loadAssigneesForDepartment(data[dataKey], data.assignedToId);
                }
            } else {
                // For other fields, set the value as usual
                field.val(data[dataKey]);
            }
        });

        let selectElements = document.querySelectorAll('.selectpicker')

        selectElements.forEach(function (element) {
            // Create a new 'change' event
            var event = new Event('change');
            // Dispatch it.
            element.dispatchEvent(event);
        });
      
        //hook up an event to the update role button
        $("#edit_ticket_modal button[name='update_ticket_btn']").unbind().click(function () { updateTicket() })

        var validator = $("#edit_ticket_modal form").validate();

        //validator.resetForm();

        $("#edit_ticket_modal").modal("show");
    })
}
//escalate ticket
function EscalationForm(id, area = "") {

    //get the input field inside the edit role modal form

    $("#escalate_ticket_modal input[name ='TicketId']").val(id)

    //hook up an event to the update role button

    $("#escalate_ticket_modal button[name='escalate_ticket_btn']").unbind().click(function () { escalateTicket() })

    var validator = $("#escalate_ticket_modal form").validate();

    //validator.resetForm();

    $("#escalate_ticket_modal").modal("show");

}
function Delete(id) {

    bootbox.confirm("Are you sure you want to delete this ticket from the system?", function (result) {


        if (result) {
            $.ajax({
                url: 'tickets/delete/' + id,
                type: 'POST',

            }).done(function (data) {

                if (data.status == "success") {

                    toastr.success(data.message)
                }
                else {
                    toastr.error(data.message)
                }




                datatable.ajax.reload();


            }).fail(function (response) {

                toastr.error(response.responseText)

                datatable.ajax.reload();
            });
        }


    });
}
function escalateTicket() {
    showSpinner();
    toastr.clear()

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#escalate_ticket_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#escalate_ticket_modal form").attr("action");

    var form = $("#escalate_ticket_modal form")


    let formData = new FormData(form[0]);


    //send the request



    $.ajax({
        url: form_url,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {

            hideSpinner();

            //parse whatever comes back to html

            var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (isInvalid == true) {

                //replace the form data with the data retrieved from the server
                $("#escalate_ticket_modal").html(data)


                //rewire the onclick event on the form

                $("#escalate_ticket_modal button[name='escalate_ticket_btn']").unbind().click(function () { escalateTicket() });

                var form = $("#escalate_ticket_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success("Ticket has been escalated successfully")

                $("#escalate_ticket_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {
            hideSpinner();
            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


}
function updateTicket() {
    toastr.clear()
    showSpinner();

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#edit_ticket_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#edit_ticket_modal form").attr("action");

    var form = $("#edit_ticket_modal form")


    let formData = new FormData(form[0]);


    //send the request



    $.ajax({
        url: form_url,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {

            hideSpinner();
            //parse whatever comes back to html

            var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (isInvalid == true) {

                //replace the form data with the data retrieved from the server
                $("#edit_ticket_modal").html(data)


                 //hook up custom select event

                 initSelect2({
                    url: "/officer/Tickets/GetAllMembersJson",
                    hiddenFieldId: "MemberId",
                    pageSize: 20,
                    initialSearchValue: "",
                }, "create_ticket_modal");
                
                //rewire the onclick event on the form

                $("#edit_ticket_modal button[name='update_ticket_btn']").unbind().click(function () { updateTicket() });

                var form = $("#edit_ticket_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success(data.message)

                $("#edit_ticket_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {
            hideSpinner();
            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


}
function addComment(ticketId) {

    toastr.clear()

    let comment = $("#ticketComment").val()
    let formData = new FormData();


    formData.append("ticketId", ticketId)
    formData.append("comment", comment)



    $.ajax({
        url: "/officer/tickets/AddTicketComment",
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {


            //parse whatever comes back to html

            var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (isInvalid == true) {

                //replace the form data with the data retrieved from the server
                $("#edit_ticket_modal").html(data)


                //rewire the onclick event on the form

                $("#edit_ticket_modal button[name='update_ticket_btn']").unbind().click(function () { updateTicket() });

                var form = $("#edit_ticket_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success(data.message)

                $("#edit_ticket_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });
}
function closeTicketfn(id) {

    toastr.clear()

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#close_ticket_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#close_ticket_modal form").attr("action");

    var form = $("#close_ticket_modal form")


    let formData = new FormData(form[0]);


    //send the request



    $.ajax({
        url: form_url,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {


            //parse whatever comes back to html

            var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (isInvalid == true) {

                //replace the form data with the data retrieved from the server
                $("#close_ticket_modal").html(data)


                //rewire the onclick event on the form

                $("#close_ticket_modal button[name='closeTicketfn']").unbind().click(function () { closeTicket(id) });

                var form = $("#close_ticket_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success("Ticket has been closed successfully")

                $("#close_ticket_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });
}

// Function to start the spinner
function showSpinner() {
    document.getElementById('spinner').style.display = 'block';
}

// Function to stop the spinner
function hideSpinner() {
    document.getElementById('spinner').style.display = 'none';
}

// Add this function at global scope
function loadAssigneesForDepartment(departmentId, selectedAssigneeId = null) {
    const assigneeSelect = $('#edit_ticket_modal select[name="AssignedToId"]');
    assigneeSelect.empty().append('<option value="">---- Select Assignee -------</option>');
    
    if (departmentId) {
        $.get('tickets/FetchReassignList', { selectedValue: departmentId })
            .done(function(data) {
                if (data && Array.isArray(data)) {
                    data.forEach(item => {
                        assigneeSelect.append($('<option>', {
                            value: item.value,
                            text: item.text
                        }));
                    });
                  
                   
                }
            })
            .fail(function(jqXHR, textStatus, errorThrown) {
                console.error('Failed to fetch assignees:', textStatus, errorThrown);
            });
    }
}


