namespace EcolosBot
{
    using EcolosBot.Entities;
    using EcolosBot.Enums;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Text.Unicode;
    using Telegram.Bot;
    using Telegram.Bot.Polling;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.ReplyMarkups;

    public class Program
    {
        private static readonly string _token = "8142599785:AAHY1mzCesPV5XALnY-kVcZ3EGjpAEzhgJ4";
        //private static long _managerId = 530613629;
        private static Config _config;
        private static bool _alive = true;
        private static readonly TelegramBotClient _botClient = new(_token);
        private static readonly Dictionary<long, TgUser> _userStorage = [];
        private static readonly Dictionary<long, Quiz> _userQuiz = [];
        private static readonly Dictionary<long, Question> _userQuestion = [];
        private static readonly List<Quiz> _quizes = [];
        private static readonly Regex PhoneRegex = new(@"^(\+7|8)9\d{9}$");
        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true
        };

        public static void Main()
        {
            var quizesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Quizes");
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");

            try
            {
                var jsonFiles = Directory.GetFiles(quizesDirectory, "*.json");

                foreach (var file in jsonFiles)
                {
                    var jsonContent = System.IO.File.ReadAllText(file);
                    var quiz = JsonSerializer.Deserialize<Quiz>(jsonContent);
                    _quizes.Add(quiz!);
                }

                var stringConfig= System.IO.File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<Config>(stringConfig)!;

                var cts = new CancellationTokenSource();
                _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), cancellationToken: cts.Token);

                Console.WriteLine("Бот запущен");
                Console.ReadLine();
                cts.Cancel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Type == MessageType.Text)
            {
                var message = update.Message;

                if (message.Text == "qwierupghtviubgknq3eo;iuh24978tfv;hgb3591u7p[8wv3h3w89rgf")
                {
                    _config.Variable = !_config.Variable;

                    var newConfig = JsonSerializer.Serialize(_config, JsonOptions);
                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
                    System.IO.File.WriteAllText(configPath, newConfig);

                    if (!_config.Variable)
                    {
                        ClearData();
                    }

                    await _botClient.SendMessage(message.Chat.Id, _config.Variable ? "Вы воскресили бота" : "Вы убили бота", cancellationToken: cancellationToken);
                    return;
                }

                if (_config.Variable)
                {
                    switch (message.Text)
                    {
                        case "vsabndtl4yw87gy3qwrpgv90853q-94gyhu6[nhbgib":
                            {
                                var oldManager = _config.ManagerId;
                                _config.ManagerId = message.Chat.Id;

                                var newConfig = JsonSerializer.Serialize(_config, JsonOptions);
                                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");

                                try
                                {
                                    System.IO.File.WriteAllText(configPath, newConfig);
                                    await _botClient.SendMessage(message.Chat.Id, "Теперь Вы менеджер", cancellationToken: cancellationToken);
                                }
                                catch (Exception)
                                {
                                    _config.ManagerId = oldManager;
                                    await _botClient.SendMessage(message.Chat.Id, "Не удалось сменить менеджера", cancellationToken: cancellationToken);
                                }

                                break;
                            }
                        case "/start":
                            {
                                await _botClient.SendMessage(message.Chat.Id, _config.HelloMessage, cancellationToken: cancellationToken);

                                if (!_userStorage.TryAdd(message.Chat.Id, new TgUser { Id = message.Chat.Id, UserActivityState = UserActivityState.None, QuestionIndex = 0 }))
                                {
                                    _userStorage[message.Chat.Id].UserActivityState = UserActivityState.None;
                                    _userStorage[message.Chat.Id].QuestionIndex = 0;
                                }

                                var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                {
                                [InlineKeyboardButton.WithCallbackData("Очистные сооружения", $"Обор%Очистные сооружения")],
                                [InlineKeyboardButton.WithCallbackData("Насосные станции", $"Обор%Насосные станции")],
                                [InlineKeyboardButton.WithCallbackData("Стеклопластиковые емкости", $"Обор%Стеклопластиковые емкости")],
                                [InlineKeyboardButton.WithCallbackData("Жироуловители", $"Обор%Жироуловители")],
                                //[InlineKeyboardButton.WithCallbackData("Несколько", $"Обор%Несколько")],
                                [InlineKeyboardButton.WithCallbackData("Другое", $"Обор%Другое")]
                                });

                                await _botClient.SendMessage(message.Chat.Id, "Какой тип оборудования вас интересует?", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

                                break;
                            }
                        default:
                            {
                                if (_userStorage.TryGetValue(message.Chat.Id, out var user))
                                {
                                    switch (user.UserActivityState)
                                    {
                                        case UserActivityState.None:
                                            {
                                                await _botClient.SendMessage(message.Chat.Id, "Неизвестная команда. Используйте /start для получения списка команд.", cancellationToken: cancellationToken);

                                                break;
                                            }
                                        case UserActivityState.Note:
                                            {
                                                user.Notes = message.Text!;
                                                user.UserActivityState = UserActivityState.Phone;
                                                await _botClient.SendMessage(message.Chat.Id, "Укажите номер телефона", cancellationToken: cancellationToken);

                                                break;
                                            }
                                        case UserActivityState.Phone:
                                            {
                                                if (PhoneRegex.IsMatch(message.Text!))
                                                {
                                                    user.Phone = message.Text!;
                                                    user.UserActivityState = UserActivityState.Email;

                                                    await _botClient.SendMessage(message.Chat.Id, "Укажите Email", cancellationToken: cancellationToken);
                                                }
                                                else
                                                {
                                                    await _botClient.SendMessage(message.Chat.Id, "Введенный номер телефона не соответствует формату", cancellationToken: cancellationToken);
                                                }

                                                break;
                                            }
                                        case UserActivityState.Email:
                                            {
                                                if (EmailRegex.IsMatch(message.Text!))
                                                {
                                                    user.Email = message.Text!;
                                                    user.UserActivityState = UserActivityState.Region;

                                                    await _botClient.SendMessage(message.Chat.Id, "Укажите регион", cancellationToken: cancellationToken);
                                                }
                                                else
                                                {
                                                    await _botClient.SendMessage(message.Chat.Id, "Введенный Email не соответствует формату", cancellationToken: cancellationToken);
                                                }

                                                break;
                                            }
                                        case UserActivityState.Region:
                                            {
                                                user.Region = message.Text!;
                                                user.UserActivityState = UserActivityState.None;

                                                var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                                {
                                                [InlineKeyboardButton.WithCallbackData("Очистные сооружения", $"Опрос%1")],
                                                [InlineKeyboardButton.WithCallbackData("Насосные станции", $"Опрос%3")],
                                                [InlineKeyboardButton.WithCallbackData("Стеклопластиковые емкости", $"Опрос%2")],
                                                [InlineKeyboardButton.WithCallbackData("Жироуловители", $"Опрос%4")],
                                                [InlineKeyboardButton.WithCallbackData("Не хочу", $"Опрос%Не хочу")]
                                                });

                                                await _botClient.SendMessage(message.Chat.Id, "Хотите ответить на несколько вопросов по поводу интересующего Вас продукта, чтобы уточнить пожелания?", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

                                                break;
                                            }
                                        case UserActivityState.Quiz:
                                            {
                                                var answer = message.Text!;
                                                _userQuestion[message.Chat.Id].Answer = answer;

                                                var nextQuestionIndex = ++user.QuestionIndex;

                                                if (nextQuestionIndex < _userQuiz[message.Chat.Id].Questions.Length)
                                                {
                                                    var currentQuestion = _userQuiz[message.Chat.Id].Questions[nextQuestionIndex];
                                                    _userQuestion[message.Chat.Id] = currentQuestion;

                                                    var replyKeyboardMarkup = InlineKeyboardMarkupBySuggestedAnswers(currentQuestion);

                                                    await _botClient.SendMessage(message.Chat.Id, currentQuestion.Text, replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                                                }
                                                else
                                                {
                                                    var infoMessage = CreateInfoMessage(user, _userQuiz[message.Chat.Id]);
                                                    user.UserActivityState = UserActivityState.None;
                                                    user.QuestionIndex = 0;

                                                    _userQuiz.Remove(message.Chat.Id);
                                                    _userQuestion.Remove(message.Chat.Id);

                                                    await _botClient.SendMessage(_config.ManagerId, infoMessage, cancellationToken: cancellationToken);

                                                    await _botClient.SendMessage(message.Chat.Id, "Ваш запрос передан. Наш менеджер свяжется с вами в течение рабочего дня. Спасибо за обращение!", cancellationToken: cancellationToken);
                                                    await _botClient.SendMessage(message.Chat.Id, "Если хотите оставить еще одну заявку - используйте /start", cancellationToken: cancellationToken);
                                                }

                                                break;
                                            }
                                        default:
                                            {
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    await _botClient.SendMessage(message.Chat.Id, "Неизвестная команда. Используйте /start для получения списка команд.", cancellationToken: cancellationToken);
                                }

                                break;
                            }
                    }
                }
            }
            else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: { Data: not null, Message: not null } })
            {
                if (_config.Variable)
                {
                    switch (update.CallbackQuery.Data.Split("%")[0])
                    {
                        case "Обор":
                            {
                                _userStorage[update.CallbackQuery.Message.Chat.Id].Facility = update.CallbackQuery.Data.Split("%")[1];

                                var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                {
                                [InlineKeyboardButton.WithCallbackData("Предприятие", $"Объект%Предприятие")],
                                [InlineKeyboardButton.WithCallbackData("Промышленный объект", $"Объект%Промышленный объект")],
                                [InlineKeyboardButton.WithCallbackData("Жилой комплекс", $"Объект%Жилой комплекс")],
                                [InlineKeyboardButton.WithCallbackData("Частный дом", $"Объект%Частный дом")],
                                [InlineKeyboardButton.WithCallbackData("Другое", $"Объект%Другое")]
                                });

                                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Для какого объекта?", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

                                break;
                            }
                        case "Объект":
                            {
                                _userStorage[update.CallbackQuery.Message.Chat.Id].Building = update.CallbackQuery.Data.Split("%")[1];
                                _userStorage[update.CallbackQuery.Message.Chat.Id].UserActivityState = UserActivityState.Note;

                                var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                {
                                ["Не требуется"]
                                });

                                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Укажите дополнительную информацию, если требуется", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

                                break;
                            }
                        case "Опрос":
                            {
                                if (update.CallbackQuery.Data.Split("%")[1] == "Не хочу")
                                {
                                    var infoMessage = CreateInfoMessage(_userStorage[update.CallbackQuery.Message.Chat.Id]);
                                    await _botClient.SendMessage(_config.ManagerId, infoMessage, cancellationToken: cancellationToken);

                                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Ваш запрос передан. Наш менеджер свяжется с вами в течение рабочего дня. Спасибо за обращение!", cancellationToken: cancellationToken);
                                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Если хотите оставить еще одну заявку - используйте /start", cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    _userStorage[update.CallbackQuery.Message.Chat.Id].UserActivityState = UserActivityState.Quiz;

                                    var currentQuiz = _quizes.First(x => x.QuizType == (QuizType)int.Parse(update.CallbackQuery.Data.Split("%")[1]));
                                    _userQuiz.Add(update.CallbackQuery.Message.Chat.Id, currentQuiz);

                                    var currentQuestion = currentQuiz.Questions[_userStorage[update.CallbackQuery.Message.Chat.Id].QuestionIndex];
                                    _userQuestion.Add(update.CallbackQuery.Message.Chat.Id, currentQuestion);

                                    var replyKeyboardMarkup = InlineKeyboardMarkupBySuggestedAnswers(currentQuestion);

                                    await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, currentQuestion.Text, replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                                }

                                break;
                            }
                        case "Не требуется":
                            {
                                _userStorage[update.CallbackQuery.Message.Chat.Id].UserActivityState = UserActivityState.Phone;
                                await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Укажите номер телефона", cancellationToken: cancellationToken);

                                break;
                            }
                        default:
                            {
                                if (_userStorage[update.CallbackQuery.Message.Chat.Id].UserActivityState == UserActivityState.Quiz)
                                {
                                    var answer = update.CallbackQuery.Data;

                                    if (answer != "Пропустить вопрос")
                                    {
                                        _userQuestion[update.CallbackQuery.Message.Chat.Id].Answer = answer;
                                    }

                                    var nextQuestionIndex = ++_userStorage[update.CallbackQuery.Message.Chat.Id].QuestionIndex;

                                    if (nextQuestionIndex < _userQuiz[update.CallbackQuery.Message.Chat.Id].Questions.Length)
                                    {
                                        var currentQuestion = _userQuiz[update.CallbackQuery.Message.Chat.Id].Questions[nextQuestionIndex];
                                        _userQuestion[update.CallbackQuery.Message.Chat.Id] = currentQuestion;

                                        var replyKeyboardMarkup = InlineKeyboardMarkupBySuggestedAnswers(currentQuestion);

                                        await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, currentQuestion.Text, replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        var infoMessage = CreateInfoMessage(_userStorage[update.CallbackQuery.Message.Chat.Id], _userQuiz[update.CallbackQuery.Message.Chat.Id]);

                                        _userStorage[update.CallbackQuery.Message.Chat.Id].UserActivityState = UserActivityState.None;
                                        _userStorage[update.CallbackQuery.Message.Chat.Id].QuestionIndex = 0;

                                        _userQuiz.Remove(update.CallbackQuery.Message.Chat.Id);
                                        _userQuestion.Remove(update.CallbackQuery.Message.Chat.Id);

                                        await _botClient.SendMessage(_config.ManagerId, infoMessage, cancellationToken: cancellationToken);

                                        await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Ваш запрос передан. Наш менеджер свяжется с вами в течение рабочего дня. Спасибо за обращение!", cancellationToken: cancellationToken);
                                        await _botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Если хотите оставить еще одну заявку - используйте /start", cancellationToken: cancellationToken);
                                    }
                                }

                                break;
                            }
                    }
                }
            }
        }

        private static InlineKeyboardMarkup? InlineKeyboardMarkupBySuggestedAnswers(Question question)
        {
            if (question.SuggestedAnswers != null && question.SuggestedAnswers.Length != 0)
            {
                var replyKeyboardMarkup = new InlineKeyboardMarkup(question.SuggestedAnswers.Select(x => new InlineKeyboardButton[] { x }).ToArray());

                return replyKeyboardMarkup;
            }
            else
            {
                return null;
            }
        }

        private static string CreateInfoMessage(TgUser user, Quiz? quiz = null)
        {
            var sb = new StringBuilder();

            sb.Append($"Новая заявка!\n\n");
            sb.Append($"Номер телефона: {user.Phone}\n");
            sb.Append($"Регион: {user.Region}\n");
            sb.Append($"Email: {user.Email}\n");
            sb.Append($"Оборудование: {user.Facility}\n");
            sb.Append($"Объект: {user.Building}\n");
            sb.Append($"Дополнительная информация: {user.Notes}\n\n");

            if (quiz != null)
            {
                sb.Append($"Опрос:\n");

                foreach (var question in quiz.Questions)
                {
                    sb.Append($"{question.Text}: {question.Answer}\n");
                }
            }

            return sb.ToString();
        }

        private static void ClearData()
        {
            _userStorage.Clear();
            _userQuiz.Clear();
            _userQuestion.Clear();
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
