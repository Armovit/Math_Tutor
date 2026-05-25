# Дизайн-система

Цель UI — clean educational SaaS/dashboard: светлый фон, карточки, спокойная палитра, понятная иерархия, один главный CTA на экран.

## Токены

Ресурсы находятся в `src/MathTutor.Wpf/Resources/DesignSystem`:

- `Colors.xaml` — background, surface, border, text, primary, success, warning, danger, info, difficulty colors.
- `Typography.xaml` — Display, H1, H2, H3, Body, Caption на Segoe UI Variable / Segoe UI.
- `Spacing.xaml` — шкала 4/8 px: 4, 8, 12, 16, 20, 24, 32, 40, 48.
- `Radius.xaml` — input, button, card radius.
- `Shadows.xaml` — мягкая тень карточек.
- `Controls.xaml` и `Components.xaml` — кнопки, поля, DataGrid, SurfaceCard, StatusPill.

## Правила

- Не задавать случайные inline-цвета в отдельных View.
- Формы группировать внутри SurfaceCard.
- Для списков предусматривать empty/error/loading паттерны через общие сообщения ViewModel.
- Destructive actions визуально отделять через DangerButton.
- Ошибки показывать human-readable, без stack trace.
