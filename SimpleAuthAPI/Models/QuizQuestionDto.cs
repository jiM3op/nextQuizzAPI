namespace SimpleAuthAPI.Models;

public class QuizQuestionDto
{
    public int Id { get; set; }
    public string QuestionBody { get; set; }
    public int DifficultyLevel { get; set; }
    public List<QuizAnswerDto> Answers { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    // Static method to convert from QuestionSimple to QuizQuestionDto
    public static QuizQuestionDto FromQuestionSimple(QuestionSimple question)
    {
        var quizQuestion = new QuizQuestionDto
        {
            Id = question.Id,
            QuestionBody = question.QuestionBody,
            DifficultyLevel = question.DifficultyLevel,
            Categories = question.Categories.Select(id => new Category { Id = id }).ToList()
        };

        // Convert answers without including correctness
        if (question.Answers != null)
        {
            quizQuestion.Answers = question.Answers
                .Select(QuizAnswerDto.FromAnswerSimple)
                .ToList();
        }

        return quizQuestion;
    }
}