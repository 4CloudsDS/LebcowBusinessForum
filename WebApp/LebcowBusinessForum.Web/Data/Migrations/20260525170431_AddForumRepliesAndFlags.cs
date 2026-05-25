using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LebcowBusinessForum.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForumRepliesAndFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                schema: "BusinessForums",
                table: "ForumPosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ForumReplies",
                schema: "BusinessForums",
                columns: table => new
                {
                    ReplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumReplies", x => x.ReplyId);
                    table.ForeignKey(
                        name: "FK_ForumReplies_ForumPosts_PostId",
                        column: x => x.PostId,
                        principalSchema: "BusinessForums",
                        principalTable: "ForumPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumReplies_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "BusinessForums",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_AuthorId",
                schema: "BusinessForums",
                table: "ForumReplies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_CreatedAt",
                schema: "BusinessForums",
                table: "ForumReplies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_PostId",
                schema: "BusinessForums",
                table: "ForumReplies",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumReplies",
                schema: "BusinessForums");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                schema: "BusinessForums",
                table: "ForumPosts");
        }
    }
}
