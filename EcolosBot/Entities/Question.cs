namespace EcolosBot.Entities
{
    public class Question
    {
        public string Text { get; set; }
        public string[]? SuggestedAnswers { get; set; }
        public string Answer { get; set; }
    }
}
