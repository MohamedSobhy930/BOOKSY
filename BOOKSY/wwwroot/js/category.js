var dataTable
$(function () {
    loadDataTable();
});
function loadDataTable() {
    dataTable = $('#tableData').DataTable({
        "ajax": { url: '/admin/category/getall' },
        "columns": [
            { data: 'name', "width": "30%" },
            { data: 'displayOrder', "width": "30%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                        <a href="category/edit?id=${data}" class="btn btn-primary btn-sm me-1" >
                            <i class="bi bi-pencil-square"></i> Update
						</a>
                        <a onClick=Delete('category/delete/${data}') class="btn btn-danger btn-sm">
                            <i class="bi bi-x-square-fill"></i> Delete
                        </a>
							</div>` 
                },
                "width":"25%"
            }
        ]
    });
}

function Delete (url)
{
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                }
            })
        }
    })
}