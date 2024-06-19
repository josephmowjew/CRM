function initSelect2(options, containerId) {
    var containerElement = document.getElementById(containerId);

    if (!containerElement) {
        console.error("Container element with ID '" + containerId + "' not found.");
        return;
    }

    var selectSearch = containerElement.querySelector("#select-search");
    var selectOptions = containerElement.querySelector(".select2-options");
    var selectDropdown = containerElement.querySelector(".select2-dropdown");
    var selectSelection = containerElement.querySelector(".select2-selection");
    var uniqueOptions = new Set();
    var currentPage = 1;
    var hiddenFieldId = options.hiddenFieldId || "MemberId";

    if (!selectSearch) {
        console.error("Search input element not found within container element.");
        return;
    }

    if (options.initialSearchValue !== undefined && options.initialSearchValue !== null) {
        selectSearch.value = options.initialSearchValue;
    }

    function fetchData(debounceFetch = false) {
        if (debounceFetch) {
            clearTimeout(debounceFetch);
            debounceFetch = setTimeout(() => performFetch(), 300); // Adjust delay as needed
        } else {
            performFetch();
        }
    }

    function performFetch() {
        $.ajax({
            url: options.url || "/GetAllMembersJson",
            type: "GET",
            data: {
                page: currentPage,
                pageSize: options.pageSize || 20,
                searchValue: selectSearch.value.toLowerCase()
            },
            success: function (response) {
                response.forEach(function (option) {
                    var optionValue = option.name;
                    var optionId = option.id;

                    if (!uniqueOptions.has(optionValue)) {
                        var newOption = document.createElement('div');
                        newOption.classList.add('select2-dropdown-option');
                        newOption.setAttribute('data-value', optionId);
                        newOption.textContent = optionValue;

                        var newOptionLi = document.createElement('li');
                        newOptionLi.appendChild(newOption);

                        if (options.initialSearchValue && options.initialSearchValue.trim() !== '') {
                            var accountNumber = extractAccountNumber(optionValue);
                            if (accountNumber === options.initialSearchValue) {
                                newOptionLi.classList.add('selected');
                                document.getElementById(hiddenFieldId).value = optionId;
                                selectSelection.textContent = optionValue;
                            }
                        }

                        selectOptions.appendChild(newOptionLi);
                        uniqueOptions.add(optionValue);

                        newOption.addEventListener("click", function () {
                            var selectedValue = this.textContent;
                            var selectId = this.getAttribute("data-value");
                            document.getElementById(hiddenFieldId).value = selectId;
                            selectSelection.textContent = selectedValue;
                            selectDropdown.style.display = "none";
                        });
                    }
                });

                filterOptions();
                currentPage++;
            },
            error: function (xhr, status, error) {
                console.error("Error fetching data: ", error);
            }
        });
    }

    function extractAccountNumber(optionValue) {
        var matches = optionValue.match(/\((.*?)\)/);
        return matches && matches.length > 1 ? matches[1] : null;
    }

    function filterOptions() {
        var searchValue = selectSearch.value.toLowerCase();
        var selectOptionsLi = selectOptions.querySelectorAll("li");
        selectOptionsLi.forEach(function (option) {
            var text = option.textContent.toLowerCase();
            option.style.display = text.includes(searchValue) ? "list-item" : "none";
        });
    }

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                fetchData();
            }
        });
    }, { threshold: 0.5 });

    fetchData();

    selectSearch.addEventListener("input", function () {
        fetchData(true);
    });

    var isDropdownOpen = false;
    selectSelection.addEventListener("click", function () {
        selectDropdown.style.display = isDropdownOpen ? "none" : "block";
        isDropdownOpen = !isDropdownOpen;
    });
}
