var dataTable
$(function () {
    var url = window.location.search;
    if(url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else if(url.includes("pending")) {
        loadDataTable("pending");
    }
    else if(url.includes("completed")) {
        loadDataTable("completed");
    }
    else if(url.includes("approved")) {
        loadDataTable("approved");
    }
    else {
        loadDataTable();
    }
});
function loadDataTable(status) {
    dataTable = $('#tableData').DataTable({
        "ajax": { url: '/admin/order/getall?status=' + status},
        "columns": [
            { data: 'id', "width": "5%" },
            { data: 'name', "width": "15%" },
            { data: 'phoneNumber', "width": "20%" },
            { data: 'appUser.email', "width": "15%" },
            { data: 'orderStatus', "width": "15%" },
            { data: 'orderTotal', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                        <a href="order/details?orderId=${data}" class="btn btn-primary btn-sm me-1" >
                            <i class="bi bi-pencil-square"></i> 
						</a>
							</div>` 
                },
                "width":"20%"
            }
        ]
    });
}
