using ITRepairService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITRepairService.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260902040002_MakeApproverDepartmentNullable")]
    public partial class MakeApproverDepartmentNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ApproverDepartment ("ฝ่ายที่อนุมัติ") must stay NULL until an approver
            // actually approves the ticket (first save / Create keeps it null).
            migrationBuilder.AlterColumn<string>(
                name: "ApproverDepartment",
                table: "RepairTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApproverDepartment",
                table: "RepairTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
