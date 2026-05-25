using MathTutor.Application.Abstractions;
using MathTutor.Domain.Entities;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Seed;

public sealed class SeedData(MathTutorDbContext dbContext, IPasswordHasher passwordHasher)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = await EnsureUserAsync("Admin", "MathTutor", "admin@mathtutor.local", "Admin123!", UserRole.Admin, cancellationToken);
        var anna = await EnsureUserAsync("Анна", "Иванова", "anna@student.local", "Student123!", UserRole.Student, cancellationToken);
        var pavel = await EnsureUserAsync("Павел", "Смирнов", "pavel@student.local", "Student123!", UserRole.Student, cancellationToken);

        var linear = await EnsureMaterialAsync(admin.Id, "Линейные уравнения", "Алгебра", MaterialType.Theory, DifficultyLevel.Beginner, "Базовые приёмы решения линейных уравнений.", "Линейное уравнение имеет вид ax + b = 0. Перенесите свободный член и разделите на коэффициент при x.", true, cancellationToken, "Линейные уравнения");
        var quadratic = await EnsureMaterialAsync(admin.Id, "Квадратные уравнения", "Алгебра", MaterialType.Practice, DifficultyLevel.Intermediate, "Дискриминант, корни и проверка решений.", "Для ax^2 + bx + c = 0 используйте D = b^2 - 4ac. Если D >= 0, найдите корни по формуле.", true, cancellationToken, "Квадратные уравнения");
        var percents = await EnsureMaterialAsync(admin.Id, "Проценты: вводный тест", "Проценты и доли", MaterialType.Test, DifficultyLevel.Beginner, "Практика вычисления процентов и долей.", "Процент - это сотая часть числа. 15% от 200 равно 0.15 * 200.", true, cancellationToken, "Базовые проценты");
        var functions = await EnsureMaterialAsync(admin.Id, "Функции и графики", "Функции", MaterialType.Theory, DifficultyLevel.Advanced, "Область определения, значения и чтение графиков.", "Функция сопоставляет каждому x из области определения единственное значение y. График помогает увидеть зависимость между x и y.", true, cancellationToken, "Графики");

        var task1 = await EnsureTaskAsync(linear.Id, "Решите уравнение", "2x + 6 = 0", AnswerType.Number, "-3", 1, "Перенесите 6 вправо и разделите на 2.", DifficultyLevel.Beginner, cancellationToken);
        var task2 = await EnsureTaskAsync(quadratic.Id, "Дискриминант", "Найдите дискриминант уравнения x^2 - 5x + 6 = 0", AnswerType.Number, "1", 2, "D = 25 - 24 = 1.", DifficultyLevel.Intermediate, cancellationToken);
        var task3 = await EnsureTaskAsync(percents.Id, "Процент от числа", "Сколько будет 15% от 200?", AnswerType.Number, "30", 1, "0.15 * 200 = 30.", DifficultyLevel.Beginner, cancellationToken);
        for (var i = 2; i <= 10; i++)
        {
            await EnsureTaskAsync(percents.Id, $"Вводный вопрос {i}", $"Введите 1, чтобы пройти вводный вопрос {i} по процентам.", AnswerType.Number, "1", 1, "Дополнительное задание вводного теста.", DifficultyLevel.Beginner, cancellationToken);
        }

        await EnsureOptionAsync(task1.Id, "-3", true, 1, cancellationToken);
        await EnsureOptionAsync(task1.Id, "3", false, 2, cancellationToken);
        await EnsureTheoryMaterialsAsync(admin.Id, cancellationToken);
        await EnsureRichTestsAsync(admin.Id, cancellationToken);
        await EnsureExpandedCurriculumAsync(admin.Id, cancellationToken);
        await EnsureSubmissionAsync(anna.Id, task1.Id, "-3", 1, 1, SubmissionStatus.AutoChecked, "Ответ верный.", cancellationToken);
        await EnsureSubmissionAsync(pavel.Id, task3.Id, "25", 0, 1, SubmissionStatus.AutoChecked, "Ответ неверный. Правильный ответ: 30.", cancellationToken);
        await EnsureReviewAsync(anna.Id, linear.Id, null, 2, 5, "Понятное объяснение, удобно повторять перед тестом.", cancellationToken);
        await EnsureNotificationAsync(anna.Id, "Добро пожаловать", "Новые задания по линейным уравнениям уже доступны.", NotificationType.NewMaterial, cancellationToken);
        await EnsureNotificationAsync(pavel.Id, "Оценка выставлена", "Ваше решение по процентам проверено автоматически.", NotificationType.Grade, cancellationToken);
    }

    private async Task<User> EnsureUserAsync(string firstName, string lastName, string email, string password, UserRole role, CancellationToken cancellationToken)
    {
        var normalized = email.ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == normalized, cancellationToken);
        if (user is not null) return user;

        user = new User { FirstName = firstName, LastName = lastName, Email = normalized, PasswordHash = passwordHasher.Hash(password), Role = role, CreatedAtUtc = DateTime.UtcNow };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<EducationalMaterial> EnsureMaterialAsync(int adminId, string title, string topic, MaterialType type, DifficultyLevel difficulty, string description, string content, bool published, CancellationToken cancellationToken, string section = "Основы")
    {
        var material = await dbContext.EducationalMaterials.FirstOrDefaultAsync(x => x.Title == title, cancellationToken);
        if (material is not null)
        {
            material.Topic = topic;
            material.Section = section;
            material.Type = type;
            material.Difficulty = difficulty;
            material.Description = description;
            material.TheoryContent = content;
            material.IsPublished = published;
            material.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return material;
        }
        material = new EducationalMaterial { CreatedByUserId = adminId, Title = title, Topic = topic, Section = section, Type = type, Difficulty = difficulty, Description = description, TheoryContent = content, IsPublished = published, CreatedAtUtc = DateTime.UtcNow };
        dbContext.EducationalMaterials.Add(material);
        await dbContext.SaveChangesAsync(cancellationToken);
        return material;
    }

    private async Task<MathTask> EnsureTaskAsync(int materialId, string title, string question, AnswerType answerType, string correctAnswer, decimal maxScore, string explanation, DifficultyLevel difficulty, CancellationToken cancellationToken)
    {
        var task = await dbContext.MathTasks.FirstOrDefaultAsync(x => x.EducationalMaterialId == materialId && x.Title == title, cancellationToken);
        if (task is not null)
        {
            task.Question = question;
            task.AnswerType = answerType;
            task.CorrectAnswer = correctAnswer;
            task.MaxScore = maxScore;
            task.Explanation = explanation;
            task.Difficulty = difficulty;
            await dbContext.SaveChangesAsync(cancellationToken);
            return task;
        }
        task = new MathTask { EducationalMaterialId = materialId, Title = title, Question = question, AnswerType = answerType, CorrectAnswer = correctAnswer, MaxScore = maxScore, Explanation = explanation, Difficulty = difficulty, CreatedAtUtc = DateTime.UtcNow };
        dbContext.MathTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return task;
    }

    private async Task EnsureTheoryMaterialsAsync(int adminId, CancellationToken cancellationToken)
    {
        await EnsureMaterialAsync(adminId, "Методы решения линейных уравнений", "Алгебра", MaterialType.Theory, DifficultyLevel.Beginner, "Как переносить слагаемые и проверять корни.", "1. Раскройте скобки. 2. Перенесите слагаемые с x в одну сторону, числа - в другую. 3. Разделите на коэффициент при x. 4. Подставьте найденный корень обратно в уравнение.", true, cancellationToken, "Линейные уравнения");
        await EnsureMaterialAsync(adminId, "Дискриминант и корни", "Алгебра", MaterialType.Theory, DifficultyLevel.Intermediate, "Краткая теория по квадратным уравнениям.", "Для ax^2 + bx + c = 0 вычислите D = b^2 - 4ac. Если D > 0, корня два. Если D = 0, корень один. Если D < 0, действительных корней нет.", true, cancellationToken, "Квадратные уравнения");
        await EnsureMaterialAsync(adminId, "Проценты в задачах", "Проценты и доли", MaterialType.Theory, DifficultyLevel.Beginner, "Перевод процентов в дроби и решение задач.", "p% от числа a равно a * p / 100. Для скидки сначала найдите величину скидки, затем вычтите её из исходной цены. Для процентного отношения разделите часть на целое и умножьте на 100.", true, cancellationToken, "Базовые проценты");
        await EnsureMaterialAsync(adminId, "Функция и её график", "Функции", MaterialType.Theory, DifficultyLevel.Intermediate, "Что такое функция, график, коэффициенты k и b.", "Функция задаёт соответствие x -> y. Для y = kx + b число k отвечает за наклон прямой, а b показывает пересечение с осью y. Нуль функции - значение x, при котором y = 0.", true, cancellationToken, "Графики");
        await EnsureMaterialAsync(adminId, "Треугольники: основные свойства", "Геометрия", MaterialType.Theory, DifficultyLevel.Intermediate, "Углы, площадь и периметр треугольников.", "Сумма углов треугольника равна 180 градусам. Площадь равна половине произведения основания на высоту. В прямоугольном треугольнике гипотенуза лежит напротив прямого угла.", true, cancellationToken, "Треугольники");
    }

    private async Task EnsureRichTestsAsync(int adminId, CancellationToken cancellationToken)
    {
        await EnsureTestAsync(adminId, "Тест: линейные уравнения", DifficultyLevel.Beginner, "8 вопросов на перенос слагаемых, раскрытие скобок и проверку корней.", "Повторите правило: одинаковые действия с обеими частями уравнения сохраняют равенство.", cancellationToken,
            new("Корень уравнения", "x + 7 = 12", AnswerType.Number, "5", 1, "12 - 7 = 5", DifficultyLevel.Beginner),
            new("Перенос слагаемого", "3x - 9 = 0", AnswerType.Number, "3", 1, "3x = 9, x = 3.", DifficultyLevel.Beginner),
            new("Отрицательный корень", "4x + 20 = 0", AnswerType.Number, "-5", 1, "4x = -20.", DifficultyLevel.Beginner),
            new("Уравнение со скобками", "2(x + 3) = 14", AnswerType.Number, "4", 1, "x + 3 = 7.", DifficultyLevel.Beginner),
            new("Один вариант", "Какой корень уравнения 5x = 25?", AnswerType.SingleChoice, "5", 1, "25 / 5 = 5.", DifficultyLevel.Beginner, "4", "5", "20", "-5"),
            new("Коэффициент при x", "В уравнении 7x - 2 = 19 коэффициент при x равен", AnswerType.SingleChoice, "7", 1, "Коэффициент стоит перед переменной.", DifficultyLevel.Beginner, "7", "-2", "19", "21"),
            new("Два верных преобразования", "Выберите верные шаги для 2x + 8 = 18", AnswerType.MultipleChoice, "2x = 10;x = 5", 2, "Сначала переносим 8, затем делим на 2.", DifficultyLevel.Beginner, "2x = 10", "x = 5", "x = 13", "2x = 26"),
            new("Проверка ответа", "Подходит ли x = 6 для уравнения x - 2 = 4?", AnswerType.Text, "да", 1, "6 - 2 = 4.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: квадратные уравнения", DifficultyLevel.Intermediate, "Дискриминант, корни и количество решений.", "Для ax^2 + bx + c = 0 используйте D = b^2 - 4ac.", cancellationToken,
            new("Дискриминант", "D для x^2 - 6x + 5 = 0", AnswerType.Number, "16", 1, "36 - 20 = 16.", DifficultyLevel.Intermediate),
            new("Количество корней", "Сколько корней у уравнения x^2 + 4x + 4 = 0?", AnswerType.Number, "1", 1, "D = 0.", DifficultyLevel.Intermediate),
            new("Корень уравнения", "Один из корней x^2 - 9 = 0", AnswerType.SingleChoice, "3", 1, "Корни: -3 и 3.", DifficultyLevel.Intermediate, "0", "3", "9", "6"),
            new("Формула D", "Выберите формулу дискриминанта", AnswerType.SingleChoice, "b^2 - 4ac", 1, "Это стандартная формула.", DifficultyLevel.Intermediate, "b^2 - 4ac", "a^2 - 4bc", "2a + b", "c - b"),
            new("Корни", "Выберите корни x^2 - 5x + 6 = 0", AnswerType.MultipleChoice, "2;3", 2, "Произведение 6, сумма 5.", DifficultyLevel.Intermediate, "1", "2", "3", "6"),
            new("Неполное уравнение", "x^2 = 49. Положительный корень равен", AnswerType.Number, "7", 1, "sqrt(49) = 7.", DifficultyLevel.Beginner),
            new("Знак D", "Если D < 0, действительных корней", AnswerType.SingleChoice, "нет", 1, "Квадратный корень из отрицательного D в действительных числах не берётся.", DifficultyLevel.Intermediate, "нет", "один", "два", "бесконечно много"),
            new("Проверка", "Является ли 2 корнем x^2 - 4 = 0?", AnswerType.Text, "да", 1, "2^2 - 4 = 0.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: проценты и доли", DifficultyLevel.Beginner, "Проценты, скидки, прирост и доли числа.", "Процент переводится в десятичную дробь делением на 100.", cancellationToken,
            new("Процент от числа", "20% от 150", AnswerType.Number, "30", 1, "0.2 * 150 = 30.", DifficultyLevel.Beginner),
            new("Скидка", "Цена 1000 уменьшилась на 15%. Скидка равна", AnswerType.Number, "150", 1, "0.15 * 1000.", DifficultyLevel.Beginner),
            new("Новая цена", "После скидки 10% от 500 новая цена", AnswerType.Number, "450", 1, "500 - 50.", DifficultyLevel.Beginner),
            new("Доля", "25% это какая дробь?", AnswerType.SingleChoice, "1/4", 1, "25 из 100 = 1/4.", DifficultyLevel.Beginner, "1/2", "1/4", "3/4", "1/5"),
            new("Верные равенства", "Выберите верные записи 50%", AnswerType.MultipleChoice, "0.5;1/2", 2, "50% = 0.5 = 1/2.", DifficultyLevel.Beginner, "0.5", "1/2", "5", "50"),
            new("Прирост", "Число 80 увеличили на 25%. Результат", AnswerType.Number, "100", 1, "80 + 20.", DifficultyLevel.Beginner),
            new("Процентное отношение", "10 от 40 это сколько процентов?", AnswerType.Number, "25", 1, "10 / 40 * 100.", DifficultyLevel.Beginner),
            new("Понятие", "Процент - это ... часть числа", AnswerType.Text, "сотая", 1, "1% = 1/100.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: функции и графики", DifficultyLevel.Intermediate, "Область определения, значения функции и чтение графиков.", "Функция каждому допустимому x ставит в соответствие одно значение y.", cancellationToken,
            new("Значение функции", "f(x)=2x+1. Найдите f(3)", AnswerType.Number, "7", 1, "2 * 3 + 1.", DifficultyLevel.Beginner),
            new("Нуль функции", "Нуль функции y = x - 4", AnswerType.Number, "4", 1, "y = 0 при x = 4.", DifficultyLevel.Intermediate),
            new("Линейная функция", "График y = kx + b является", AnswerType.SingleChoice, "прямой", 1, "Линейная функция задаёт прямую.", DifficultyLevel.Beginner, "прямой", "параболой", "окружностью", "гиперболой"),
            new("Возрастание", "y = 3x + 2 при положительном коэффициенте k", AnswerType.SingleChoice, "возрастает", 1, "k > 0.", DifficultyLevel.Intermediate, "возрастает", "убывает", "постоянна", "не является функцией"),
            new("Свойства функции", "Выберите верные утверждения о функции", AnswerType.MultipleChoice, "каждому x соответствует одно y;есть область определения", 2, "Это базовые свойства функции.", DifficultyLevel.Intermediate, "каждому x соответствует одно y", "есть область определения", "каждому x соответствует два y", "у функции нет значений"),
            new("Коэффициент", "В y = -2x + 5 коэффициент k равен", AnswerType.Number, "-2", 1, "k стоит перед x.", DifficultyLevel.Beginner),
            new("Свободный член", "В y = 4x - 9 свободный член равен", AnswerType.Number, "-9", 1, "Это b.", DifficultyLevel.Beginner),
            new("Область определения", "Для y = 1/x нельзя брать x =", AnswerType.Number, "0", 1, "Деление на ноль запрещено.", DifficultyLevel.Intermediate));

        await EnsureTestAsync(adminId, "Тест: геометрия треугольников", DifficultyLevel.Intermediate, "Углы, стороны, площадь и признаки треугольников.", "Сумма углов треугольника равна 180 градусам.", cancellationToken,
            new("Сумма углов", "Сумма внутренних углов треугольника", AnswerType.Number, "180", 1, "Базовое свойство треугольника.", DifficultyLevel.Beginner),
            new("Третий угол", "Углы 50 и 60 градусов. Третий угол", AnswerType.Number, "70", 1, "180 - 110.", DifficultyLevel.Beginner),
            new("Площадь", "Площадь при основании 10 и высоте 6", AnswerType.Number, "30", 1, "S = ah / 2.", DifficultyLevel.Intermediate),
            new("Равнобедренный", "В равнобедренном треугольнике равны", AnswerType.SingleChoice, "две стороны", 1, "По определению.", DifficultyLevel.Beginner, "две стороны", "все стороны", "нет сторон", "только углы 90"),
            new("Прямоугольный", "У прямоугольного треугольника есть угол", AnswerType.SingleChoice, "90", 1, "Прямой угол равен 90 градусам.", DifficultyLevel.Beginner, "30", "60", "90", "180"),
            new("Верные формулы", "Выберите формулы площади треугольника", AnswerType.MultipleChoice, "a*h/2;1/2*a*h", 2, "Это одна и та же формула.", DifficultyLevel.Intermediate, "a*h/2", "1/2*a*h", "a*h", "a+b+c"),
            new("Гипотенуза", "В прямоугольном треугольнике самая длинная сторона называется", AnswerType.Text, "гипотенуза", 1, "Она лежит напротив прямого угла.", DifficultyLevel.Beginner),
            new("Периметр", "Стороны 3, 4, 5. Периметр", AnswerType.Number, "12", 1, "3 + 4 + 5.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: степени и корни", DifficultyLevel.Intermediate, "Правила степеней, квадратные корни и вычисления.", "При умножении степеней с одинаковым основанием показатели складываются.", cancellationToken,
            new("Квадрат", "5^2 =", AnswerType.Number, "25", 1, "5 * 5.", DifficultyLevel.Beginner),
            new("Корень", "sqrt(81) =", AnswerType.Number, "9", 1, "9^2 = 81.", DifficultyLevel.Beginner),
            new("Нулевая степень", "7^0 =", AnswerType.Number, "1", 1, "Любое ненулевое число в нулевой степени равно 1.", DifficultyLevel.Intermediate),
            new("Правило умножения", "a^2 * a^3 =", AnswerType.SingleChoice, "a^5", 1, "Показатели складываются.", DifficultyLevel.Intermediate, "a^6", "a^5", "a^1", "a^9"),
            new("Верные корни", "Выберите верные равенства", AnswerType.MultipleChoice, "sqrt(16)=4;sqrt(49)=7", 2, "4^2 = 16, 7^2 = 49.", DifficultyLevel.Beginner, "sqrt(16)=4", "sqrt(49)=7", "sqrt(25)=6", "sqrt(9)=9"),
            new("Степень произведения", "(2*3)^2 =", AnswerType.Number, "36", 1, "6^2.", DifficultyLevel.Intermediate),
            new("Отрицательная степень", "10^-1 =", AnswerType.SingleChoice, "0.1", 1, "Это 1/10.", DifficultyLevel.Advanced, "10", "0.1", "-10", "1"),
            new("Название", "Число под знаком корня называется", AnswerType.Text, "подкоренное", 1, "Подкоренное выражение.", DifficultyLevel.Intermediate));

        await EnsureTestAsync(adminId, "Тест: системы уравнений", DifficultyLevel.Intermediate, "Метод подстановки, сложения и проверка пары решений.", "Решение системы должно удовлетворять всем уравнениям.", cancellationToken,
            new("Пара решения", "Для x + y = 5 и x = 2 значение y", AnswerType.Number, "3", 1, "2 + y = 5.", DifficultyLevel.Beginner),
            new("Сложение", "x + y = 7, x - y = 1. x =", AnswerType.Number, "4", 2, "Сложите уравнения: 2x = 8.", DifficultyLevel.Intermediate),
            new("Значение y", "x + y = 7, x - y = 1. y =", AnswerType.Number, "3", 2, "4 + y = 7.", DifficultyLevel.Intermediate),
            new("Метод", "Если выражаем одну переменную через другую, это метод", AnswerType.SingleChoice, "подстановки", 1, "Далее подставляем выражение во второе уравнение.", DifficultyLevel.Beginner, "подстановки", "процентов", "дискриминанта", "площадей"),
            new("Верная пара", "Какая пара подходит к x + y = 10?", AnswerType.SingleChoice, "(4;6)", 1, "4 + 6 = 10.", DifficultyLevel.Beginner, "(4;6)", "(7;4)", "(1;12)", "(9;2)"),
            new("Верные действия", "Выберите корректные методы решения систем", AnswerType.MultipleChoice, "подстановка;сложение", 2, "Оба метода стандартные.", DifficultyLevel.Intermediate, "подстановка", "сложение", "измерение", "округление всегда"),
            new("Проверка", "Пара (1;2) подходит к x + y = 3?", AnswerType.Text, "да", 1, "1 + 2 = 3.", DifficultyLevel.Beginner),
            new("Количество уравнений", "Обычная система с двумя переменными часто содержит сколько уравнений?", AnswerType.Number, "2", 1, "Два уравнения для двух неизвестных.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: статистика и вероятность", DifficultyLevel.Advanced, "Среднее, медиана, вероятность простых событий.", "Вероятность события равна отношению благоприятных исходов ко всем равновозможным исходам.", cancellationToken,
            new("Среднее", "Среднее чисел 2, 4, 6", AnswerType.Number, "4", 1, "(2+4+6)/3.", DifficultyLevel.Beginner),
            new("Медиана", "Медиана ряда 1, 3, 9", AnswerType.Number, "3", 1, "Средний элемент упорядоченного ряда.", DifficultyLevel.Intermediate),
            new("Вероятность монеты", "Вероятность орла при честной монете", AnswerType.SingleChoice, "1/2", 1, "Один благоприятный исход из двух.", DifficultyLevel.Beginner, "1/2", "1/3", "1", "0"),
            new("Кубик", "Вероятность выпадения 6 на кубике", AnswerType.SingleChoice, "1/6", 1, "Один исход из шести.", DifficultyLevel.Beginner, "1/2", "1/6", "6", "0"),
            new("Показатели ряда", "Выберите характеристики набора данных", AnswerType.MultipleChoice, "среднее;медиана", 2, "Обе используются в статистике.", DifficultyLevel.Intermediate, "среднее", "медиана", "дискриминант", "гипотенуза"),
            new("Размах", "Размах ряда 4, 8, 10", AnswerType.Number, "6", 1, "10 - 4.", DifficultyLevel.Intermediate),
            new("Процент вероятности", "Вероятность 0.25 в процентах", AnswerType.Number, "25", 1, "0.25 * 100.", DifficultyLevel.Intermediate),
            new("Термин", "Событие, которое точно произойдет, называют", AnswerType.Text, "достоверное", 1, "Вероятность такого события равна 1.", DifficultyLevel.Advanced));

        await EnsureAdditionalTopicTestsAsync(adminId, cancellationToken);
    }

    private async Task EnsureAdditionalTopicTestsAsync(int adminId, CancellationToken cancellationToken)
    {
        await EnsureTestAsync(adminId, "Тест: линейные уравнения — закрепление", DifficultyLevel.Beginner, "Дополнительный тест по линейным уравнениям.", "Решайте уравнения последовательно: раскрытие скобок, перенос слагаемых, деление на коэффициент.", cancellationToken,
            new("Уравнение 1", "x - 9 = 4", AnswerType.Number, "13", 1, "x = 13.", DifficultyLevel.Beginner),
            new("Уравнение 2", "6x = 42", AnswerType.Number, "7", 1, "42 / 6 = 7.", DifficultyLevel.Beginner),
            new("Уравнение 3", "3x + 12 = 0", AnswerType.Number, "-4", 1, "3x = -12.", DifficultyLevel.Beginner),
            new("Уравнение 4", "2(x - 1) = 10", AnswerType.Number, "6", 1, "x - 1 = 5.", DifficultyLevel.Beginner),
            new("Выбор корня", "Корень уравнения x + 5 = 8", AnswerType.SingleChoice, "3", 1, "8 - 5 = 3.", DifficultyLevel.Beginner, "2", "3", "4", "13"));

        await EnsureTestAsync(adminId, "Тест: проценты и доли — задачи", DifficultyLevel.Beginner, "Сюжетные задачи на проценты.", "Переводите проценты в десятичную дробь и внимательно читайте условие.", cancellationToken,
            new("Скидка", "20% от 800", AnswerType.Number, "160", 1, "0.2 * 800.", DifficultyLevel.Beginner),
            new("После скидки", "Цена 600 уменьшилась на 25%. Новая цена", AnswerType.Number, "450", 1, "600 - 150.", DifficultyLevel.Beginner),
            new("Доля класса", "5 учеников из 20 это сколько процентов?", AnswerType.Number, "25", 1, "5 / 20 * 100.", DifficultyLevel.Beginner),
            new("Половина", "50% числа 90", AnswerType.Number, "45", 1, "Половина от 90.", DifficultyLevel.Beginner),
            new("Записи", "Выберите записи 25%", AnswerType.MultipleChoice, "0.25;1/4", 2, "25% = 0.25 = 1/4.", DifficultyLevel.Beginner, "0.25", "1/4", "2.5", "25"));

        await EnsureTestAsync(adminId, "Тест: квадратные уравнения — корни", DifficultyLevel.Intermediate, "Дополнительная практика на дискриминант и корни.", "Сначала найдите D, затем примените формулу корней.", cancellationToken,
            new("D", "D для x^2 - 4x + 4 = 0", AnswerType.Number, "0", 1, "16 - 16 = 0.", DifficultyLevel.Intermediate),
            new("Корень", "Корень x^2 - 4x + 4 = 0", AnswerType.Number, "2", 1, "Единственный корень 2.", DifficultyLevel.Intermediate),
            new("Корни", "Выберите корни x^2 - 1 = 0", AnswerType.MultipleChoice, "-1;1", 2, "Корни -1 и 1.", DifficultyLevel.Intermediate, "-1", "1", "0", "2"),
            new("Количество", "Если D > 0, корней", AnswerType.SingleChoice, "два", 1, "При положительном D два действительных корня.", DifficultyLevel.Intermediate, "нет", "один", "два", "бесконечно"),
            new("Свободный член", "В x^2 + 2x - 8 = 0 свободный член", AnswerType.Number, "-8", 1, "Это c.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: функции и графики — практика", DifficultyLevel.Intermediate, "Практические вопросы по линейным функциям.", "Обращайте внимание на коэффициенты k и b.", cancellationToken,
            new("f(2)", "f(x)=3x-1. f(2)", AnswerType.Number, "5", 1, "6 - 1.", DifficultyLevel.Beginner),
            new("Нуль", "Нуль y = 2x - 10", AnswerType.Number, "5", 1, "2x = 10.", DifficultyLevel.Intermediate),
            new("k", "В y = -5x + 1 коэффициент k", AnswerType.Number, "-5", 1, "k перед x.", DifficultyLevel.Beginner),
            new("b", "В y = x + 12 свободный член", AnswerType.Number, "12", 1, "b = 12.", DifficultyLevel.Beginner),
            new("График", "График линейной функции", AnswerType.SingleChoice, "прямая", 1, "Линейная функция задает прямую.", DifficultyLevel.Beginner, "прямая", "парабола", "точка", "окружность"));
    }

    private async Task EnsureExpandedCurriculumAsync(int adminId, CancellationToken cancellationToken)
    {
        await EnsureLinearEquationsPackAsync(adminId, cancellationToken);
        await EnsureQuadraticEquationsPackAsync(adminId, cancellationToken);
        await EnsurePercentsPackAsync(adminId, cancellationToken);
        await EnsureFunctionsPackAsync(adminId, cancellationToken);
        await EnsureTrianglesPackAsync(adminId, cancellationToken);
        await EnsurePowersPackAsync(adminId, cancellationToken);
        await EnsureSystemsPackAsync(adminId, cancellationToken);
        await EnsureStatisticsPackAsync(adminId, cancellationToken);
    }

    private async Task EnsureLinearEquationsPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "линейные уравнения";
        await EnsureMaterialAsync(adminId, "Теория: линейные уравнения от простого к сложному", topic, MaterialType.Theory, DifficultyLevel.Beginner,
            "Большой конспект: что такое линейное уравнение, как переносить слагаемые и проверять корень.",
            """
            Линейное уравнение - это уравнение, которое после упрощения можно привести к виду ax + b = 0.

            1. Что нужно помнить
            - Корень уравнения - значение переменной, при котором равенство становится верным.
            - С обеими частями уравнения можно выполнять одно и то же действие: прибавлять, вычитать, умножать или делить на ненулевое число.
            - Главная цель - оставить переменную в одной части, а числа перенести в другую.

            2. Алгоритм решения
            1) Раскройте скобки.
            2) Приведите подобные слагаемые.
            3) Перенесите все слагаемые с x в одну сторону.
            4) Перенесите свободные числа в другую сторону.
            5) Разделите обе части на коэффициент при x.
            6) Проверьте ответ подстановкой.

            3. Пример
            3x + 8 = 20
            3x = 20 - 8
            3x = 12
            x = 4
            Проверка: 3 * 4 + 8 = 20.

            4. Частые ошибки
            - При переносе слагаемого забывают менять знак.
            - Делят только одну часть уравнения.
            - Теряют минус перед скобкой.
            - Не проверяют найденный корень.

            Мини-чеклист: упростил? перенёс? поделил? проверил?
            """, true, cancellationToken, "Основы");

        await EnsureMaterialAsync(adminId, "Теория: линейные уравнения со скобками и дробями", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Как работать со скобками, отрицательными коэффициентами и дробными множителями.",
            """
            Уравнения со скобками и дробями решаются теми же правилами, но требуют аккуратности.

            Скобки:
            - Если перед скобкой стоит плюс, знаки внутри не меняются.
            - Если перед скобкой стоит минус, все знаки внутри меняются.
            - Если перед скобкой стоит число, умножьте его на каждое слагаемое внутри.

            Пример со скобками:
            2(x - 3) + 5 = 11
            2x - 6 + 5 = 11
            2x - 1 = 11
            2x = 12
            x = 6.

            Дроби:
            Чтобы избавиться от дробей, умножьте обе части уравнения на общий знаменатель.
            Пример:
            x/3 + 2 = 5
            x/3 = 3
            x = 9.

            Если коэффициент отрицательный:
            -4x = 20
            x = -5.

            Совет: после раскрытия скобок всегда перепишите уравнение заново. Это снижает количество ошибок.
            """, true, cancellationToken, "Скобки и дроби");

        await EnsurePracticeMaterialAsync(adminId, "Практика: линейные уравнения", topic, DifficultyLevel.Beginner,
            "Тренажёр на перенос слагаемых, раскрытие скобок и проверку корней.",
            "Решайте задания по порядку: от простых уравнений к уравнениям со скобками.",
            cancellationToken,
            new("Простое уравнение", "x + 14 = 31", AnswerType.Number, "17", 1, "31 - 14 = 17.", DifficultyLevel.Beginner),
            new("Отрицательный ответ", "5x + 10 = 0", AnswerType.Number, "-2", 1, "5x = -10.", DifficultyLevel.Beginner),
            new("Скобки", "3(x + 2) = 21", AnswerType.Number, "5", 1, "x + 2 = 7.", DifficultyLevel.Beginner),
            new("Дробь", "x/4 = 6", AnswerType.Number, "24", 1, "Умножаем на 4.", DifficultyLevel.Beginner),
            new("Проверка", "Подходит ли x = -1 для 2x + 2 = 0?", AnswerType.Text, "да", 1, "2 * (-1) + 2 = 0.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: линейные уравнения — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест по линейным уравнениям: простые случаи, скобки, дроби и проверка решений.",
            "Перед тестом повторите перенос слагаемых и правило раскрытия скобок.", cancellationToken,
            new("Корень 1", "x + 18 = 45", AnswerType.Number, "27", 1, "45 - 18 = 27.", DifficultyLevel.Beginner),
            new("Корень 2", "9x = 72", AnswerType.Number, "8", 1, "72 / 9 = 8.", DifficultyLevel.Beginner),
            new("Корень 3", "7x - 14 = 0", AnswerType.Number, "2", 1, "7x = 14.", DifficultyLevel.Beginner),
            new("Скобки", "4(x - 2) = 20", AnswerType.Number, "7", 1, "x - 2 = 5.", DifficultyLevel.Beginner),
            new("Дробь", "x/5 + 3 = 9", AnswerType.Number, "30", 2, "x/5 = 6.", DifficultyLevel.Intermediate),
            new("Минус перед скобкой", "-(x - 4) = 10", AnswerType.Number, "-6", 2, "-x + 4 = 10.", DifficultyLevel.Intermediate),
            new("Выбор корня", "Корень уравнения 2x - 6 = 10", AnswerType.SingleChoice, "8", 1, "2x = 16.", DifficultyLevel.Beginner, "2", "4", "8", "16"),
            new("Верные шаги", "Выберите верные шаги для 5x + 15 = 40", AnswerType.MultipleChoice, "5x = 25;x = 5", 2, "Переносим 15 и делим на 5.", DifficultyLevel.Beginner, "5x = 25", "x = 5", "x = 11", "5x = 55"),
            new("Проверка", "Подходит ли x = 3 для 4x - 1 = 11?", AnswerType.Text, "да", 1, "12 - 1 = 11.", DifficultyLevel.Beginner),
            new("Коэффициент", "В уравнении -6x + 9 = 0 коэффициент при x равен", AnswerType.Number, "-6", 1, "Коэффициент стоит перед переменной.", DifficultyLevel.Beginner));
    }

    private async Task EnsureQuadraticEquationsPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "квадратные уравнения";
        await EnsureMaterialAsync(adminId, "Теория: квадратные уравнения и дискриминант", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Подробный разбор квадратных уравнений, дискриминанта и количества корней.",
            """
            Квадратное уравнение имеет вид ax^2 + bx + c = 0, где a не равно 0.

            Дискриминант:
            D = b^2 - 4ac.

            Что показывает D:
            - D > 0: два действительных корня.
            - D = 0: один действительный корень.
            - D < 0: действительных корней нет.

            Формула корней:
            x1 = (-b - sqrt(D)) / 2a
            x2 = (-b + sqrt(D)) / 2a

            Пример:
            x^2 - 5x + 6 = 0
            a = 1, b = -5, c = 6
            D = 25 - 24 = 1
            x1 = (5 - 1) / 2 = 2
            x2 = (5 + 1) / 2 = 3.

            Важно: после нахождения корней полезно проверить их подстановкой.
            """, true, cancellationToken, "Дискриминант");

        await EnsureMaterialAsync(adminId, "Теория: неполные квадратные уравнения", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Как решать уравнения без b или c быстрее, чем через дискриминант.",
            """
            Неполные квадратные уравнения можно решать короткими способами.

            1. ax^2 + c = 0
            Перенесите c и разделите на a:
            ax^2 = -c
            x^2 = -c/a.
            Если справа отрицательное число, действительных корней нет.

            Пример:
            x^2 - 25 = 0
            x^2 = 25
            x = -5 или x = 5.

            2. ax^2 + bx = 0
            Вынесите x за скобку:
            x(ax + b) = 0.
            Тогда x = 0 или ax + b = 0.

            Пример:
            x^2 - 7x = 0
            x(x - 7) = 0
            x = 0 или x = 7.

            3. Полный квадрат
            x^2 + 6x + 9 = 0
            (x + 3)^2 = 0
            x = -3.
            """, true, cancellationToken, "Неполные уравнения");

        await EnsurePracticeMaterialAsync(adminId, "Практика: квадратные уравнения", topic, DifficultyLevel.Intermediate,
            "Тренажёр по дискриминанту, неполным уравнениям и проверке корней.",
            "Сначала определите тип уравнения: полное, неполное или разность квадратов.",
            cancellationToken,
            new("Дискриминант", "D для x^2 - 8x + 12 = 0", AnswerType.Number, "16", 1, "64 - 48 = 16.", DifficultyLevel.Intermediate),
            new("Корень", "Меньший корень x^2 - 8x + 12 = 0", AnswerType.Number, "2", 1, "Корни 2 и 6.", DifficultyLevel.Intermediate),
            new("Разность квадратов", "Положительный корень x^2 - 64 = 0", AnswerType.Number, "8", 1, "x = +-8.", DifficultyLevel.Beginner),
            new("Вынесение x", "Один корень x^2 - 9x = 0", AnswerType.SingleChoice, "0", 1, "x(x - 9)=0.", DifficultyLevel.Intermediate, "0", "1", "-9", "18"),
            new("Количество корней", "Если D = 0, сколько корней?", AnswerType.Number, "1", 1, "Один действительный корень.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: квадратные уравнения — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест по дискриминанту, корням, неполным уравнениям и проверке.",
            "Записывайте a, b, c отдельно: это снижает риск ошибки в знаках.", cancellationToken,
            new("D 1", "D для x^2 - 7x + 10 = 0", AnswerType.Number, "9", 1, "49 - 40 = 9.", DifficultyLevel.Intermediate),
            new("D 2", "D для 2x^2 - 4x + 2 = 0", AnswerType.Number, "0", 1, "16 - 16 = 0.", DifficultyLevel.Intermediate),
            new("Количество", "Сколько действительных корней при D < 0?", AnswerType.Number, "0", 1, "Действительных корней нет.", DifficultyLevel.Beginner),
            new("Корень", "Один корень x^2 - 10x + 21 = 0", AnswerType.SingleChoice, "3", 1, "Корни 3 и 7.", DifficultyLevel.Intermediate, "3", "5", "10", "21"),
            new("Корни", "Выберите корни x^2 - 16 = 0", AnswerType.MultipleChoice, "-4;4", 2, "x = +-4.", DifficultyLevel.Beginner, "-4", "4", "8", "16"),
            new("Неполное", "Положительный корень x^2 = 121", AnswerType.Number, "11", 1, "sqrt(121)=11.", DifficultyLevel.Beginner),
            new("Вынесение", "Корни x^2 - 5x = 0", AnswerType.MultipleChoice, "0;5", 2, "x(x-5)=0.", DifficultyLevel.Intermediate, "0", "5", "-5", "1"),
            new("Формула", "Дискриминант равен", AnswerType.SingleChoice, "b^2 - 4ac", 1, "Стандартная формула.", DifficultyLevel.Beginner, "b^2 - 4ac", "a^2 - 4bc", "2a+b", "b-4ac"),
            new("Проверка", "Является ли 1 корнем x^2 - 1 = 0?", AnswerType.Text, "да", 1, "1 - 1 = 0.", DifficultyLevel.Beginner),
            new("Коэффициент c", "В 3x^2 - 2x - 5 = 0 коэффициент c равен", AnswerType.Number, "-5", 1, "Свободный член.", DifficultyLevel.Beginner));
    }

    private async Task EnsurePercentsPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "проценты и доли";
        await EnsureMaterialAsync(adminId, "Теория: проценты, доли и отношения", topic, MaterialType.Theory, DifficultyLevel.Beginner,
            "Большой конспект по процентам: перевод, часть от числа, число по проценту и процентное отношение.",
            """
            Процент - это сотая часть числа. 1% = 1/100 = 0.01.

            Основные задачи:
            1. Найти процент от числа:
            p% от a = a * p / 100.
            Пример: 15% от 200 = 200 * 15 / 100 = 30.

            2. Найти число по его проценту:
            Если 20% числа равны 50, то всё число равно 50 / 0.2 = 250.

            3. Найти, сколько процентов составляет часть:
            часть / целое * 100%.
            Пример: 12 из 60 = 12 / 60 * 100 = 20%.

            4. Скидка:
            Новая цена = старая цена * (1 - p/100).

            5. Увеличение:
            Новое значение = старое значение * (1 + p/100).

            Полезные доли:
            50% = 1/2, 25% = 1/4, 20% = 1/5, 10% = 1/10, 75% = 3/4.
            """, true, cancellationToken, "Основы");

        await EnsureMaterialAsync(adminId, "Теория: проценты в жизненных задачах", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Скидки, наценки, банковские проценты, смеси и сравнение величин.",
            """
            В задачах на проценты важно понять, что считается за 100%.

            Скидка и наценка:
            - Скидка 20% означает, что покупатель платит 80% исходной цены.
            - Наценка 20% означает, что новая цена равна 120% исходной.

            Последовательные изменения:
            Если цена сначала выросла на 10%, а потом упала на 10%, она не вернётся к исходной.
            Пример: 100 -> 110 -> 99.

            Процентный пункт:
            Рост с 20% до 25% - это рост на 5 процентных пунктов, но относительно 20% это рост на 25%.

            Как решать текстовые задачи:
            1) Выпишите, что является целым.
            2) Переведите проценты в десятичные дроби.
            3) Составьте выражение.
            4) Проверьте смысл ответа: скидка не может быть больше цены, вероятность не может быть больше 100%.
            """, true, cancellationToken, "Практические задачи");

        await EnsurePracticeMaterialAsync(adminId, "Практика: проценты и доли", topic, DifficultyLevel.Beginner,
            "Тренажёр на проценты от числа, скидки, прирост и процентное отношение.",
            "Переводите процент в дробь: 30% = 0.30.",
            cancellationToken,
            new("10 процентов", "10% от 340", AnswerType.Number, "34", 1, "340 / 10.", DifficultyLevel.Beginner),
            new("25 процентов", "25% от 120", AnswerType.Number, "30", 1, "Четверть от 120.", DifficultyLevel.Beginner),
            new("Скидка", "Скидка 30% от 900", AnswerType.Number, "270", 1, "0.3 * 900.", DifficultyLevel.Beginner),
            new("Новая цена", "Цена 700 выросла на 10%. Новая цена", AnswerType.Number, "770", 1, "700 + 70.", DifficultyLevel.Beginner),
            new("Доля", "18 из 60 это сколько процентов?", AnswerType.Number, "30", 1, "18/60*100.", DifficultyLevel.Intermediate));

        await EnsureTestAsync(adminId, "Тест: проценты и доли — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест на вычисления, скидки, прирост, доли и процентное отношение.",
            "Не забывайте: p% = p / 100.", cancellationToken,
            new("Процент", "35% от 200", AnswerType.Number, "70", 1, "0.35 * 200.", DifficultyLevel.Beginner),
            new("Скидка", "Скидка 12% от 500", AnswerType.Number, "60", 1, "0.12 * 500.", DifficultyLevel.Beginner),
            new("После скидки", "Цена 1000 уменьшилась на 18%. Новая цена", AnswerType.Number, "820", 2, "1000 - 180.", DifficultyLevel.Intermediate),
            new("После роста", "Число 80 увеличили на 15%. Результат", AnswerType.Number, "92", 2, "80 + 12.", DifficultyLevel.Intermediate),
            new("Процентное отношение", "9 от 45 это сколько процентов?", AnswerType.Number, "20", 1, "9/45*100.", DifficultyLevel.Beginner),
            new("Дробь", "75% это", AnswerType.SingleChoice, "3/4", 1, "75/100=3/4.", DifficultyLevel.Beginner, "1/2", "1/4", "3/4", "1/5"),
            new("Верные записи", "Выберите записи 20%", AnswerType.MultipleChoice, "0.2;1/5", 2, "20% = 0.2 = 1/5.", DifficultyLevel.Beginner, "0.2", "1/5", "2", "20"),
            new("Число по проценту", "30% числа равны 90. Число равно", AnswerType.Number, "300", 2, "90 / 0.3.", DifficultyLevel.Intermediate),
            new("Сравнение", "Если часть равна целому, это сколько процентов?", AnswerType.Number, "100", 1, "Целое составляет 100%.", DifficultyLevel.Beginner),
            new("Понятие", "1% - это какая часть числа?", AnswerType.Text, "сотая", 1, "Один процент - одна сотая.", DifficultyLevel.Beginner));
    }

    private async Task EnsureFunctionsPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "функции и графики";
        await EnsureMaterialAsync(adminId, "Теория: функции и способы задания", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Что такое функция, область определения, область значений, таблица и график.",
            """
            Функция - это правило, по которому каждому допустимому значению x соответствует ровно одно значение y.

            Основные понятия:
            - Область определения - все значения x, которые можно подставлять.
            - Область значений - все значения y, которые функция может принимать.
            - График функции - множество точек (x; y) на координатной плоскости.

            Способы задания функции:
            1) Формулой: y = 2x + 3.
            2) Таблицей значений.
            3) Графиком.
            4) Описанием словами.

            Линейная функция:
            y = kx + b.
            k отвечает за наклон прямой.
            b показывает, где график пересекает ось y.

            Если k > 0, функция возрастает.
            Если k < 0, функция убывает.
            Если k = 0, функция постоянна.
            """, true, cancellationToken, "Основы");

        await EnsureMaterialAsync(adminId, "Теория: чтение графиков", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Как по графику находить значения функции, нули, промежутки возрастания и убывания.",
            """
            Чтобы читать график, двигайтесь от оси x к графику, а затем к оси y.

            Как найти значение функции:
            1) Найдите нужное значение x на горизонтальной оси.
            2) Проведите мысленно вертикальную линию до графика.
            3) Посмотрите соответствующее значение y.

            Нуль функции - точка, где график пересекает ось x.
            На графике это значит y = 0.

            Промежутки возрастания:
            Если при движении вправо график поднимается, функция возрастает.

            Промежутки убывания:
            Если при движении вправо график опускается, функция убывает.

            Типичные ошибки:
            - Путают оси x и y.
            - Считывают значение не в той точке.
            - Считают, что любая кривая является функцией. Если одному x соответствует два y, это не функция.
            """, true, cancellationToken, "Графики");

        await EnsurePracticeMaterialAsync(adminId, "Практика: функции и графики", topic, DifficultyLevel.Intermediate,
            "Тренажёр по значениям функций, коэффициентам и нулям линейной функции.",
            "Для y = kx + b сначала подставьте x, затем выполните вычисления.",
            cancellationToken,
            new("Значение", "f(x)=4x-3. Найдите f(2)", AnswerType.Number, "5", 1, "8 - 3 = 5.", DifficultyLevel.Beginner),
            new("Нуль", "Нуль функции y = x + 9", AnswerType.Number, "-9", 1, "x + 9 = 0.", DifficultyLevel.Intermediate),
            new("Коэффициент k", "В y = -7x + 2 коэффициент k", AnswerType.Number, "-7", 1, "k стоит перед x.", DifficultyLevel.Beginner),
            new("Свободный член", "В y = 3x - 11 свободный член", AnswerType.Number, "-11", 1, "Это b.", DifficultyLevel.Beginner),
            new("Тип графика", "График y = 2x + 1", AnswerType.SingleChoice, "прямая", 1, "Это линейная функция.", DifficultyLevel.Beginner, "прямая", "парабола", "окружность", "ломаная всегда"));

        await EnsureTestAsync(adminId, "Тест: функции и графики — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест по функциям: значения, нули, коэффициенты, графики и свойства.",
            "Повторите смысл k и b в формуле y = kx + b.", cancellationToken,
            new("f(4)", "f(x)=2x-5. f(4)", AnswerType.Number, "3", 1, "8 - 5 = 3.", DifficultyLevel.Beginner),
            new("f(-2)", "f(x)=-3x+1. f(-2)", AnswerType.Number, "7", 1, "6 + 1 = 7.", DifficultyLevel.Intermediate),
            new("Нуль", "Нуль функции y = x - 12", AnswerType.Number, "12", 1, "x - 12 = 0.", DifficultyLevel.Beginner),
            new("Нуль 2", "Нуль функции y = 2x + 8", AnswerType.Number, "-4", 2, "2x = -8.", DifficultyLevel.Intermediate),
            new("k", "В y = 5x - 2 коэффициент k равен", AnswerType.Number, "5", 1, "Коэффициент при x.", DifficultyLevel.Beginner),
            new("b", "В y = -x + 6 свободный член равен", AnswerType.Number, "6", 1, "b = 6.", DifficultyLevel.Beginner),
            new("График", "Линейная функция задаёт график", AnswerType.SingleChoice, "прямая", 1, "График y=kx+b - прямая.", DifficultyLevel.Beginner, "прямая", "парабола", "гипербола", "отрезок всегда"),
            new("Возрастание", "Если k > 0, линейная функция", AnswerType.SingleChoice, "возрастает", 1, "Положительный наклон.", DifficultyLevel.Intermediate, "возрастает", "убывает", "не определена", "всегда равна нулю"),
            new("Свойства", "Выберите верные утверждения", AnswerType.MultipleChoice, "у функции есть область определения;каждому x соответствует одно y", 2, "Это базовое определение функции.", DifficultyLevel.Intermediate, "у функции есть область определения", "каждому x соответствует одно y", "каждому x соответствует много y", "функция не имеет значений"),
            new("Термин", "Значение x, при котором y=0, называется ... функции", AnswerType.Text, "нуль", 1, "Это нуль функции.", DifficultyLevel.Intermediate));
    }

    private async Task EnsureTrianglesPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "геометрия треугольников";
        await EnsureMaterialAsync(adminId, "Теория: треугольники и их элементы", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Стороны, углы, медианы, биссектрисы, высоты и базовые свойства треугольников.",
            """
            Треугольник - фигура из трёх точек, не лежащих на одной прямой, и трёх отрезков между ними.

            Основные элементы:
            - Стороны.
            - Вершины.
            - Углы.
            - Высота - перпендикуляр из вершины к противоположной стороне.
            - Медиана - отрезок от вершины к середине противоположной стороны.
            - Биссектриса - отрезок, который делит угол пополам.

            Главное свойство:
            Сумма углов любого треугольника равна 180 градусов.

            Виды треугольников по сторонам:
            - Разносторонний.
            - Равнобедренный.
            - Равносторонний.

            Виды по углам:
            - Остроугольный.
            - Прямоугольный.
            - Тупоугольный.
            """, true, cancellationToken, "Основы");

        await EnsureMaterialAsync(adminId, "Теория: площадь и периметр треугольника", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Формулы площади, периметра и базовые задачи на прямоугольный треугольник.",
            """
            Периметр треугольника:
            P = a + b + c.

            Площадь через основание и высоту:
            S = a * h / 2.
            Здесь a - основание, h - высота к этому основанию.

            Пример:
            Основание 12, высота 5.
            S = 12 * 5 / 2 = 30.

            Прямоугольный треугольник:
            Сторона напротив прямого угла называется гипотенузой.
            Две стороны, образующие прямой угол, называются катетами.

            Теорема Пифагора:
            a^2 + b^2 = c^2, где c - гипотенуза.

            Частая тройка:
            3, 4, 5 - прямоугольный треугольник, потому что 3^2 + 4^2 = 5^2.
            """, true, cancellationToken, "Площадь и периметр");

        await EnsurePracticeMaterialAsync(adminId, "Практика: треугольники", topic, DifficultyLevel.Intermediate,
            "Тренажёр на углы, площадь, периметр и прямоугольные треугольники.",
            "Сначала определите, какая формула подходит: сумма углов, площадь или периметр.",
            cancellationToken,
            new("Третий угол", "Углы 40 и 80. Третий угол", AnswerType.Number, "60", 1, "180 - 120.", DifficultyLevel.Beginner),
            new("Площадь", "Основание 8, высота 5. Площадь", AnswerType.Number, "20", 1, "8*5/2.", DifficultyLevel.Intermediate),
            new("Периметр", "Стороны 6, 7, 8. Периметр", AnswerType.Number, "21", 1, "6+7+8.", DifficultyLevel.Beginner),
            new("Гипотенуза", "В треугольнике 5, 12, 13 гипотенуза", AnswerType.Number, "13", 1, "Самая длинная сторона.", DifficultyLevel.Intermediate),
            new("Равнобедренный", "В равнобедренном треугольнике равны", AnswerType.SingleChoice, "две стороны", 1, "По определению.", DifficultyLevel.Beginner, "две стороны", "три высоты только", "все углы разные", "периметр и площадь"));

        await EnsureTestAsync(adminId, "Тест: геометрия треугольников — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест по углам, площади, периметру, видам треугольников и Пифагору.",
            "Помните: сумма углов треугольника всегда 180 градусов.", cancellationToken,
            new("Сумма углов", "Сумма углов треугольника", AnswerType.Number, "180", 1, "Базовое свойство.", DifficultyLevel.Beginner),
            new("Угол", "Углы 35 и 65. Третий угол", AnswerType.Number, "80", 1, "180 - 100.", DifficultyLevel.Beginner),
            new("Площадь", "Основание 14, высота 6. Площадь", AnswerType.Number, "42", 1, "14*6/2.", DifficultyLevel.Intermediate),
            new("Периметр", "Стороны 9, 10, 11. Периметр", AnswerType.Number, "30", 1, "Сумма сторон.", DifficultyLevel.Beginner),
            new("Прямой угол", "Прямоугольный треугольник имеет угол", AnswerType.Number, "90", 1, "Прямой угол.", DifficultyLevel.Beginner),
            new("Гипотенуза", "Самая длинная сторона прямоугольного треугольника", AnswerType.Text, "гипотенуза", 1, "Она напротив прямого угла.", DifficultyLevel.Beginner),
            new("Вид", "Треугольник с двумя равными сторонами", AnswerType.SingleChoice, "равнобедренный", 1, "Две равные стороны.", DifficultyLevel.Beginner, "равнобедренный", "разносторонний", "тупоугольный", "прямой"),
            new("Формулы", "Выберите верные формулы", AnswerType.MultipleChoice, "P=a+b+c;S=a*h/2", 2, "Периметр и площадь.", DifficultyLevel.Intermediate, "P=a+b+c", "S=a*h/2", "S=a+h", "P=a*b*c"),
            new("Пифагор", "Катеты 6 и 8. Гипотенуза", AnswerType.Number, "10", 2, "36+64=100.", DifficultyLevel.Intermediate),
            new("Равносторонний", "В равностороннем треугольнике каждый угол", AnswerType.Number, "60", 1, "180/3.", DifficultyLevel.Intermediate));
    }

    private async Task EnsurePowersPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "степени и корни";
        await EnsureMaterialAsync(adminId, "Теория: степени и свойства степеней", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Правила умножения, деления, степени степени, нулевой и отрицательной степени.",
            """
            Степень a^n показывает произведение n одинаковых множителей a.

            Основные правила:
            - a^m * a^n = a^(m+n)
            - a^m / a^n = a^(m-n), если a не равно 0
            - (a^m)^n = a^(m*n)
            - (ab)^n = a^n * b^n
            - a^0 = 1, если a не равно 0
            - a^(-n) = 1 / a^n

            Примеры:
            2^3 * 2^4 = 2^7 = 128.
            5^0 = 1.
            10^-1 = 1/10 = 0.1.

            Частая ошибка: при умножении степеней показатели складываются, а не перемножаются.
            """, true, cancellationToken, "Степени");

        await EnsureMaterialAsync(adminId, "Теория: квадратные корни", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Что такое квадратный корень, арифметический корень и как упрощать выражения.",
            """
            Квадратный корень из числа a - такое неотрицательное число, квадрат которого равен a.

            sqrt(49) = 7, потому что 7^2 = 49.
            sqrt(0) = 0.
            Корень из отрицательного числа в действительных числах не определён.

            Важные свойства:
            sqrt(ab) = sqrt(a) * sqrt(b), если a и b неотрицательны.
            sqrt(a^2) = |a|.

            Примеры:
            sqrt(36) = 6.
            sqrt(100) = 10.
            sqrt(9 * 16) = 3 * 4 = 12.

            При решении уравнений x^2 = 25 не забывайте два корня: x = -5 и x = 5.
            """, true, cancellationToken, "Корни");

        await EnsurePracticeMaterialAsync(adminId, "Практика: степени и корни", topic, DifficultyLevel.Intermediate,
            "Тренажёр на свойства степеней и квадратные корни.",
            "Следите, что происходит с показателями: складываются, вычитаются или умножаются.",
            cancellationToken,
            new("Степень", "3^3", AnswerType.Number, "27", 1, "3*3*3.", DifficultyLevel.Beginner),
            new("Корень", "sqrt(144)", AnswerType.Number, "12", 1, "12^2=144.", DifficultyLevel.Beginner),
            new("Нулевая степень", "15^0", AnswerType.Number, "1", 1, "Любое ненулевое число в нулевой степени равно 1.", DifficultyLevel.Intermediate),
            new("Умножение", "a^4 * a^2", AnswerType.SingleChoice, "a^6", 1, "Показатели складываются.", DifficultyLevel.Intermediate, "a^8", "a^6", "a^2", "2a^6"),
            new("Верные равенства", "Выберите верные", AnswerType.MultipleChoice, "sqrt(25)=5;2^4=16", 2, "Оба равенства верны.", DifficultyLevel.Beginner, "sqrt(25)=5", "2^4=16", "sqrt(36)=8", "3^2=6"));

        await EnsureTestAsync(adminId, "Тест: степени и корни — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест по степеням, корням и основным свойствам.",
            "Повторите правила действий со степенями с одинаковым основанием.", cancellationToken,
            new("Квадрат", "12^2", AnswerType.Number, "144", 1, "12*12.", DifficultyLevel.Beginner),
            new("Куб", "4^3", AnswerType.Number, "64", 1, "4*4*4.", DifficultyLevel.Beginner),
            new("Корень", "sqrt(169)", AnswerType.Number, "13", 1, "13^2=169.", DifficultyLevel.Beginner),
            new("Нулевая", "(-3)^0", AnswerType.Number, "1", 1, "Ненулевое число в нулевой степени равно 1.", DifficultyLevel.Intermediate),
            new("Отрицательная", "10^-2", AnswerType.SingleChoice, "0.01", 2, "1/100.", DifficultyLevel.Advanced, "100", "0.01", "-100", "10"),
            new("Умножение", "x^3 * x^5", AnswerType.SingleChoice, "x^8", 1, "3+5=8.", DifficultyLevel.Intermediate, "x^15", "x^8", "x^2", "2x^8"),
            new("Деление", "a^7 / a^2", AnswerType.SingleChoice, "a^5", 1, "7-2=5.", DifficultyLevel.Intermediate, "a^9", "a^14", "a^5", "a^3"),
            new("Верные", "Выберите верные равенства", AnswerType.MultipleChoice, "sqrt(81)=9;5^2=25", 2, "Оба верны.", DifficultyLevel.Beginner, "sqrt(81)=9", "5^2=25", "sqrt(64)=6", "2^5=10"),
            new("Подкоренное", "Число под знаком корня называется", AnswerType.Text, "подкоренное", 1, "Подкоренное число или выражение.", DifficultyLevel.Intermediate),
            new("Уравнение", "Положительный корень x^2 = 36", AnswerType.Number, "6", 1, "sqrt(36)=6.", DifficultyLevel.Beginner));
    }

    private async Task EnsureSystemsPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "системы уравнений";
        await EnsureMaterialAsync(adminId, "Теория: системы уравнений", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Метод подстановки, метод сложения и проверка пары решений.",
            """
            Система уравнений - несколько уравнений, которые должны выполняться одновременно.

            Решение системы с двумя переменными - пара чисел (x; y), которая подходит ко всем уравнениям.

            Метод подстановки:
            1) Выразите одну переменную через другую.
            2) Подставьте выражение во второе уравнение.
            3) Найдите одну переменную.
            4) Подставьте её и найдите вторую.
            5) Проверьте пару в обоих уравнениях.

            Метод сложения:
            1) Подберите множители, чтобы коэффициенты при одной переменной стали противоположными.
            2) Сложите уравнения.
            3) Найдите одну переменную.
            4) Найдите вторую.

            Пример:
            x + y = 7
            x - y = 1
            Складываем: 2x = 8, x = 4.
            Тогда y = 3.
            Ответ: (4; 3).
            """, true, cancellationToken, "Основы");

        await EnsureMaterialAsync(adminId, "Теория: выбор метода решения системы", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Как понять, когда удобнее подстановка, а когда сложение.",
            """
            Метод подстановки удобен, если одна переменная уже выражена или легко выражается.
            Пример:
            x = y + 2
            x + y = 10.

            Метод сложения удобен, если коэффициенты можно быстро уничтожить.
            Пример:
            2x + y = 9
            3x - y = 6.
            При сложении y исчезает.

            Проверка обязательна:
            Даже если вычисления выглядят правильными, пара должна подходить к каждому уравнению.

            Возможные случаи:
            - Одно решение: прямые пересекаются.
            - Нет решений: прямые параллельны.
            - Бесконечно много решений: уравнения задают одну и ту же прямую.
            """, true, cancellationToken, "Методы");

        await EnsurePracticeMaterialAsync(adminId, "Практика: системы уравнений", topic, DifficultyLevel.Intermediate,
            "Тренажёр на подстановку, сложение и проверку решения системы.",
            "Записывайте ответ как отдельные значения x и y, затем проверяйте оба уравнения.",
            cancellationToken,
            new("Подстановка", "x + y = 9, x = 4. Найдите y", AnswerType.Number, "5", 1, "4 + y = 9.", DifficultyLevel.Beginner),
            new("Сложение x", "x + y = 8, x - y = 2. x =", AnswerType.Number, "5", 2, "2x=10.", DifficultyLevel.Intermediate),
            new("Сложение y", "x + y = 8, x - y = 2. y =", AnswerType.Number, "3", 2, "5+y=8.", DifficultyLevel.Intermediate),
            new("Метод", "Если переменная уже выражена, удобно использовать метод", AnswerType.SingleChoice, "подстановки", 1, "Подставляем выражение.", DifficultyLevel.Beginner, "подстановки", "Пифагора", "процентов", "корней"),
            new("Проверка", "Пара (2;3) подходит к x+y=5?", AnswerType.Text, "да", 1, "2+3=5.", DifficultyLevel.Beginner));

        await EnsureTestAsync(adminId, "Тест: системы уравнений — большой зачёт", DifficultyLevel.Intermediate,
            "Итоговый тест по системам: подстановка, сложение, проверка и смысл решения.",
            "Решение системы - пара, подходящая ко всем уравнениям.", cancellationToken,
            new("y", "x + y = 11, x = 6. y =", AnswerType.Number, "5", 1, "11-6=5.", DifficultyLevel.Beginner),
            new("x", "x + y = 13, y = 4. x =", AnswerType.Number, "9", 1, "13-4=9.", DifficultyLevel.Beginner),
            new("Сложение", "x + y = 9, x - y = 3. x =", AnswerType.Number, "6", 2, "2x=12.", DifficultyLevel.Intermediate),
            new("Сложение 2", "x + y = 9, x - y = 3. y =", AnswerType.Number, "3", 2, "6+y=9.", DifficultyLevel.Intermediate),
            new("Пара", "Какая пара подходит к x+y=7?", AnswerType.SingleChoice, "(2;5)", 1, "2+5=7.", DifficultyLevel.Beginner, "(2;5)", "(4;4)", "(8;1)", "(0;8)"),
            new("Методы", "Выберите методы решения систем", AnswerType.MultipleChoice, "подстановка;сложение", 2, "Оба метода стандартные.", DifficultyLevel.Intermediate, "подстановка", "сложение", "измерение угла", "процентная скидка"),
            new("Проверка", "Пара (3;4) подходит к x+y=7?", AnswerType.Text, "да", 1, "3+4=7.", DifficultyLevel.Beginner),
            new("Нет решений", "Если прямые параллельны и не совпадают, решений", AnswerType.SingleChoice, "нет", 1, "Они не пересекаются.", DifficultyLevel.Intermediate, "нет", "одно", "два", "бесконечно много"),
            new("Бесконечно много", "Если уравнения задают одну прямую, решений", AnswerType.SingleChoice, "бесконечно много", 1, "Все точки прямой подходят.", DifficultyLevel.Intermediate, "нет", "одно", "бесконечно много", "только два"),
            new("Количество", "Обычная система с двумя неизвестными часто содержит сколько уравнений?", AnswerType.Number, "2", 1, "Два уравнения.", DifficultyLevel.Beginner));
    }

    private async Task EnsureStatisticsPackAsync(int adminId, CancellationToken cancellationToken)
    {
        const string topic = "статистика и вероятность";
        await EnsureMaterialAsync(adminId, "Теория: статистика, среднее и медиана", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Как описывать набор данных: среднее, медиана, мода, размах.",
            """
            Статистика помогает описывать данные короткими числовыми характеристиками.

            Среднее арифметическое:
            Сложите все значения и разделите на их количество.
            Пример: для 2, 4, 9 среднее равно (2+4+9)/3 = 5.

            Медиана:
            Средний элемент упорядоченного ряда.
            Если элементов чётное количество, медиана - среднее двух центральных.

            Мода:
            Значение, которое встречается чаще всего.

            Размах:
            Максимальное значение минус минимальное.

            Когда что использовать:
            - Среднее удобно для ровных данных.
            - Медиана устойчивее к очень большим или очень маленьким выбросам.
            - Размах показывает разброс.
            """, true, cancellationToken, "Статистика");

        await EnsureMaterialAsync(adminId, "Теория: вероятность простых событий", topic, MaterialType.Theory, DifficultyLevel.Intermediate,
            "Классическая вероятность, благоприятные исходы, невозможные и достоверные события.",
            """
            Вероятность показывает, насколько ожидаемо событие.

            Классическая формула:
            P = число благоприятных исходов / число всех равновозможных исходов.

            Пример с монетой:
            Вероятность орла = 1/2.

            Пример с кубиком:
            Вероятность выпадения 6 = 1/6.
            Вероятность чётного числа = 3/6 = 1/2.

            Виды событий:
            - Достоверное: точно произойдёт, вероятность 1.
            - Невозможное: не произойдёт, вероятность 0.
            - Случайное: может произойти или не произойти.

            Вероятность можно записывать дробью, десятичной дробью или процентом:
            1/4 = 0.25 = 25%.
            """, true, cancellationToken, "Вероятность");

        await EnsurePracticeMaterialAsync(adminId, "Практика: статистика и вероятность", topic, DifficultyLevel.Intermediate,
            "Тренажёр на среднее, медиану, размах и вероятность простых событий.",
            "Для вероятности сначала посчитайте все исходы, затем благоприятные.",
            cancellationToken,
            new("Среднее", "Среднее чисел 3, 6, 9", AnswerType.Number, "6", 1, "18/3.", DifficultyLevel.Beginner),
            new("Медиана", "Медиана ряда 2, 5, 8", AnswerType.Number, "5", 1, "Средний элемент.", DifficultyLevel.Beginner),
            new("Размах", "Размах ряда 1, 4, 10", AnswerType.Number, "9", 1, "10-1.", DifficultyLevel.Intermediate),
            new("Кубик", "Вероятность выпадения 1 на кубике", AnswerType.SingleChoice, "1/6", 1, "Один исход из шести.", DifficultyLevel.Beginner, "1/6", "1/2", "1", "6"),
            new("Характеристики", "Выберите статистические характеристики", AnswerType.MultipleChoice, "среднее;медиана", 2, "Это характеристики данных.", DifficultyLevel.Beginner, "среднее", "медиана", "гипотенуза", "дискриминант"));

        await EnsureTestAsync(adminId, "Тест: статистика и вероятность — большой зачёт", DifficultyLevel.Advanced,
            "Итоговый тест по среднему, медиане, размаху и вероятности.",
            "Вероятность всегда находится от 0 до 1, или от 0% до 100%.", cancellationToken,
            new("Среднее", "Среднее чисел 5, 7, 12", AnswerType.Number, "8", 1, "24/3.", DifficultyLevel.Beginner),
            new("Среднее 2", "Среднее чисел 10, 20, 30, 40", AnswerType.Number, "25", 1, "100/4.", DifficultyLevel.Beginner),
            new("Медиана", "Медиана ряда 1, 4, 9, 10, 12", AnswerType.Number, "9", 1, "Центральный элемент.", DifficultyLevel.Intermediate),
            new("Размах", "Размах ряда 6, 8, 15, 20", AnswerType.Number, "14", 1, "20-6.", DifficultyLevel.Intermediate),
            new("Монета", "Вероятность орла при честной монете", AnswerType.SingleChoice, "1/2", 1, "Один исход из двух.", DifficultyLevel.Beginner, "1/2", "1/3", "1/6", "0"),
            new("Кубик", "Вероятность чётного числа на кубике", AnswerType.SingleChoice, "1/2", 2, "2,4,6 - три исхода из шести.", DifficultyLevel.Intermediate, "1/2", "1/3", "1/6", "1"),
            new("Процент", "Вероятность 0.4 в процентах", AnswerType.Number, "40", 1, "0.4*100.", DifficultyLevel.Beginner),
            new("Верные", "Выберите верные утверждения", AnswerType.MultipleChoice, "вероятность невозможного события равна 0;вероятность достоверного события равна 1", 2, "Это определения.", DifficultyLevel.Intermediate, "вероятность невозможного события равна 0", "вероятность достоверного события равна 1", "вероятность всегда больше 1", "медиана всегда равна максимуму"),
            new("Термин", "Событие с вероятностью 1 называется", AnswerType.Text, "достоверное", 1, "Оно точно произойдёт.", DifficultyLevel.Intermediate),
            new("Мода", "В ряду 2, 2, 3, 5 мода равна", AnswerType.Number, "2", 1, "2 встречается чаще всего.", DifficultyLevel.Intermediate));
    }

    private async Task EnsurePracticeMaterialAsync(int adminId, string title, string topic, DifficultyLevel difficulty, string description, string content, CancellationToken cancellationToken, params SeedQuestion[] questions)
    {
        var material = await EnsureMaterialAsync(adminId, title, topic, MaterialType.Practice, difficulty, description, content, true, cancellationToken, "Практика");
        foreach (var question in questions)
        {
            var task = await EnsureTaskAsync(material.Id, question.Title, question.Question, question.AnswerType, question.CorrectAnswer, question.MaxScore, question.Explanation, question.Difficulty, cancellationToken);
            for (var i = 0; i < question.Options.Length; i++)
            {
                var option = question.Options[i];
                var isCorrect = question.CorrectAnswer.Split(';', ',', '|').Any(x => string.Equals(x.Trim(), option, StringComparison.OrdinalIgnoreCase));
                await EnsureOptionAsync(task.Id, option, isCorrect, i + 1, cancellationToken);
            }
        }
    }

    private async Task EnsureTestAsync(int adminId, string title, DifficultyLevel difficulty, string description, string content, CancellationToken cancellationToken, params SeedQuestion[] questions)
    {
        var topic = title.StartsWith("Тест:", StringComparison.OrdinalIgnoreCase)
            ? title["Тест:".Length..].Split('—')[0].Trim()
            : title;
        var section = title.Contains('—') ? title.Split('—')[1].Trim() : "Основы";
        var material = await EnsureMaterialAsync(adminId, title, topic, MaterialType.Test, difficulty, description, content, true, cancellationToken, section);
        foreach (var question in questions)
        {
            var task = await EnsureTaskAsync(material.Id, question.Title, question.Question, question.AnswerType, question.CorrectAnswer, question.MaxScore, question.Explanation, question.Difficulty, cancellationToken);
            for (var i = 0; i < question.Options.Length; i++)
            {
                var option = question.Options[i];
                var isCorrect = question.CorrectAnswer.Split(';', ',', '|').Any(x => string.Equals(x.Trim(), option, StringComparison.OrdinalIgnoreCase));
                await EnsureOptionAsync(task.Id, option, isCorrect, i + 1, cancellationToken);
            }
        }

        for (var i = questions.Length + 1; i <= 10; i++)
        {
            var review = BuildReviewQuestion(topic, i, difficulty);
            await EnsureTaskAsync(material.Id, $"Дополнительный вопрос {i}", review.Question, review.AnswerType, review.CorrectAnswer, review.MaxScore, review.Explanation, review.Difficulty, cancellationToken);
        }
    }

    private static SeedQuestion BuildReviewQuestion(string topic, int index, DifficultyLevel difficulty)
    {
        var normalized = topic.ToLowerInvariant();
        if (normalized.Contains("линей"))
        {
            return index % 2 == 0
                ? new("Повторение", "Решите: x + 4 = 9", AnswerType.Number, "5", 1, "9 - 4 = 5.", DifficultyLevel.Beginner)
                : new("Повторение", "Подходит ли x = 2 для 3x = 6?", AnswerType.Text, "да", 1, "3 * 2 = 6.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("квадрат"))
        {
            return index % 2 == 0
                ? new("Повторение", "D для x^2 - 2x + 1 = 0", AnswerType.Number, "0", 1, "4 - 4 = 0.", DifficultyLevel.Intermediate)
                : new("Повторение", "Сколько корней при D > 0?", AnswerType.Number, "2", 1, "Положительный дискриминант даёт два корня.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("процент"))
        {
            return index % 2 == 0
                ? new("Повторение", "10% от 250", AnswerType.Number, "25", 1, "250 / 10 = 25.", DifficultyLevel.Beginner)
                : new("Повторение", "50% числа - это какая его часть?", AnswerType.Text, "половина", 1, "50% = 1/2.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("функц"))
        {
            return index % 2 == 0
                ? new("Повторение", "f(x)=x+6. f(4)", AnswerType.Number, "10", 1, "4 + 6 = 10.", DifficultyLevel.Beginner)
                : new("Повторение", "График линейной функции - это", AnswerType.Text, "прямая", 1, "y=kx+b задаёт прямую.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("треуг"))
        {
            return index % 2 == 0
                ? new("Повторение", "Сумма углов треугольника", AnswerType.Number, "180", 1, "Всегда 180 градусов.", DifficultyLevel.Beginner)
                : new("Повторение", "Сторона напротив прямого угла называется", AnswerType.Text, "гипотенуза", 1, "Это гипотенуза.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("степ") || normalized.Contains("кор"))
        {
            return index % 2 == 0
                ? new("Повторение", "sqrt(100)", AnswerType.Number, "10", 1, "10^2 = 100.", DifficultyLevel.Beginner)
                : new("Повторение", "2^5", AnswerType.Number, "32", 1, "2*2*2*2*2.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("систем"))
        {
            return index % 2 == 0
                ? new("Повторение", "x + y = 6, x = 2. y =", AnswerType.Number, "4", 1, "6 - 2 = 4.", DifficultyLevel.Beginner)
                : new("Повторение", "Решение системы должно подходить ко всем уравнениям?", AnswerType.Text, "да", 1, "Это смысл системы.", DifficultyLevel.Beginner);
        }

        if (normalized.Contains("стат") || normalized.Contains("вероят"))
        {
            return index % 2 == 0
                ? new("Повторение", "Среднее чисел 2 и 8", AnswerType.Number, "5", 1, "(2+8)/2.", DifficultyLevel.Beginner)
                : new("Повторение", "Вероятность достоверного события", AnswerType.Number, "1", 1, "Достоверное событие происходит всегда.", DifficultyLevel.Beginner);
        }

        return new("Повторение", $"Контрольный вопрос по теме \"{topic}\": введите 1, если материал повторён.", AnswerType.Number, "1", 1, "Повторение ключевой идеи темы.", difficulty);
    }

    private async Task EnsureOptionAsync(int taskId, string text, bool isCorrect, int sortOrder, CancellationToken cancellationToken)
    {
        var existing = await dbContext.AnswerOptions.FirstOrDefaultAsync(x => x.MathTaskId == taskId && x.Text == text, cancellationToken);
        if (existing is not null)
        {
            existing.IsCorrect = isCorrect;
            existing.SortOrder = sortOrder;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }
        dbContext.AnswerOptions.Add(new AnswerOption { MathTaskId = taskId, Text = text, IsCorrect = isCorrect, SortOrder = sortOrder });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSubmissionAsync(int userId, int taskId, string answer, decimal score, decimal maxScore, SubmissionStatus status, string feedback, CancellationToken cancellationToken)
    {
        if (await dbContext.TaskSubmissions.AnyAsync(x => x.UserId == userId && x.MathTaskId == taskId && x.Answer == answer, cancellationToken)) return;
        dbContext.TaskSubmissions.Add(new TaskSubmission { UserId = userId, MathTaskId = taskId, Answer = answer, Score = score, MaxScore = maxScore, Status = status, Feedback = feedback, SubmittedAtUtc = DateTime.UtcNow.AddDays(-1) });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureReviewAsync(int userId, int? materialId, int? taskId, int difficulty, int usefulness, string comment, CancellationToken cancellationToken)
    {
        if (await dbContext.Reviews.AnyAsync(x => x.UserId == userId && x.Comment == comment, cancellationToken)) return;
        dbContext.Reviews.Add(new Review { UserId = userId, EducationalMaterialId = materialId, MathTaskId = taskId, DifficultyRating = difficulty, UsefulnessRating = usefulness, Comment = comment, CreatedAtUtc = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNotificationAsync(int userId, string title, string message, NotificationType type, CancellationToken cancellationToken)
    {
        if (await dbContext.Notifications.AnyAsync(x => x.UserId == userId && x.Title == title, cancellationToken)) return;
        dbContext.Notifications.Add(new Notification { UserId = userId, Title = title, Message = message, Type = type, CreatedAtUtc = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedQuestion(string Title, string Question, AnswerType AnswerType, string CorrectAnswer, decimal MaxScore, string Explanation, DifficultyLevel Difficulty, params string[] Options);
}
