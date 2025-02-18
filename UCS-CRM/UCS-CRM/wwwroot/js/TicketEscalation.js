﻿$(function () {

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
        var assignedToId = $("#create_ticket_modal select[name ='AssignedToId']").val()
        var memberId = $("#create_ticket_modal select[name ='MemberId']").val()
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


})



function EditForm(id, area = "") {

    //get the record from the database

    $.ajax({
        url: area + 'edit/' + id,
        type: 'GET'
    }).done(function (data) {

        //get the input field inside the edit role modal form
       
        $("#edit_ticket_modal input[name ='Title']").val(data.ticket.title)
        $("#edit_ticket_modal textarea[name ='Description']").val(data.ticket.description)
        $("#edit_ticket_modal select[name ='TicketCategoryId']").val(data.ticket.ticketCategoryId)
        $("#edit_ticket_modal select[name ='TicketPriorityId']").val(data.ticket.ticketPriorityId)
        $("#edit_ticket_modal select[name ='StateId']").val(data.ticket.stateId)
        $("#edit_ticket_modal select[name ='AssignedToId']").val(data.ticket.assignedToId)
        $("#edit_ticket_modal select[name ='MemberId']").val(data.ticket.memberId)
        $("#edit_ticket_modal input[name='Id']").val(data.ticket.id)

        //hook up an event to the update role button

        $("#edit_ticket_modal button[name='update_ticket_btn']").unbind().click(function () { updateTicket() })

        var validator = $("#edit_ticket_modal form").validate();

        //validator.resetForm();

        $("#edit_ticket_modal").modal("show");

    })
}


function EscalationForm(id, area="") {

    //get the input field inside the edit role modal form

    $.ajax({
        url: area + 'edit/' + id,
        type: 'GET'
    }).done(function (data) {

        $("#escalate_ticket_modal input[name ='TicketId']").val(data.ticket.id)

    //hook up an event to the update role button

    $("#escalate_ticket_modal button[name='escalate_ticket_btn']").unbind().click(function () { escalateTicket() })

    var validator = $("#escalate_ticket_modal form").validate();

    //validator.resetForm();

    $("#escalate_ticket_modal").modal("show");
    })
}



function escalateTicket() {
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

                toastr.success(" Ticket Escalated")

                $("#escalate_ticket_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


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


function MarkDone(id) {

    bootbox.confirm("Are you sure you want to mark this ticket done?", function (result) {


        if (result) {
            $.ajax({
                url: 'MarkDone/' + id,
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


function updateTicket() {
    toastr.clear()

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

function addComment(ticketId) {

    toastr.clear()

    let comment = $("#ticketComment").val()
    let formData = new FormData();


    formData.append("ticketId", ticketId)
    formData.append("comment", comment)



    $.ajax({
        url: "/manager/tickets/AddTicketComment",
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
                $("#escalate_ticket_modal").html(data)


                //rewire the onclick event on the form

                $("#escalate_ticket_modal button[name='escalate_ticket_btn']").unbind().click(function () { updateTicket() });

                var form = $("#escalate_ticket_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success(data.message)

                $("#escalate_ticket_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });
}



