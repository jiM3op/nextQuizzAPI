using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleAuthAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForeignKeyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Questions",
                newName: "CreatedById");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedById",
                table: "Quizzes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_QuizId",
                table: "QuizSessions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_UserId",
                table: "QuizSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedById",
                table: "Questions",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Users_CreatedById",
                table: "Questions",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSessions_Quizzes_QuizId",
                table: "QuizSessions",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSessions_Users_UserId",
                table: "QuizSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Users_CreatedById",
                table: "Quizzes",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Users_CreatedById",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSessions_Quizzes_QuizId",
                table: "QuizSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSessions_Users_UserId",
                table: "QuizSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Users_CreatedById",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_CreatedById",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_QuizId",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_UserId",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CreatedById",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Quizzes");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Questions",
                newName: "CreatedBy");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
