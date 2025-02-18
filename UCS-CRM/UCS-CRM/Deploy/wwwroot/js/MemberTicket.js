$(function () {
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
       // var ticketPriorityId = $("#create_ticket_modal select[name ='TicketPriorityId']").val()
        var files = $("#create_ticket_modal input[name = 'Attachments']")[0].files;

        //prepare data for request pushing
        var formData = new FormData(form[0]);

        //formData.append("Title", title);
        //formData.append("Description", description);
        //formData.append("TicketCategoryId", ticketCategoryId);
        //formData.append("TicketPriorityId", ticketPriorityId);
        //formData.append("__RequestVerificationToken", authenticationToken);

        //formData.append("TicketAttachments", files);


        //var userInput = {
        //    __RequestVerificationToken: authenticationToken,
        //    Title: title,
        //    Description: description,
        //    TicketCategoryId: ticketCategoryId,
        //    TicketPriorityId: ticketPriorityId,
        //    TicketAttachments: files
        //}


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
        url: area + '/member/tickets/edit/' + id,
        type: 'GET'
    }).done(function (data) {

        //get the input field inside the edit role modal form
        //var date = new Date(data.dateOfBirth);


        // var currentDate = new Date(data.dateOfBirth);

        // var day = ("0" + currentDate.getDate()).slice(-2);
        // var month = ("0" + (currentDate.getMonth() + 1)).slice(-2);

        // var date = currentDate.getFullYear() + "-" + (month) + "-" + (day);

        const prefix = "#edit_ticket_modal ";
        const fields = {
            Title: "title",
            Description: "description",
            TicketCategoryId: "ticketCategoryId",
            TicketPriorityId: "ticketPriorityId",
            Id: "id"
        };

        Object.entries(fields).forEach(([name, value]) => {
            $(`${prefix}[name='${name}']`).val(data[value]);
        });


        //hook up an event to the update role button

        $("#edit_ticket_modal button[name='update_ticket_btn']").unbind().click(function () { updateTicket() })

        var validator = $("#edit_ticket_modal form").validate();

        //validator.resetForm();

        $("#edit_ticket_modal").modal("show");

    })
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

function updateTicket() {
    toastr.clear()

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#edit_ticket_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#edit_ticket_modal form").attr("action");

    var form = $("#edit_ticket_modal form")


    //get the form fields

    //var title = $("#edit_ticket_modal input[name ='Title']").val()
    //var description = $("#edit_ticket_modal textarea[name ='Description']").val()
    //var ticketCategoryId = $("#edit_ticket_modal select[name ='TicketCategoryId']").val()
    //var ticketPriorityId = $("#edit_ticket_modal input[name ='TicketPriorityId']").val()
    //var id = $("#edit_ticket_modal input[name='Id']").val()


    let formData = new FormData(form[0]);


    //prepare data for request pushing

    //var userInput = {
    //    __RequestVerificationToken: authenticationToken,
    //    Title: title,
    //    Description: description,
    //    TicketCategoryId: ticketCategoryId,
    //    TicketPriorityId: ticketPriorityId,
    //    Id: id
    //}


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
        url: "/member/tickets/AddTicketComment",
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

function DeleteComment(id) {

    bootbox.confirm("Are you sure you want to delete this comment from the system?", function (result) {


        if (result) {
            $.ajax({
                url: '/member/tickets/deleteComment/' + id,
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

function closeTicketfn(id) {

    showSpinner();

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

            hideSpinner();
            //parse whatever comes back to html

            //var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            //var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (data.status == "error") {

                //replace the form data with the data retrieved from the server
                //$("#close_ticket_modal").html(data)


                //rewire the onclick event on the form

                $("#close_ticket_modal button[name='closeTicketfn']").unbind().click(function () { closeTicket(id) });

                var form = $("#close_ticket_modal")

                toastr.error(data.message)

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

                toggleTicketButtons(true);
            }



        },
        error: function (xhr, ajaxOtions, thrownError) {
            hideSpinner();
            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });
}
function reopenTicketfn(id) {

    showSpinner();
    toastr.clear()

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#reopen_ticket_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#reopen_ticket_modal form").attr("action");

    var form = $("#reopen_ticket_modal form")


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

            //var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            //var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (data.status == "error") {

                //replace the form data with the data retrieved from the server
                //$("#close_ticket_modal").html(data)


                //rewire the onclick event on the form

                $("#reopen_ticket_modal button[name='reopenTicketfn']").unbind().click(function () { closeTicket(id) });

                var form = $("#reopen_ticket_modal")

                toastr.error(data.message)

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success("Ticket has been reopened successfully")

                $("#reopen_ticket_modal").modal("hide")

                dataTable.ajax.reload();

                toggleTicketButtons(false);
            }



        },
        error: function (xhr, ajaxOtions, thrownError) {
            hideSpinner();
            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });
}

function toggleTicketButtons(isClosed) {
    const closeBtn = $("#closeTicketBtn");
    const reopenBtn = $("#reopenTicketBtn");
    
    if (isClosed) {
        closeBtn.hide();
        reopenBtn.show();
    } else {
        closeBtn.show();
        reopenBtn.hide();
    }
}

// Function to start the spinner
function showSpinner() {
    document.getElementById('spinner').style.display = 'block';
}

// Function to stop the spinner
function hideSpinner() {
    document.getElementById('spinner').style.display = 'none';
}
