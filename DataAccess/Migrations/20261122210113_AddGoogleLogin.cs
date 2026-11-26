    using Microsoft.EntityFrameworkCore.Migrations;

    #nullable disable

    namespace DataAccess.Migrations
    {
        /// <inheritdoc />
        public partial class AddGoogleLogin : Migration
        {
            /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AlterColumn<string>(
                    name: "Password",
                    table: "Users",
                    type: "nvarchar(max)",
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)");

                migrationBuilder.AddColumn<string>(
                    name: "GoogleId",
                    table: "Users",
                    type: "nvarchar(max)",
                    nullable: true);
            }

            /// <inheritdoc />
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(
                    name: "GoogleId",
                    table: "Users");

                migrationBuilder.AlterColumn<string>(
                    name: "Password",
                    table: "Users",
                    type: "nvarchar(max)",
                    nullable: false,
                    defaultValue: "",
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)",
                    oldNullable: true);
            }
        }
    }
