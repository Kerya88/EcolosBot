using EcolosBot.Enums;

namespace EcolosBot.Entities
{
    public class Quiz
    {
        public QuizType QuizType { get; set; }
        public Question[] Questions { get; set; }
    }
}
