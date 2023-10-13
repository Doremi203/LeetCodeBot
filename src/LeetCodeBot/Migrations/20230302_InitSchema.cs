using FluentMigrator;

namespace LeetCodeBot.Migrations;

[Migration(20230302, TransactionBehavior.None)]
public class InitSchema : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("telegram_user_id").AsInt64().PrimaryKey("users_pk").NotNullable()
            .WithColumn("difficulty").AsInt32().NotNullable()
            .WithColumn("time_setting").AsInt32().NotNullable()
            .WithColumn("state").AsInt32().NotNullable()
            .WithColumn("is_premium").AsBoolean().NotNullable();
        
        Create.Table("solved_questions")
            .WithColumn("id").AsGuid().PrimaryKey("solved_questions_pk").NotNullable()
            .WithColumn("telegram_user_id").AsInt64()
                .ForeignKey(
                "solved_questions_users_telegram_user_id_fk",
                "users", 
                "telegram_user_id").NotNullable()
            .WithColumn("date").AsDate().NotNullable()
            .WithColumn("question_id").AsInt32().NotNullable();

        Create.Index("solved_questions_telegram_user_id_question_id_uindex")
            .OnTable("solved_questions")
            .OnColumn("telegram_user_id").Unique()
            .OnColumn("question_id").Unique();
    }

    public override void Down()
    {
        Delete.Table("users");
        Delete.Table("solved_questions");
    }
}