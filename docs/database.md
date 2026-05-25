# База данных

Используется SQL Server LocalDB с EF Core migrations. Подключение по умолчанию:

```text
Server=(localdb)\MSSQLLocalDB;Database=MathTutor;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Основные таблицы: `Users`, `EducationalMaterials`, `MathTasks`, `AnswerOptions`, `TaskSubmissions`, `Reviews`, `Notifications`, `EmailLogs`.

Seed data создаёт администратора, учеников, темы, задания, отзывы, уведомления и несколько отправленных решений. Повторный запуск не создаёт дубликаты.
