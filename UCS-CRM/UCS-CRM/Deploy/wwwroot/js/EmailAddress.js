$(function () {

    //hook up a click event to the login button

    var createEmailAddressbutton = $("#create_emailAddress_modal button[name='create_emailAddress_btn']").unbind().click(OnCreateClick);

    function OnCreateClick() {

        //get the form url

        var form_url = $("#create_emailAddress_modal form").attr("action");

        //get the authentication token

        var authenticationToken = $("#create_emailAddress_modal input[name='__RequestVerificationToken']").val();

        //get the form fields

        //var EmailAddressId = $("#create_emailAddress_modal input[name ='TicketCategoryId']").val()
        var email = $("#create_emailAddress_modal input[name ='Email']").val()
        var owner = $("#create_emailAddress_modal input[name ='Owner']").val()


        var formData = new FormData();

        //append the file to the formdata 

        formData.append("Email", email);
        formData.append("Owner", owner);
        formData.append("__RequestVerificationToken", authenticationToken)
        //send the request

        $.ajax({
            url: form_url,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (data) {

                
                toastr.clear()
                //parse whatever comes back to html

                var parsedData = $.parseHTML(data)

                //check if there is an error in the data that is coming back from the user

                var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"



                if (isInvalid == true) {

                    //replace the form data with the data retrieved from the server
                    $("#create_emailAddress_modal").html(data)


                    //rewire the onclick event on the form

                    $("#edit_emailAddress_modal button[name='update_EmailAddress_btn']").unbind().click(function () { updateTicketCategory(id) })

                    var form = $("#create_emailAddress_modal")

                    $(form).removeData("validator")
                    $(form).removeData("unobtrusiveValidation")
                    $.validator.unobtrusive.parse(form)


                } else {
                    //show success message to the user
                    var dataTable = $('#my_table').DataTable();

                    //send success message
                    toastr.success("Ticket emailAddress added successfully")

                    $("#create_emailAddress_modal").modal("hide")

                    dataTable.ajax.reload();

                }



            },
            error: function (xhr, ajaxOtions, thrownError) {


                console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
            }

        });
    }


})


function EditForm(id) {

    //get the record from the database



    $.ajax({
        url: 'EmailAddresses/edit/' + id,
        type: 'GET'
    }).done(function (data) {

        //get the form url

        var form_url = $("#edit_emailAddress_modal form").attr("action");

        //get the authentication token

        //get the form fields

        var email = $("#edit_emailAddress_modal input[name ='Email']").val(data.email)
        var owner = $("#edit_emailAddress_modal input[name ='Owner']").val(data.owner)
        var id = $("#edit_emailAddress_modal input[name ='Id']").val(data.id)




        //hook up an event to the update role button

        $("#edit_emailAddress_modal button[name='update_emailAddress_btn']").unbind().click(function () { updateEmailAddress(id) })


        $("#edit_emailAddress_modal").modal("show");

    })
}

function Delete(id) {

    bootbox.confirm("Are you sure you want to delete this Email Address from the system?", function (result) {


        if (result) {
            $.ajax({
                url: 'EmailAddresses/delete/' + id,
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


function updateEmailAddress(id) {

    toastr.clear()

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#edit_emailAddress_modal input[name='__RequestVerificationToken']").val();
    var email = $("#edit_emailAddress_modal input[name ='Email']").val()
    var owner = $("#edit_emailAddress_modal input[name ='Owner']").val()
    var id = $("#edit_emailAddress_modal input[name='Id']").val()

    var form_url = $("#edit_emailAddress_modal form").attr("action");


    var formData = new FormData();

    //append the file to the formdata 

    var userInput = {
        __RequestVerificationToken: authenticationToken,
        Email: email,
        Owner: owner,
        Id: id
    }


    //send the request

    $.ajax({
        url: form_url,
        type: 'POST',
        data: userInput,
        success: function (data) {

            toastr.clear()
            //parse whatever comes back to html

            var parsedData = $.parseHTML(data)



            //check if there is an error in the data that is coming back from the user

            var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


            if (isInvalid == true) {

                //replace the form data with the data retrieved from the server
                $("#edit_emailAddress_modal").html(data)


                //rewire the onclick event on the form

                $("#edit_emailAddress_modal button[name='update_emailAddress_btn']").unbind().click(function () { updateEmailAddress(id) });

                var form = $("#edit_emailAddress_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)




            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();
                toastr.clear()

                if (data.message == undefined) {

                    toastr.success("nothing to update")

                } else {
                    toastr.success(data.message)
                }


                dataTable.ajax.reload();

                $("#edit_emailAddress_modal").modal("hide")

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


}