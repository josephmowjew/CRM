$(function() {

    //hook up a click event to the login button

    var loginButton = $("#create_role_modal button[name='create_role_btn']").unbind().click(OnCreateClick);

    function OnCreateClick() {

        //get the form url

        var form_url = $("#create_role_modal form").attr("action");

        //get the authentication token

        var authenticationToken = $("#create_role_modal input[name='__RequestVerificationToken']").val();

        //get the form fields

        var roleName = $("#create_role_modal input[name ='Name']").val()
        var rating = $("#create_role_modal input[name ='Rating']").val()


        //prepare data for request pushing

        var userInput = {
            __RequestVerificationToken: authenticationToken,
            Name: roleName,
            Rating: rating
        }

       //send the request

        $.ajax({
            url: form_url,
            type: 'POST',
            data: userInput,
            success: function (data) {
                //parse whatever comes back to html

                var parsedData = $.parseHTML(data)



                //check if there is an error in the data that is coming back from the user

                var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


                if (isInvalid == true) {

                    //replace the form data with the data retrieved from the server
                    $("#create_role_modal").html(data)


                    //rewire the onclick event on the form

                    $("#create_role_modal button[name='create_role_form']").unbind().click(OnCreateClick);

                    var form = $("#create_role_modal")

                    $(form).removeData("validator")
                    $(form).removeData("unobtrusiveValidation")
                    $.validator.unobtrusive.parse(form)


                } else {
                    //show success message to the user
                    var dataTable = $('#my_table').DataTable();

                    toastr.success("New role created successfully")

                    dataTable.ajax.reload();

                    $("#create_role_modal").modal("hide")
                    $("#create_role_modal form")[0].reset();

                }



            },
            error: function (xhr, ajaxOtions, thrownError) {

                console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
            }

        });
    }


})

function Delete(id) {

    bootbox.confirm("Are you sure you want to delete this role?", function (result) {


        if (result) {
            $.ajax({
                url: 'roles/delete/' + id,
                type: 'POST',
            }).done(function (data) {


                toastr.success(data.response)

                datatable.ajax.reload();


            }).fail(function (response) {

                toastr.error(response.responseText)

                datatable.ajax.reload();
            });
        }


    });
}

function EditForm(id) {

    //get the record from the database

    $.ajax({
        url: 'roles/edit/' + id,
        type: 'GET'
    }).done(function (data) {

        //get the input field inside the edit role modal form
        $("#edit_role_modal input[name='Name']").val(data.name)

        $("#edit_role_modal input[name='Id']").val(data.id)
        $("#edit_role_modal input[name='Rating']").val(data.rating)

        //hook up an event to the update role button

        $("#edit_role_modal button[name='update_role_btn']").unbind().click(function () { upDateRole(id) })

        $("#edit_role_modal").modal("show");

    })
}


function upDateRole(id) {

    //get the authorisation token
    upDateRole
    var authenticationToken = $("#edit_role_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#edit_role_modal form").attr("action");


    var roleName = $("#edit_role_modal input[name ='Name']").val()
    var rating = $("#edit_role_modal input[name ='Rating']").val()
    var id = $("#edit_role_modal input[name ='Id']").val();


    //prepare data for request pushing

    var userInput = {
        __RequestVerificationToken: authenticationToken,
        Name: roleName,
        Rating: rating,
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
                $("#edit_role_modal").html(data)


                //rewire the onclick event on the form

                $("#edit_role_modal button[name='update_role_btn']").unbind().click(function () { upDateRole(id) });

                var form = $("#edit_role_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success("Role updated successfully")

                dataTable.ajax.reload();

                $("#edit_role_modal").modal("hide")

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


}