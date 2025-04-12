namespace SimpleAuthAPI.Models;

public class QuizAnswerDto
{
    public int Id { get; set; }
    public string AnswerBody { get; set; }
    public string AnswerPosition { get; set; }
    public int QuestionSimpleId { get; set; }

    // AnswerCorrect is intentionally omitted

    // Static method to convert from AnswerSimple to QuizAnswerDto
    public static QuizAnswerDto FromAnswerSimple(AnswerSimple answer)
    {
        return new QuizAnswerDto
        {
            Id = answer.Id,
            AnswerBody = answer.AnswerBody,
            AnswerPosition = answer.AnswerPosition,
            QuestionSimpleId = answer.QuestionSimpleId
        };
    }
}