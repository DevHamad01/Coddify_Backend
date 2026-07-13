using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "NVARCHAR(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "NVARCHAR(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "NVARCHAR(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "NVARCHAR(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "NVARCHAR(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Bio = table.Column<string>(type: "NVARCHAR(350)", maxLength: 350, nullable: false),
                    Location = table.Column<string>(type: "NVARCHAR(200)", maxLength: 200, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "NVARCHAR(1000)", maxLength: 1000, nullable: false),
                    CoverImageUrl = table.Column<string>(type: "NVARCHAR(1000)", maxLength: 1000, nullable: false),
                    BlockedReason = table.Column<string>(type: "NVARCHAR(1500)", maxLength: 1500, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");
        }
    }
}
