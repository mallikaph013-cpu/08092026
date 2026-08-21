using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITRepairService.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondThirdApproverDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecondApproverDepartment",
                table: "RepairTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThirdApproverDepartment",
                table: "RepairTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondApproverDepartment",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "ThirdApproverDepartment",
                table: "RepairTickets");
        }
    }
}
