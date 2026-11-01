# 📁 TrayFolderFlyout

<div align="center">

![TrayFolderFlyout](Assets/app.ico)

**Нативное десктопное приложение для Windows 11 на C# (.NET 8, WPF), возвращающее компактные кастомные папки на панель задач.**

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?logo=windows&logoColor=white)](https://github.com)
[![Framework](https://img.shields.io/badge/.NET-8.0--windows-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![UI](https://img.shields.io/badge/UI-Fluent%20Dark%20%7C%20Acrylic-6366F1)](https://github.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

</div>

---

## 💡 О проекте

В Windows 11 корпорация Microsoft удалила возможность закреплять папки в виде панелей инструментов (Toolbars) на панели задач. Пользователям приходится либо захламлять Рабочий стол десятками ярлыков, либо открывать громоздкий Проводник.

**TrayFolderFlyout** полностью решает эту проблему:
Вы создаёте собственные аккуратные папки (например: «Игры», «Работа», «Софт», «Система»), выбираете им стильные иконки и закрепляете прямо в панель задач Windows 11. При клике на иконку вместо Проводника мгновенно открывается компактное всплывающее окно (Flyout) прямо над иконкой — в точности как системное меню скрытых значков трея Windows 11.

---

## ✨ Ключевые особенности

- ⚡ **Мгновенный отклик (0–1 мс)**: Фоновая служба (Single-Instance Daemon) держит окна наготове в памяти с 0% нагрузки на процессор. Команды передаются через асинхронные Named Pipes без задержек.
- 🔗 **Автономное хранилище ярлыков (`SavedLinks`)**: Добавленные в папки ярлыки (`.lnk`, `.url`, `.exe`) копируются во внутреннее хранилище `%AppData%\TrayFolderFlyout\SavedLinks\`. Вы можете спокойно перемещать или удалять исходные ярлыки с Рабочего стола — всё продолжит работать.
- 🩹 **Auto-Heal (Автовосстановление)**: Если вы переместили оригинальные файлы ярлыков до их добавления, приложение при старте автоматически найдёт их через фоновый индексатор и восстановит пути.
- 🖼️ **Иконки высокой чёткости с кэшированием**: Все иконки извлекаются через нативный Win32 Shell API в STA-потоках с сохранением 32-битной прозрачности (ARGB). Никаких «белых листков» или мутных картинок.
- 🎨 **10 ярких встроенных пресетов**: Готовые градиентные иконки в стиле Windows 11 Fluent («Игры», «Работа», «Система», «Медиа», «Код», «Дизайн», «Интернет», «Офис», «Безопасность», «Папка») + поддержка любых пользовательских `.ico`, `.png`, `.jpg`, `.exe`.
- 🎛️ **Гибкая настройка отображения**:
  - Сетка от 3 до 6 колонок.
  - Режимы «Иконки с подписями» или «Только иконки».
  - Поддержка виртуальных папок и автосинхронизации с реальными папками диска (например, «Загрузки»).
  - Drag-and-Drop: перетаскивание для добавления файлов и сортировка карточек внутри папки.
- 🛡️ **Полная автономность**: Приложение и установщик скомпилированы в формате Self-Contained Single File со встроенным рантаймом .NET 8. Работает на любом ПК без предварительной установки фреймворков.

---

## 🚀 Установка и запуск

### Вариант 1. Установщик (Рекомендуется)
1. Скачайте **`TrayFolderFlyout_Setup.exe`** из раздела [Releases](../../releases).
2. Запустите установщик.
3. Выберите нужные опции:
   - `[x] Создать ярлык на Рабочем столе`
   - `[x] Запускать фоновую службу вместе с Windows`
4. Нажмите **«Установить»** — приложение распакуется в `%LocalAppData%\Programs\TrayFolderFlyout\`, зарегистрируется в меню «Пуск» и в параметрах Windows («Установленные приложения»).

### Вариант 2. Портативная версия
- Скачайте **`TrayFolderFlyout_Portable.exe`** и запустите из любой удобной папки. Все настройки, кэш и ярлыки будут храниться в `%AppData%\TrayFolderFlyout\`.

---

## 🗑️ Удаление

Приложение удаляется чисто и без следов:
- Через **«Параметры Windows» → «Приложения» → «Установленные приложения» → TrayFolderFlyout → Удалить**.
- Либо запуском `Uninstall.exe` в папке `%LocalAppData%\Programs\TrayFolderFlyout\`.

Установщик автоматически снимет задачу, удалит ярлыки, автозапуск из реестра и очистит файлы программы.

---

## 🛠️ Сборка из исходников

### Требования
- Windows 10 (1809+) или Windows 11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Клонирование репозитория
```bash
git clone https://github.com/your-username/TrayFolderFlyout.git
cd TrayFolderFlyout
```

### 2. Сборка основного приложения (Self-Contained)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o ./Publish/App
```

### 3. Сборка установщика `TrayFolderFlyout_Setup.exe`
```bash
dotnet publish TrayFolderFlyout.Setup/TrayFolderFlyout.Setup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o ./Release
```

Итоговые файлы:
- `./Release/TrayFolderFlyout_Setup.exe` — полноценный автономный инсталлятор.
- `./Publish/App/TrayFolderFlyout.exe` — портативный исполняемый файл.

---

## 📂 Архитектура решения

```
TaskbarFolders/
├── Assets/                        # Иконки и графические ресурсы
├── Models/                        # Модели данных (FolderGroup, FolderItem, AppConfig)
├── Native/                        # Win32 P/Invoke, COM ShellInterfaces, ShellShortcutHelper
├── Services/                      # Бизнес-логика (IpcService, IconService, ConfigService, TrayService)
├── Styles/                        # XAML-стили Fluent Dark
├── ViewModels/                    # MVVM ViewModels
├── Views/                         # FlyoutWindow, IconPickerWindow, InputDialog
├── TrayFolderFlyout.Setup/        # Проект автономного установщика
│   ├── MainWindow.xaml            # Fluent Dark интерфейс установщика
│   ├── InstallerLogic.cs          # Распаковка, ярлыки, реестр, удаление
│   └── ShortcutHelper.cs          # Нативное создание .lnk через IShellLinkW
├── App.xaml / App.xaml.cs         # Точка входа, Mutex, Named Pipes, диспетчер
├── MainWindow.xaml                # Менеджер папок и живой редактор элементов
└── TrayFolderFlyout.csproj        # Спецификация сборки .NET 8 WPF
```

---

## 📄 Лицензия

Проект распространяется под лицензией [MIT](LICENSE).
Вы можете свободно использовать, модифицировать и распространять данный код.
