using SimpleAuthAPI.Controllers;

namespace SimpleAuthAPI.Models;

public class QuestionDto
{
    public int Id { get; set; }
    public string QuestionBody { get; set; }
    public int DifficultyLevel { get; set; }
    public bool QsChecked { get; set; }
    public int CreatedById { get; set; }
    public UserSummaryDto Creator { get; set; }
    public DateTime Created { get; set; }
    public List<AnswerSimple> Answers { get; set; }
    public List<Category> Categories { get; set; }

}
