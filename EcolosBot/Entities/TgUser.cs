using EcolosBot.Enums;

namespace EcolosBot.Entities
{
    public class TgUser
    {
        public long Id { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Facility { get; set; }
        public string Building { get; set; }
        public string Region { get; set; }
        public string Notes { get; set; }
        public int QuestionIndex { get; set; }
        public UserActivityState UserActivityState { get; set; }
        public List<Quiz> Quizes { get; set; }
    }
}
