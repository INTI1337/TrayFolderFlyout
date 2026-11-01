# 📁 TrayFolderFlyout

<div align="center">

<img src="Assets/app.ico" alt="TrayFolderFlyout Logo" width="96" height="96" />

### Кастомные компактные папки на панели задач Windows 11

Нативное легковесное десктопное приложение на C# (.NET 8, WPF), возвращающее удобные всплывающие папки-меню на панель задач Windows 11 в фирменном стиле Fluent & Acrylic.

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?logo=windows&logoColor=white)](https://github.com/INTI1337/TrayFolderFlyout)
[![Framework](https://img.shields.io/badge/.NET-8.0--windows-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![UI](https://img.shields.io/badge/UI-Fluent%20Dark%20%7C%20Acrylic-6366F1)](https://github.com/INTI1337/TrayFolderFlyout)
[![Release](https://img.shields.io/github/v/release/INTI1337/TrayFolderFlyout?color=10B981&label=Release)](https://github.com/INTI1337/TrayFolderFlyout/releases)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

</div>

---

## 💡 Зачем это нужно?

В Windows 11 Microsoft удалила функцию классических инструментальных панелей (Toolbars), из-за чего пользователи лишились возможности закреплять папки с быстрыми ярлыками на панели задач. В результате Рабочий стол захламляется сотнями иконок, а поиск нужной программы через меню «Пуск» или Проводник отнимает время.

**TrayFolderFlyout** возвращает эту функциональность на качественно новом уровне:
Каждая папка закрепляется на панели задач как независимый значок. При клике на неё мгновенно открывается аккуратное всплывающее окно (Flyout) прямо над иконкой — в точности как системное всплывающее меню скрытых значков трея Windows 11.


<img width="1082" height="724" alt="image" src="https://github.com/user-attachments/assets/ea42964e-e7e8-427e-a2e1-dcec36bbed0b" />
<img width="235" height="128" alt="image" src="https://github.com/user-attachments/assets/7127f0ed-75ea-48cd-b8fd-0045d6e9ef23" />
<img width="377" height="300" alt="image" src="https://github.com/user-attachments/assets/f33efe6f-37c0-4d0a-957a-1ee849b9a331" />

---

## 📥 Как скачать и установить

> [!TIP]
> Приложение скомпилировано в формате **Self-Contained Single File**: вам **не нужно** устанавливать .NET Runtime или дополнительные библиотеки. Всё уже встроено внутрь и работает «из коробки» на любой Windows 10/11 x64.

### Вариант 1. Автоматический установщик (Рекомендуется)
1. Перейдите в блок [**Releases**](https://github.com/INTI1337/TrayFolderFlyout/releases/latest) в правой колонке репозитория.
2. Скачайте **`TrayFolderFlyout_Setup.exe`**.
3. Запустите установщик, отметьте нужные пункты:
   - `[x] Создать ярлык на Рабочем столе`
   - `[x] Запускать фоновую службу вместе с Windows`
4. Нажмите **«Установить»** — инсталлятор распакует программу в `%LocalAppData%\Programs\TrayFolderFlyout\`, создаст ярлыки и зарегистрирует приложение в меню «Установленные приложения» Windows.

### Вариант 2. Портативная версия (Portable)
1. В разделе [**Releases**](https://github.com/INTI1337/TrayFolderFlyout/releases/latest) скачайте **`TrayFolderFlyout_Portable.exe`**.
2. Поместите файл в любую папку и запустите. Никакой установки в систему не требуется, все настройки и кэш хранятся в `%AppData%\TrayFolderFlyout\`.

---

## ✨ Основные возможности

### ⚡ 1. Мгновенный отклик за 1 миллисекунду
- Архитектура **Single-Instance Daemon**: приложение работает в фоне как невидимая служба (0% CPU, минимум RAM) с иконкой в системном трее.
- При клике на закреплённую папку клиентский процесс за 1 мс передаёт ID группы через асинхронный **Named Pipe** (`TrayFolderFlyout_Pipe`), и заранее инициализированное окно мгновенно отображается на экране.

### 🎨 2. Кастомные иконки и 10 сочных пресетов
- 10 встроенных градиентных сквирклов в палитре Fluent Design: «Игры», «Работа», «Система», «Медиа», «Код», «Дизайн», «Интернет», «Офис», «Безопасность», «Папка».
- Возможность установить любую собственную иконку или изображение (`.ico`, `.png`, `.jpg`, `.jpeg`, `.bmp`, `.exe`) — они автоматически конвертируются в многослойный `.ico` с поддержкой всех разрешений Windows.

### 🫥 3. «Безымянные папки» для ультрачистого вида панели задач
- Если стереть название папки, приложение автоматически генерирует скрытый ярлык с уникальным символом Брайля (`\u2800`). На панели задач отображается только аккуратная иконка без лишнего текста и многоточий.

### 📦 4. Автономное хранилище ярлыков (`SavedLinks`)
- При перетаскивании (Drag-and-Drop) файлов и ярлыков в папку приложение копирует сам `.lnk`/`.url` внутрь `%AppData%\TrayFolderFlyout\SavedLinks\`.
- **Вы можете спокойно удалять исходные ярлыки с Рабочего стола** — папка продолжит стабильно запускать программы.

### 🩹 5. Интеллектуальное автовосстановление (Auto-Heal)
- Если файлы были перемещены во вложенные каталоги Рабочего стола или Меню «Пуск», встроенный сканер при запуске автоматически найдёт их, восстановит привязку и обновит кэш.

### 🖼️ 6. Кэширование иконок без «белых листков»
- Извлечение значков выполняется строго в STA-потоках через нативный Win32 Shell API (`IShellItemImageFactory`, `SHGetFileInfo`, `ExtractIconEx`).
- Все значки кэшируются в несжатый PNG с честным 32-битным альфа-каналом (прозрачностью) без черных ореолов.

### 🎛️ 7. Гибкая настройка структуры
- Сетка от 3 до 6 колонок.
- Переключение между режимами: «С подписями» (крупные карточки) и «Только иконки» (суперкомпактно).
- Синхронизация с реальными папками диска (например, автоотображение файлов из папки «Загрузки»).
- Изменение порядка элементов простым перетаскиванием мыши.

---

## 🗑️ Удаление

Приложение удаляется стандартно и начисто:
- **Параметры Windows** → **Приложения** → **Установленные приложения** → **TrayFolderFlyout** → **Удалить**.
- Либо запуском `Uninstall.exe` в папке программы.

Установщик снимет фоновую службу, удалит ярлыки из системы, очистит ветку автозапуска и запись в реестре.

---

## 🛠️ Сборка из исходного кода

### Требования
- ОС: Windows 10 (версия 1809+) или Windows 11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Шаги сборки:

1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/INTI1337/TrayFolderFlyout.git
   cd TrayFolderFlyout
   ```

2. **Соберите основное автономное приложение (Self-Contained):**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o ./Publish/App
   ```

3. **Соберите установщик `TrayFolderFlyout_Setup.exe`:**
   ```bash
   dotnet publish TrayFolderFlyout.Setup/TrayFolderFlyout.Setup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o ./Release
   ```

Готовые исполняемые файлы будут доступны в каталоге `./Release/`.

---

## 🏛️ Архитектура проекта

```
TrayFolderFlyout/
├── Assets/                        # Иконка приложения (256x256 Bento grid) и промо-арт
├── Models/                        # Доменные модели (AppConfig, FolderGroup, FolderItem)
├── Native/                        # Win32 P/Invoke, COM IShellLinkW, IShellItemImageFactory
├── Services/                      # Сервисы IpcService, ConfigService, IconService, TrayService
├── Styles/                        # Стили Fluent Dark, шаблоны CheckBox, Button, RadioButton
├── ViewModels/                    # MVVM связка для живого реактивного интерфейса
├── Views/                         # FlyoutWindow (попап), IconPickerWindow, InputDialog
├── TrayFolderFlyout.Setup/        # Отдельный проект установщика с Fluent-интерфейсом
│   ├── MainWindow.xaml            # Экран инсталлятора
│   ├── InstallerLogic.cs          # Распаковка, реестр, ярлыки, тихий анинсталл
│   └── ShortcutHelper.cs          # Win32 создание .lnk
├── App.xaml / App.xaml.cs         # Точка входа, Single Instance Mutex, диспетчер IPC
├── MainWindow.xaml                # Менеджер папок и визуальный редактор
└── TrayFolderFlyout.csproj        # Спецификация сборки .NET 8 WPF
```

---

## 📄 Лицензия

Проект распространяется под открытой лицензией [MIT License](LICENSE). Вы можете свободно использовать, модифицировать и распространять данный код.
