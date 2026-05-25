using System.Globalization;
using System.Windows.Data;
using MathTutor.Domain.Enums;

namespace MathTutor.Wpf.Converters;

public sealed class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Student => "Ученик",
        MaterialType.Theory => "Теория",
        MaterialType.Practice => "Задачи",
        MaterialType.Test => "Тест",
        MaterialType.Lesson => "Занятие",
        DifficultyLevel.Beginner => "Начальный",
        DifficultyLevel.Intermediate => "Средний",
        DifficultyLevel.Advanced => "Продвинутый",
        AnswerType.Text => "Текст",
        AnswerType.Number => "Число",
        AnswerType.SingleChoice => "Один вариант",
        AnswerType.MultipleChoice => "Несколько вариантов",
        AnswerType.ManualReview => "Ручная проверка",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
