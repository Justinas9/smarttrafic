using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoordinatesX",
                table: "VideoRequests");

            migrationBuilder.DropColumn(
                name: "CoordinatesY",
                table: "VideoRequests");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "VideoRequests");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "VideoRequests");

            migrationBuilder.DropColumn(
                name: "VideoFile",
                table: "VideoRequests");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "VideoRequests",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "VideoRequests",
                newName: "ID");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "VideoRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntersectionID",
                table: "VideoRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoID",
                table: "VideoRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntersectionID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Intersections",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoordinatesX = table.Column<float>(type: "real", nullable: false),
                    CoordinatesY = table.Column<float>(type: "real", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intersections", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ObjectDetections",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectID = table.Column<int>(type: "int", nullable: false),
                    Probability = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DetectionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObjectCount = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectDetections", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Objects",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CO = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NOX = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PM = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VOC = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objects", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeInBytes = table.Column<int>(type: "int", nullable: false),
                    DurationInSeconds = table.Column<int>(type: "int", nullable: false),
                    CameraID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "Intersections");

            migrationBuilder.DropTable(
                name: "ObjectDetections");

            migrationBuilder.DropTable(
                name: "Objects");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "VideoRequests");

            migrationBuilder.DropColumn(
                name: "IntersectionID",
                table: "VideoRequests");

            migrationBuilder.DropColumn(
                name: "VideoID",
                table: "VideoRequests");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "VideoRequests",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "VideoRequests",
                newName: "Id");

            migrationBuilder.AddColumn<float>(
                name: "CoordinatesX",
                table: "VideoRequests",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "CoordinatesY",
                table: "VideoRequests",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "VideoRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "VideoRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VideoFile",
                table: "VideoRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
