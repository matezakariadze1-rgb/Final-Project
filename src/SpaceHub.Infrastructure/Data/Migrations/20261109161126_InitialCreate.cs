using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpaceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizerProfileRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizerProfileRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizerProfileRecords_UserRecords_UserId",
                        column: x => x.UserId,
                        principalTable: "UserRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceRecords", x => x.Id);
                    table.CheckConstraint("CK_Space_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_Space_Rate", "[HourlyRate] >= 0");
                    table.ForeignKey(
                        name: "FK_SpaceRecords_UserRecords_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "UserRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleRecords",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleRecords", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoleRecords_RoleRecords_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleRecords_UserRecords_UserId",
                        column: x => x.UserId,
                        principalTable: "UserRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageRecords", x => x.Id);
                    table.CheckConstraint("CK_Package_Price", "[Price] >= 0");
                    table.ForeignKey(
                        name: "FK_PackageRecords_SpaceRecords_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SpaceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PricingType = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceServiceRecords", x => x.Id);
                    table.CheckConstraint("CK_SpaceService_Price", "[UnitPrice] >= 0");
                    table.CheckConstraint("CK_SpaceService_Pricing", "[PricingType] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_SpaceServiceRecords_SpaceRecords_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SpaceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceWorkingHoursRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceWorkingHoursRecords", x => x.Id);
                    table.CheckConstraint("CK_SpaceWorkingHours_Day", "[DayOfWeek] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_SpaceWorkingHours_Hours", "[ClosesAt] > [OpensAt]");
                    table.ForeignKey(
                        name: "FK_SpaceWorkingHoursRecords_SpaceRecords_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SpaceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SpaceId = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendeeCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HourlyRateSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SpaceSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: true),
                    PackageNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PackagePriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServicesSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRecords", x => x.Id);
                    table.CheckConstraint("CK_Booking_Amounts", "[HourlyRateSnapshot] >= 0 AND [SpaceSubtotal] >= 0 AND [PackagePriceSnapshot] >= 0 AND [ServicesSubtotal] >= 0");
                    table.CheckConstraint("CK_Booking_Attendees", "[AttendeeCount] > 0");
                    table.CheckConstraint("CK_Booking_Currency", "[Currency] = 'GEL'");
                    table.CheckConstraint("CK_Booking_Period", "[EndsAt] > [StartsAt]");
                    table.CheckConstraint("CK_Booking_Status", "[Status] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Booking_Total", "[TotalAmount] = [SpaceSubtotal] + [PackagePriceSnapshot] + [ServicesSubtotal]");
                    table.ForeignKey(
                        name: "FK_BookingRecords_PackageRecords_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PackageRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRecords_SpaceRecords_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SpaceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRecords_UserRecords_UserId",
                        column: x => x.UserId,
                        principalTable: "UserRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageItemRecords",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    SpaceServiceId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageItemRecords", x => new { x.PackageId, x.SpaceServiceId });
                    table.CheckConstraint("CK_PackageItem_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_PackageItemRecords_PackageRecords_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PackageRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageItemRecords_SpaceServiceRecords_SpaceServiceId",
                        column: x => x.SpaceServiceId,
                        principalTable: "SpaceServiceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    SpaceServiceId = table.Column<int>(type: "int", nullable: false),
                    NameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PricingTypeSnapshot = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsIncludedInPackage = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServiceRecords", x => x.Id);
                    table.CheckConstraint("CK_BookingService_Amounts", "[UnitPriceSnapshot] >= 0 AND [Subtotal] >= 0");
                    table.CheckConstraint("CK_BookingService_Included", "[IsIncludedInPackage] = 0 OR [Subtotal] = 0");
                    table.CheckConstraint("CK_BookingService_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_BookingServiceRecords_BookingRecords_BookingId",
                        column: x => x.BookingId,
                        principalTable: "BookingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingServiceRecords_SpaceServiceRecords_SpaceServiceId",
                        column: x => x.SpaceServiceId,
                        principalTable: "SpaceServiceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxAttendees = table.Column<int>(type: "int", nullable: false),
                    TicketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    AccessCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRecords", x => x.Id);
                    table.CheckConstraint("CK_Event_Capacity", "[MaxAttendees] > 0");
                    table.CheckConstraint("CK_Event_Period", "[EndsAt] > [StartsAt]");
                    table.CheckConstraint("CK_Event_Price", "[TicketPrice] >= 0");
                    table.CheckConstraint("CK_Event_PrivateCode", "[Visibility] = 0 OR [AccessCode] IS NOT NULL");
                    table.CheckConstraint("CK_Event_Status", "[Status] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_Event_Visibility", "[Visibility] IN (0,1)");
                    table.ForeignKey(
                        name: "FK_EventRecords_BookingRecords_BookingId",
                        column: x => x.BookingId,
                        principalTable: "BookingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventTagRecords",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTagRecords", x => new { x.EventId, x.TagId });
                    table.ForeignKey(
                        name: "FK_EventTagRecords_EventRecords_EventId",
                        column: x => x.EventId,
                        principalTable: "EventRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventTagRecords_TagRecords_TagId",
                        column: x => x.TagId,
                        principalTable: "TagRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationRecords", x => x.Id);
                    table.CheckConstraint("CK_Registration_Status", "[Status] IN (0,1)");
                    table.ForeignKey(
                        name: "FK_RegistrationRecords_EventRecords_EventId",
                        column: x => x.EventId,
                        principalTable: "EventRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationRecords_UserRecords_UserId",
                        column: x => x.UserId,
                        principalTable: "UserRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketRecords_RegistrationRecords_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "RegistrationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "RoleRecords",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Attendee" },
                    { 2, "Owner" },
                    { 3, "Organizer" }
                });

            migrationBuilder.InsertData(
                table: "TagRecords",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "IT" },
                    { 2, "Music" },
                    { 3, "Workshop" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRecords_PackageId",
                table: "BookingRecords",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRecords_SpaceId_Status_StartsAt_EndsAt",
                table: "BookingRecords",
                columns: new[] { "SpaceId", "Status", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRecords_UserId",
                table: "BookingRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServiceRecords_BookingId",
                table: "BookingServiceRecords",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServiceRecords_SpaceServiceId",
                table: "BookingServiceRecords",
                column: "SpaceServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_AccessCode",
                table: "EventRecords",
                column: "AccessCode",
                unique: true,
                filter: "[AccessCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_BookingId",
                table: "EventRecords",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTagRecords_TagId",
                table: "EventTagRecords",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerProfileRecords_UserId",
                table: "OrganizerProfileRecords",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageItemRecords_SpaceServiceId",
                table: "PackageItemRecords",
                column: "SpaceServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageRecords_SpaceId",
                table: "PackageRecords",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRecords_EventId_UserId",
                table: "RegistrationRecords",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRecords_UserId",
                table: "RegistrationRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleRecords_Name",
                table: "RoleRecords",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaceRecords_OwnerId",
                table: "SpaceRecords",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceServiceRecords_SpaceId",
                table: "SpaceServiceRecords",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceWorkingHoursRecords_SpaceId_DayOfWeek",
                table: "SpaceWorkingHoursRecords",
                columns: new[] { "SpaceId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagRecords_Name",
                table: "TagRecords",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketRecords_Code",
                table: "TicketRecords",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketRecords_RegistrationId",
                table: "TicketRecords",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRecords_Email",
                table: "UserRecords",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleRecords_RoleId",
                table: "UserRoleRecords",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingServiceRecords");

            migrationBuilder.DropTable(
                name: "EventTagRecords");

            migrationBuilder.DropTable(
                name: "OrganizerProfileRecords");

            migrationBuilder.DropTable(
                name: "PackageItemRecords");

            migrationBuilder.DropTable(
                name: "SpaceWorkingHoursRecords");

            migrationBuilder.DropTable(
                name: "TicketRecords");

            migrationBuilder.DropTable(
                name: "UserRoleRecords");

            migrationBuilder.DropTable(
                name: "TagRecords");

            migrationBuilder.DropTable(
                name: "SpaceServiceRecords");

            migrationBuilder.DropTable(
                name: "RegistrationRecords");

            migrationBuilder.DropTable(
                name: "RoleRecords");

            migrationBuilder.DropTable(
                name: "EventRecords");

            migrationBuilder.DropTable(
                name: "BookingRecords");

            migrationBuilder.DropTable(
                name: "PackageRecords");

            migrationBuilder.DropTable(
                name: "SpaceRecords");

            migrationBuilder.DropTable(
                name: "UserRecords");
        }
    }
}
