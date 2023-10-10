$(function () {

    hideSpinner();

    //hook up a click event to the login button

    var createUserButton = $("#set_email_modal button[name='create_account_btn']").unbind().click(CreateUserAccount);
    
    function CreateUserAccount() {

        showSpinner();

        let id = $("#set_email_modal input[name = 'Id']").val()
        let email = $("#set_email_modal input[name = 'Email']").val()

        var form_url = $("#set_email_modal form").attr("action");

        //get the record from the database

        var userInput = {
            Email: email,
            Id: id,
        }

        $.ajax({
            url: form_url,
            type: 'POST',
            data: userInput,
            success: function (data) {

                hideSpinner();

                //parse whatever comes back to html

                var parsedData = $.parseHTML(data)



                //check if there is an error in the data that is coming back from the user

                var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


                if (isInvalid == true) {

                    //replace the form data with the data retrieved from the server
                    $("#set_email_modal").html(data)


                    //rewire the onclick event on the form

                    $("#set_email_modal button[name='create_account_btn']").unbind().click(OnCreateClick);

                    var form = $("#set_email_modal")

                    $(form).removeData("validator")
                    $(form).removeData("unobtrusiveValidation")
                    $.validator.unobtrusive.parse(form)


                } else {
                    //show success message to the user
                    var dataTable = $('#my_table').DataTable();

                    toastr.success("User account created successfully")

                    $("#set_email_modal").modal("hide")

                    dataTable.ajax.reload();

                }


            },
            error: function (xhr, ajaxOtions, thrownError) {

                console.error(xhr.responseText)
                hideSpinner();
            }

        });


    }


})


function set_email_modal(id) {

    //set the id of the form

    $("#set_email_modal input[name = 'Id']").val(id)
    $("#set_email_modal").modal("show");






}

function removeUserAccount(id) {


    bootbox.confirm("Are you sure you want to remove this member's user account from the system?", function (result) {


        if (result) {
            showSpinner();
            $.ajax({
                url: 'members/DeleteUserAccount/' + id,
                type: 'POST',

            }).done(function (data) {

                hideSpinner();
           
                if (data.status == "success") {

                    toastr.success(data.message)
                }
                else {
                    toastr.error(data.message)
                }




                datatable.ajax.reload();


            }).fail(function (response) {

                toastr.error(response.responseText)

                hideSpinner();

                datatable.ajax.reload();
            });
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

