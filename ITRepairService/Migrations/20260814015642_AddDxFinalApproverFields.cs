using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITRepairService.Migrations
{
    /// <inheritdoc />
    public partial class AddDxFinalApproverFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DxFinalApproverName",
                table: "RepairTickets",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DxFinalApproverUserId",
                table: "RepairTickets",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DxFinalApproverName",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "DxFinalApproverUserId",
                table: "RepairTickets");
        }
    }
}
