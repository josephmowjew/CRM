$(function () {

    //hook up a click event to the login button

    var createUserButton = $("#create_user_modal button[name='create_user_btn']").unbind().click(OnCreateClick);

    var edit_passwordButton = $("#edit_password_btn").unbind().click(EditPasswordModalPopUp);

    var update_passwordButton = $("#update_password_btn").unbind().click(UpdatePassword);

    function OnCreateClick() {

        //get the form url

        var form_url = $("#create_user_modal form").attr("action");

        //get the authentication token

        var authenticationToken = $("#create_user_modal input[name='__RequestVerificationToken']").val();

        //get the form fields

        var firstname = $("#create_user_modal input[name ='FirstName']").val()
        var lastName = $("#create_user_modal input[name ='LastName']").val()
        var gender = $("#create_user_modal select[name ='Gender']").val()
        var email = $("#create_user_modal input[name ='Email']").val()
        var contact = $("#create_user_modal input[name ='PhoneNumber']").val()
        var departmentId = $("#create_user_modal select[name ='DepartmentId']").val()
        var role = $("#create_user_modal select[name ='RoleName']").val()
        //prepare data for request pushing


        var userInput = {
            __RequestVerificationToken: authenticationToken,
            FirstName: firstname,
            LastName: lastName,
            Gender: gender,
            Email: email,
            PhoneNumber: contact,
            RoleName: role,
            DepartmentId: departmentId
        }


        //send the request

        $.ajax({
            url:  form_url,
            type: 'POST',
            data: userInput,
            success: function (data) {

                //parse whatever comes back to html

                var parsedData = $.parseHTML(data)



                //check if there is an error in the data that is coming back from the user

                var isInvalid = $(parsedData).find("input[name='DataInvalid']").val() == "true"


                if (isInvalid == true) {

                    //replace the form data with the data retrieved from the server
                    $("#create_user_modal").html(data)


                    //rewire the onclick event on the form

                    $("#create_user_modal button[name='create_user_btn']").unbind().click(OnCreateClick);

                    var form = $("#create_user_modal")

                    $(form).removeData("validator")
                    $(form).removeData("unobtrusiveValidation")
                    $.validator.unobtrusive.parse(form)


                } else {
                    //show success message to the user
                    var dataTable = $('#my_table').DataTable();

                    toastr.success("New User addded successfully")

                    $("#create_user_modal").modal("hide")

                    dataTable.ajax.reload();

                }


            },
            error: function (xhr, ajaxOtions, thrownError) {

                console.error(xhr.responseText)
            }

        });
    }

    //events section

    $("#create_user_modal select[name ='DepartmentId']").on('change', function () {
        // Get the selected value
        var selectedValue = $(this).val();

        // Send a GET request to the endpoint with the selected value
        $.get('users/FetchRolesOnDepartment', { selectedValue: selectedValue }, function (data) {
            // Clear the options of the second dropdown list
            $("#create_user_modal select[name ='RoleName']").empty();

            // Iterate through the received JSON data and create options for the second dropdown list
            $.each(data, function (index, item) {
                var option = $('<option>').val(item.value).text(item.text);
                $("#create_user_modal select[name ='RoleName']").append(option);
            });
        });
    });


    $("#edit_user_modal select[name ='DepartmentId']").on('change', function () {
        // Get the selected value
        var selectedValue = $(this).val();

        // Send a GET request to the endpoint with the selected value
        $.get('users/FetchRolesOnDepartment', { selectedValue: selectedValue }, function (data) {
            // Clear the options of the second dropdown list
            $("#edit_user_modal select[name ='RoleName']").empty();

            // Iterate through the received JSON data and create options for the second dropdown list
            $.each(data, function (index, item) {
                var option = $('<option>').val(item.value).text(item.text);
                $("#edit_user_modal select[name ='RoleName']").append(option);
            });
        });
    });


})


function UpdatePassword() {

    var userId = $("#edit_user_modal input[name='Id']").val()
    //var id = $("#edit_password_modal input[name='Id']").val();
    var newPassword = $("#edit_password_modal input[name='NewPassword']").val();
    var authenticationToken = $("#edit_password_modal input[name='__RequestVerificationToken']").val();
    var form_url = $("#edit_password_modal form").attr("action");

    var userInput = {
        __RequestVerificationToken: authenticationToken,
        Id: userId,
        NewPassword: newPassword
    }

    console.log(userInput);

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
                $("#edit_password_modal").html(data)


                //rewire the onclick event on the form

                $("#edit_password_modal button[name='update_password_btn']").unbind().click(function () { UpdatePassword() });

                var form = $("#edit_password_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                //var dataTable = $('#my_table').DataTable();

                toastr.success(data.message)

                $("#edit_password_modal").modal("hide")

                //dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


}
function EditPasswordModalPopUp() {

    $("#edit_password_modal input[name='Id']").val($("#edit_user_modal input[name='Id']").val())

    $("#edit_password_modal").modal("show");
}
function EditForm(id, area = "") {

    //get the record from the database

    $.ajax({
        url: area + 'users/edit/' + id,
        type: 'GET'
    }).done(function (data) {

        //get the input field inside the edit role modal form
        //var date = new Date(data.dateOfBirth);


        var currentDate = new Date(data.dateOfBirth);

        var day = ("0" + currentDate.getDate()).slice(-2);
        var month = ("0" + (currentDate.getMonth() + 1)).slice(-2);

        var date = currentDate.getFullYear() + "-" + (month) + "-" + (day);

        console.log(data)

        $("#edit_user_modal input[name ='FirstName']").val(data.firstName)
        $("#edit_user_modal input[name ='LastName']").val(data.lastName)
        $("#edit_user_modal select[name ='Gender']").val(data.gender)
        $("#edit_user_modal input[name ='Email']").val(data.email)
        $("#edit_user_modal input[name ='PhoneNumber']").val(data.phoneNumber)
        $("#edit_user_modal input[name ='DateOfBirth']").val(date)
        $("#edit_user_modal select[name ='RoleName']").val(data.roleName)
        $("#edit_user_modal select[name ='DepartmentId']").val(data.departmentId)
        $("#edit_user_modal input[name='Id']").val(data.id)

        //hook up an event to the update role button

        $("#edit_user_modal button[name='update_user_btn']").unbind().click(function () { upDateUser() })

        var validator = $("#edit_user_modal form").validate();

        validator.resetForm();

        $("#edit_user_modal").modal("show");

    })
}

function Delete(id) {

    bootbox.confirm("Are you sure you want to delete this user from the system?", function (result) {


        if (result) {
            $.ajax({
                url: 'users/delete/' + id,
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

function Reactivate(id) {

    bootbox.confirm("Are you sure you want to reactivate this user account?", function (result) {


        if (result) {
            $.ajax({
                url: 'reactivate/' + id,
                type: 'GET',

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


function upDateUser() {
    toastr.clear()

    //get the authorisation token
    //upDateRole
    var authenticationToken = $("#edit_user_modal input[name='__RequestVerificationToken']").val();

    var form_url = $("#edit_user_modal form").attr("action");


    //get the form fields

    var firstname = $("#edit_user_modal input[name ='FirstName']").val()
    var lastName = $("#edit_user_modal input[name ='LastName']").val()
    var gender = $("#edit_user_modal select[name ='Gender']").val()
    var email = $("#edit_user_modal input[name ='Email']").val()
    var contact = $("#edit_user_modal input[name ='PhoneNumber']").val()
    var dateOfBirth = $("#edit_user_modal input[name ='DateOfBirth']").val()
    var role = $("#edit_user_modal select[name ='RoleName']").val()
    var departmentId = $("#edit_user_modal select[name ='DepartmentId']").val()
    var id = $("#edit_user_modal input[name='Id']").val()


    //prepare data for request pushing

    var userInput = {
        __RequestVerificationToken: authenticationToken,
        FirstName: firstname,
        LastName: lastName,
        Gender: gender,
        Email: email,
        DateOfBirth: dateOfBirth,
        PhoneNumber: contact,
        RoleName: role,
        DepartmentId: departmentId,
        Id: id
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
                $("#edit_user_modal").html(data)


                //rewire the onclick event on the form

                $("#edit_user_modal button[name='update_user_btn']").unbind().click(function () { upDateUser() });

                var form = $("#edit_user_modal")

                $(form).removeData("validator")
                $(form).removeData("unobtrusiveValidation")
                $.validator.unobtrusive.parse(form)


            }
            else {


                //show success message to the user
                var dataTable = $('#my_table').DataTable();

                toastr.success(data.message)

                $("#edit_user_modal").modal("hide")

                dataTable.ajax.reload();

            }



        },
        error: function (xhr, ajaxOtions, thrownError) {

            console.error(thrownError + "r\n" + xhr.statusText + "r\n" + xhr.responseText)
        }

    });


}



