# Тестирование

Проверяются:

- требования к паролю и PBKDF2 hashing;
- регистрация и вход пользователя;
- seed data и связи БД через SQLite in-memory;
- автоматическая проверка числовых ответов.

Команды:

```powershell
dotnet build MathTutor.sln
dotnet test MathTutor.sln
```
